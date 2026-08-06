using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 매치 한 판의 진행 단계.
public enum GamePhase
{
    None,           // 매치 시작 전
    Ready,          // 씬 진입 직후, 첫 증강 선택까지의 준비 시간
    AugmentSelect,  // 증강 선택
    Combat,         // 전투
    RoundOver,      // 라운드 정산. 다음 라운드까지의 짧은 간격
    MatchOver       // 매치 종료
}

// 매치 한 판(증강 선택 + 전투를 roundCount번 반복)의 흐름을 관장한다.
//
// 씬 진입 → 준비 → [증강 선택 → 전투] x3 → 종료.
// 각 단계는 제한 시간이 있고, 전투는 시간이 다 되거나 EndRound()가 불리면 끝난다.
// 승패 판정이 붙기 전까지 전투는 제한 시간으로만 끝난다.
//
// 흐름의 주인은 마스터 클라이언트 한 명이다(GameFlowSync).
// 마스터만 단계를 넘기는 코루틴을 돌리고, 나머지는 마스터가 방송한 (단계, 라운드, 마감 시각)을
// 그대로 따라간다. 각자 제한 시간을 세면 접속 시점과 프레임 차이만큼 라운드가 어긋나기 때문이다.
// 그래서 남은 시간도 흐르는 시간을 빼서 세지 않고, 공통 시계와 마감 시각의 차이로 잰다.
// 방에 들어와 있지 않으면 스스로가 주인이 되므로 흐름 테스트 씬은 예전과 똑같이 돈다.
//
// 씬을 넘어 살아남으므로 씬 오브젝트를 SerializeField로 들고 있지 않는다.
// 증강 선택 화면과 플레이어는 스스로 등록해준다.
public class GameManager : Singleton<GameManager>
{
    // 화면이 콜백을 놓쳐도, 상대가 끝까지 응답하지 않아도 매치가 멈추지 않도록 두는 여유 시간(초).
    // 카드 뒤집기 연출이 끝나고 그 결과가 마스터에게 도착할 때까지는 기다려준다.
    const float SelectionGrace = 1f;

    [SerializeField] private AugmentCatalog augmentCatalog;

    [Header("매치 구성")]
    [Tooltip("증강 선택 + 전투를 몇 번 반복할지.")]
    [SerializeField] private int roundCount = 3;

    [Header("단계별 제한 시간(초)")]
    [Tooltip("씬에 들어와서 첫 증강 선택이 뜨기까지.")]
    [SerializeField] private float readyDuration = 3f;
    [SerializeField] private float augmentSelectDuration = 15f;
    [Tooltip("한 라운드의 전투 제한 시간. 이 안에 승부가 나지 않으면 그냥 다음 라운드로 넘어간다.")]
    [SerializeField] private float combatDuration = 60f;
    [Tooltip("라운드가 끝나고 다음 증강 선택이 뜨기까지.")]
    [SerializeField] private float roundOverDuration = 2f;

    private AugmentManager augmentManager;
    private IAugmentSelectionView selectionView;
    private PlayerCombatContext localPlayer;

    private readonly GameFlowSync sync = new GameFlowSync();
    // 이번 증강 선택에서 다 고른 사람들. 마스터만 쓴다.
    private readonly HashSet<int> selectionDone = new HashSet<int>();

    private Coroutine matchRoutine;
    // 마스터의 방송을 따라가는 중. 마스터 쪽에서는 항상 false다.
    private bool isFollowing;
    private bool isPlayerChoosingAugment;
    private bool isRoundOver;
    // 현재 단계가 끝나는 시각. 방 안에서는 PhotonNetwork.Time 기준이라 모두가 같은 값을 본다.
    private double phaseEndTime;

    public AugmentManager AugmentManager => augmentManager;

    public GamePhase Phase { get; private set; } = GamePhase.None;
    // 1부터 시작한다. 매치 시작 전에는 0.
    public int CurrentRound { get; private set; }
    public int RoundCount => roundCount;
    // 현재 단계에 남은 시간(초). HUD가 매 프레임 읽어 쓴다.
    public float PhaseTimeLeft { get; private set; }
    public bool IsMatchRunning => matchRoutine != null || isFollowing;
    // 이 클라이언트가 흐름을 진행시키는 쪽인지. 승패 판정을 마스터에게 몰아줄 때 쓴다.
    public bool IsFlowAuthority => sync.IsAuthority;

    public event Action<GamePhase> PhaseChanged;
    public event Action<int> RoundStarted;
    public event Action MatchEnded;

    protected override void Awake()
    {
        base.Awake();

        // 중복 인스턴스는 base.Awake()가 파괴한다. 파괴될 오브젝트가 콜백을 등록하면 안 된다.
        if (Instance != this) return;

        sync.PhaseReceived += OnPhaseReceived;
        sync.SelectionDoneReceived += OnSelectionDoneReceived;
        sync.EndRoundRequested += OnEndRoundRequested;
        sync.MasterClientSwitched += OnMasterClientSwitched;
        sync.Enable();
    }

    private void OnDestroy()
    {
        sync.PhaseReceived -= OnPhaseReceived;
        sync.SelectionDoneReceived -= OnSelectionDoneReceived;
        sync.EndRoundRequested -= OnEndRoundRequested;
        sync.MasterClientSwitched -= OnMasterClientSwitched;
        sync.Disable();
    }

    private void Update()
    {
        // 마스터를 따라가는 쪽은 진행 코루틴이 없으므로 남은 시간을 여기서 갱신한다.
        if (isFollowing) PhaseTimeLeft = RemainingTime();
    }

    // 매치를 시작한다. 전투 씬에 진입한 쪽(CombatNetworkManager, 흐름 테스트 HUD)이 부른다.
    // 양쪽 클라이언트가 다 불러야 한다. 마스터가 아닌 쪽은 시간을 재지 않고 방송을 기다린다.
    //
    // 증강 상태를 Awake가 아니라 여기서 만드는 이유: AugmentManager의 수명은 앱이 아니라
    // 매치 한 판이다. Awake에서 만들면 이 오브젝트가 DontDestroyOnLoad로 계속 살아 있기 때문에,
    // 두 번째 매치에 첫 매치에서 먹은 증강이 그대로 딸려온다.
    [ContextMenu("Start Match")]
    public void StartMatch()
    {
        if (IsMatchRunning)
        {
            Debug.LogWarning("이미 매치가 진행 중입니다. 다시 시작하려면 StopMatch()를 먼저 부르세요.", this);
            return;
        }

        if (augmentCatalog == null)
        {
            Debug.LogError("AugmentCatalog가 비어 있습니다. Resources/GameManager 프리팹에 카탈로그를 연결하세요.", this);
            return;
        }

        ResetMatchState();

        // 흐름은 마스터 한 명이 진행시킨다. 나머지는 방송을 받아 따라가기만 한다.
        if (!sync.IsAuthority)
        {
            isFollowing = true;
            return;
        }

        matchRoutine = StartCoroutine(MatchRoutine());
    }

    // 진행 중인 매치를 중단하고 초기 상태로 되돌린다. 열려 있던 선택 화면도 닫는다.
    [ContextMenu("Stop Match")]
    public void StopMatch()
    {
        if (matchRoutine != null) StopCoroutine(matchRoutine);
        matchRoutine = null;
        isFollowing = false;

        CloseSelectionView();

        isRoundOver = false;
        PhaseTimeLeft = 0f;
        phaseEndTime = 0d;
        CurrentRound = 0;
        selectionDone.Clear();
        SetPhase(GamePhase.None);
    }

    // 전투 중 승부가 갈렸을 때(사망 등) 부른다. 제한 시간이 남아 있어도 라운드를 끝낸다.
    // 마스터가 아니면 직접 끊지 못한다. 요청만 보내고, 실제로 끊는 것은 마스터다.
    // 한쪽만 라운드를 끝내고 다른 쪽은 계속 싸우는 상황을 막기 위해서다.
    public void EndRound()
    {
        if (Phase != GamePhase.Combat) return;

        if (sync.IsAuthority)
        {
            isRoundOver = true;
            return;
        }

        sync.RequestEndRound(CurrentRound);
    }

    // 증강 선택 화면이 자기 자신을 등록한다. 화면이 사라질 때 Unregister까지 해줘야 한다.
    public void RegisterSelectionView(IAugmentSelectionView view)
    {
        if (view == null) return;

        // 한 씬에 화면이 둘 이상 있으면 어느 쪽이 뜰지가 등록 순서에 좌우된다.
        // 조용히 덮어쓰지 않고 알려준다.
        if (selectionView != null && !ReferenceEquals(selectionView, view))
            Debug.LogWarning($"증강 선택 화면이 이미 등록되어 있습니다. {view.GetType().Name}으로 교체합니다.", this);

        selectionView = view;
    }

    public void UnregisterSelectionView(IAugmentSelectionView view)
    {
        // 이미 다른 화면으로 교체된 뒤 늦게 도착한 해제는 무시한다.
        if (ReferenceEquals(selectionView, view)) selectionView = null;
    }

    // 고른 증강을 받을 플레이어. 선택 화면과 같은 이유로 플레이어 쪽에서 등록한다.
    // 흐름 테스트 씬처럼 플레이어가 없는 씬에서는 등록되지 않고, 그래도 흐름은 그대로 돈다.
    public void RegisterLocalPlayer(PlayerCombatContext context)
    {
        if (context == null) return;

        if (localPlayer != null && localPlayer != context)
            Debug.LogWarning("로컬 플레이어가 이미 등록되어 있습니다. 새로 등록된 쪽으로 교체합니다.", this);

        localPlayer = context;
    }

    public void UnregisterLocalPlayer(PlayerCombatContext context)
    {
        if (localPlayer == context) localPlayer = null;
    }

    private void ResetMatchState()
    {
        augmentManager = new AugmentManager(augmentCatalog);
        CurrentRound = 0;
        isRoundOver = false;
        selectionDone.Clear();
    }

    // ── 마스터 쪽 진행 ────────────────────────────────────────────────

    private IEnumerator MatchRoutine()
    {
        yield return RunPhase(GamePhase.Ready, readyDuration);

        for (int round = 1; round <= roundCount; round++)
        {
            StartRound(round);

            yield return AugmentSelectPhase();
            yield return CombatPhase();

            // 마지막 라운드 뒤에는 정산 간격 없이 곧바로 매치를 끝낸다.
            if (round < roundCount)
                yield return RunPhase(GamePhase.RoundOver, roundOverDuration);
        }

        // MatchEnded 핸들러가 곧바로 다음 매치를 시작할 수 있도록 먼저 비워둔다.
        matchRoutine = null;
        BeginPhase(GamePhase.MatchOver, sync.Now);
        PhaseTimeLeft = 0f;
        MatchEnded?.Invoke();
    }

    private IEnumerator AugmentSelectPhase()
    {
        selectionDone.Clear();

        // 마감 시각을 먼저 못박고 방송한다. 화면을 여는 것은 그다음이다.
        // 상대는 이 시각에서 여유 시간을 뺀 만큼만 화면을 열어, 양쪽 로프 타이머가 같이 탄다.
        BeginPhase(GamePhase.AugmentSelect, sync.Now + augmentSelectDuration + SelectionGrace);
        OpenSelectionView(augmentSelectDuration);

        // 둘 다 고르면 남은 시간이 있어도 넘어간다. 아무도 응답하지 않으면 마감 시각에 끊긴다.
        yield return WaitForPhaseEnd(() => selectionDone.Count >= sync.PlayerCount);

        if (isPlayerChoosingAugment)
        {
            Debug.LogWarning("증강 선택 화면이 제한 시간 안에 응답하지 않아 그냥 넘어갑니다.", this);
            CloseSelectionView();
        }
    }

    private IEnumerator CombatPhase()
    {
        isRoundOver = false;

        yield return RunPhase(GamePhase.Combat, combatDuration, () => isRoundOver);
    }

    // 한 단계를 duration초 동안 진행한다. until이 참이 되면 시간이 남아도 즉시 끝낸다.
    private IEnumerator RunPhase(GamePhase phase, float duration, Func<bool> until = null)
    {
        BeginPhase(phase, sync.Now + duration);

        yield return WaitForPhaseEnd(until);
    }

    private IEnumerator WaitForPhaseEnd(Func<bool> until)
    {
        while (true)
        {
            PhaseTimeLeft = RemainingTime();
            if (PhaseTimeLeft <= 0f) break;
            if (until != null && until()) break;

            yield return null;
        }

        PhaseTimeLeft = 0f;
    }

    private void StartRound(int round)
    {
        CurrentRound = round;
        RoundStarted?.Invoke(round);
    }

    // 단계에 들어간다. 마스터라면 같은 마감 시각을 상대에게도 알린다.
    // 방송을 SetPhase 안이 아니라 여기서 하는 이유: 알려야 할 것은 단계 자체가 아니라
    // "언제까지인지"까지 묶인 한 벌이고, 그것을 아는 자리가 여기다.
    private void BeginPhase(GamePhase phase, double endTime)
    {
        phaseEndTime = endTime;
        PhaseTimeLeft = RemainingTime();

        if (sync.IsAuthority) sync.BroadcastPhase(phase, CurrentRound, endTime);

        SetPhase(phase);
    }

    private void SetPhase(GamePhase phase)
    {
        if (Phase == phase) return;

        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }

    private float RemainingTime()
    {
        double left = phaseEndTime - sync.Now;
        return left > 0d ? (float)left : 0f;
    }

    // ── 따라가는 쪽 ──────────────────────────────────────────────────

    private void OnPhaseReceived(GamePhase phase, int round, double endTime)
    {
        // 내가 마스터면 내 코루틴이 진행을 맡는다. (교체 직후 늦게 도착한 방송)
        if (sync.IsAuthority) return;

        // 마스터의 첫 방송이 내 StartMatch보다 먼저 도착할 수 있다. 그래도 매치는 시작되어야 한다.
        if (!IsMatchRunning) StartMatch();
        // StartMatch가 실패했다면(카탈로그 누락) 따라갈 상태가 아니다.
        if (!isFollowing) return;

        // 마스터가 매치를 새로 시작했다면 증강도 라운드도 처음으로 되돌린다.
        if (phase == GamePhase.Ready && Phase != GamePhase.Ready) ResetMatchState();

        phaseEndTime = endTime;
        PhaseTimeLeft = RemainingTime();

        if (round > 0 && round != CurrentRound) StartRound(round);

        // 같은 단계가 다시 오면(재전송 등) 마감 시각만 갱신하고 화면은 건드리지 않는다.
        if (Phase == phase) return;

        SetPhase(phase);

        switch (phase)
        {
            case GamePhase.AugmentSelect:
                // 마스터가 잡은 마감 시각에서 여유 시간을 뺀 만큼만 연다.
                // 지연 시간이 그만큼 빠지므로 양쪽 화면이 같은 시각에 닫힌다.
                OpenSelectionView(Mathf.Max(0f, RemainingTime() - SelectionGrace));
                break;

            case GamePhase.MatchOver:
                CloseSelectionView();
                isFollowing = false;
                PhaseTimeLeft = 0f;
                MatchEnded?.Invoke();
                break;

            default:
                CloseSelectionView();
                break;
        }
    }

    // 1:1이라 마스터가 나갔다는 것은 상대가 나갔다는 뜻이다. 이어받을 이유가 없으니 매치를 끝낸다.
    // 3인 이상으로 늘린다면 남은 시간을 이어받아 진행을 재개하는 자리가 여기다.
    private void OnMasterClientSwitched()
    {
        if (!isFollowing) return;

        Debug.LogWarning("마스터가 방을 떠나 매치를 종료합니다.", this);

        CloseSelectionView();
        isFollowing = false;
        PhaseTimeLeft = 0f;
        SetPhase(GamePhase.MatchOver);
        MatchEnded?.Invoke();
    }

    // ── 마스터가 받는 요청 ────────────────────────────────────────────

    private void OnSelectionDoneReceived(int actorNumber, int round)
    {
        // 지난 라운드에서 늦게 도착한 보고가 이번 라운드를 앞당겨 끊지 않도록 막는다.
        if (!sync.IsAuthority || Phase != GamePhase.AugmentSelect || round != CurrentRound) return;

        selectionDone.Add(actorNumber);
    }

    private void OnEndRoundRequested(int round)
    {
        if (!sync.IsAuthority || Phase != GamePhase.Combat || round != CurrentRound) return;

        isRoundOver = true;
    }

    // ── 증강 선택 화면 ───────────────────────────────────────────────

    private void OpenSelectionView(float duration)
    {
        if (selectionView == null)
        {
            // 화면이 없다고 매치를 멈추지는 않는다. 선택 시간이 지나가면 전투로 넘어간다.
            Debug.LogError("증강 선택 화면이 등록되지 않았습니다. AugmentSelectionUI가 활성 상태인지 확인하세요.", this);
            return;
        }

        isPlayerChoosingAugment = true;
        selectionView.Show(augmentManager, duration, OnAugmentChosen);
    }

    private void CloseSelectionView()
    {
        if (!isPlayerChoosingAugment) return;

        isPlayerChoosingAugment = false;
        selectionView?.Hide();
    }

    private void OnAugmentChosen(AugmentData data)
    {
        isPlayerChoosingAugment = false;

        // 마스터는 이 보고가 다 모여야 전투로 넘어간다. 고른 것이 없어도 보고는 해야 한다.
        ReportSelectionDone();

        // 제한 시간 안에 고를 것이 하나도 없었으면 null이 온다.
        if (data == null) return;

        // 흐름만 확인하는 씬에는 증강을 받을 플레이어가 없다. 획득 기록만 남기고 넘어간다.
        if (localPlayer == null) return;

        // data.Apply()를 직접 부르지 않는다. 그러면 효과만 붙고 획득 알림이 나가지 않아
        // HUD의 증강 아이콘이 끝까지 비어 있는다. 확정 창구는 ApplyAugment 하나다.
        localPlayer.ApplyAugment(data);
    }

    private void ReportSelectionDone()
    {
        if (sync.IsAuthority)
        {
            selectionDone.Add(sync.LocalActorNumber);
            return;
        }

        sync.ReportSelectionDone(CurrentRound);
    }
}
