using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 매치 한 판의 진행 단계.
public enum GamePhase
{
    None,           // 매치 시작 전
    Ready,          // 씬 진입 직후, 전투가 시작되기까지의 준비 시간
    Combat,         // 전투
    AugmentSelect,  // 전투를 끊고 들어가는 증강 선택
    MatchOver       // 매치 종료
}

// 매치 한 판(3분 30초 전투 + 중간 증강 선택 3회)의 흐름을 관장한다.
//
// 씬 진입 → 준비 → 전투 1분 → 증강 선택 → 전투 1분 → 증강 선택 → 전투 1분 → 증강 선택
//         → 전투 30초 → 종료.
//
// 매치 시계(MatchTimeLeft)는 전투 시간만 센다. 증강 선택 동안에는 멈춘다.
// 그래서 전투 구간을 다 더하면 60+60+60+30 = 210초, 정확히 3분 30초가 된다.
// 증강 선택이 전투 사이가 아니라 전투 도중에 끼어들므로, 그동안은 양쪽 조작을 막는다
// (PlayerCombatContext.SetInputEnabled). 카드를 고르는 사람이 일방적으로 얻어맞으면 안 된다.
//
// 흐름의 주인은 마스터 클라이언트 한 명이다(GameFlowSync).
// 마스터만 단계를 넘기는 코루틴을 돌리고, 나머지는 마스터가 방송한
// (단계, 증강 선택 회차, 마감 시각, 그 단계가 끝났을 때 남는 전투 시간)을 그대로 따라간다.
// 각자 제한 시간을 세면 접속 시점과 프레임 차이만큼 진행이 어긋나기 때문이다.
// 그래서 남은 시간도 흐르는 시간을 빼서 세지 않고, 공통 시계와 마감 시각의 차이로 잰다.
// 방에 들어와 있지 않으면 스스로가 주인이 되므로 흐름 테스트 씬은 혼자서도 그대로 돈다.
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
    [Tooltip("매치 전체 전투 시간(초). 증강 선택에 쓰는 시간은 여기 포함되지 않는다.")]
    [SerializeField] private float matchDuration = 210f;
    [Tooltip("전투 도중 증강 선택을 몇 번 띄울지.")]
    [SerializeField] private int augmentSelectCount = 3;
    [Tooltip("증강 선택이 뜨는 간격(초). 흐른 전투 시간 기준이다.")]
    [SerializeField] private float augmentSelectInterval = 60f;

    [Header("단계별 제한 시간(초)")]
    [Tooltip("씬에 들어와서 전투가 시작되기까지.")]
    [SerializeField] private float readyDuration = 3f;
    [SerializeField] private float augmentSelectDuration = 15f;

    private AugmentManager augmentManager;
    private IAugmentSelectionView selectionView;
    private PlayerCombatContext localPlayer;

    private readonly GameFlowSync sync = new GameFlowSync();
    // 이번 증강 선택을 끝낸 사람들. 마스터만 쓴다.
    private readonly HashSet<int> selectionDone = new HashSet<int>();

    private Coroutine matchRoutine;
    // 마스터의 방송을 따라가는 중. 마스터 쪽에서는 항상 false다.
    private bool isFollowing;
    private bool isPlayerChoosingAugment;
    // 제한 시간을 다 쓰기 전에 매치를 끊어달라는 요청이 들어왔다. 마스터만 쓴다.
    private bool isMatchEnding;
    // 현재 단계가 끝나는 시각. 방 안에서는 PhotonNetwork.Time 기준이라 모두가 같은 값을 본다.
    private double phaseEndTime;
    // 현재 단계가 끝났을 때 남아 있을 전투 시간(초).
    private float matchTimeLeftAfterPhase;

    public AugmentManager AugmentManager => augmentManager;

    public GamePhase Phase { get; private set; } = GamePhase.None;
    // 지금까지 띄운 증강 선택 횟수. 매치 시작 전에는 0.
    public int AugmentSelectionsOffered { get; private set; }
    public int AugmentSelectCount => augmentSelectCount;
    public float MatchDuration => matchDuration;

    // 매치에 남은 전투 시간(초). 증강 선택 중에는 멈춰 있다.
    public float MatchTimeLeft =>
        Phase == GamePhase.Combat ? matchTimeLeftAfterPhase + RemainingTime() : matchTimeLeftAfterPhase;

    // 다음 증강 선택이 뜨는 순간의 MatchTimeLeft 값. 남은 전투 시간으로 시점을 표현한다.
    private float NextSelectionAtMatchTime => matchDuration - (AugmentSelectionsOffered + 1) * augmentSelectInterval;

    // 아직 뜰 증강 선택이 남았는지. 횟수를 다 썼거나, 전투 시간이 모자라 다음 회차가
    // 통째로 잘리는 경우(MatchPhases가 남은 시간 0에서 멈추는 것과 같은 조건)에 false다.
    public bool HasNextAugmentSelect =>
        AugmentSelectionsOffered < augmentSelectCount && NextSelectionAtMatchTime > 0f;

    // 다음 증강 선택까지 남은 전투 시간(초). 남은 회차가 없으면 0.
    // 증강 선택 중에는 매치 시계가 멈추므로 이 값도 같이 멈춘 채 다음 회차까지의 간격을 가리킨다.
    public float TimeUntilAugmentSelect =>
        HasNextAugmentSelect ? Mathf.Max(0f, MatchTimeLeft - NextSelectionAtMatchTime) : 0f;

    // 현재 단계에 남은 시간(초). 증강 선택 타이머와 HUD가 매 프레임 읽어 쓴다.
    public float PhaseTimeLeft { get; private set; }
    public bool IsMatchRunning => matchRoutine != null || isFollowing;
    // 이 클라이언트가 흐름을 진행시키는 쪽인지. 승패 판정을 마스터에게 몰아줄 때 쓴다.
    public bool IsFlowAuthority => sync.IsAuthority;

    public event Action<GamePhase> PhaseChanged;
    public event Action MatchEnded;

    protected override void Awake()
    {
        base.Awake();

        // 중복 인스턴스는 base.Awake()가 파괴한다. 파괴될 오브젝트가 콜백을 등록하면 안 된다.
        if (Instance != this) return;

        sync.PhaseReceived += OnPhaseReceived;
        sync.SelectionDoneReceived += OnSelectionDoneReceived;
        sync.EndMatchRequested += OnEndMatchRequested;
        sync.MasterClientSwitched += OnMasterClientSwitched;
        sync.Enable();
    }

    private void OnDestroy()
    {
        sync.PhaseReceived -= OnPhaseReceived;
        sync.SelectionDoneReceived -= OnSelectionDoneReceived;
        sync.EndMatchRequested -= OnEndMatchRequested;
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

        isMatchEnding = false;
        PhaseTimeLeft = 0f;
        phaseEndTime = 0d;
        matchTimeLeftAfterPhase = 0f;
        AugmentSelectionsOffered = 0;
        selectionDone.Clear();
        SetPhase(GamePhase.None);
    }

    // 제한 시간이 남아 있어도 매치를 끝낸다. 승부가 갈렸을 때(사망 등) 부르는 자리다.
    // 마스터가 아니면 직접 끊지 못한다. 요청만 보내고, 실제로 끊는 것은 마스터다.
    // 한쪽만 매치를 끝내고 다른 쪽은 계속 싸우는 상황을 막기 위해서다.
    public void EndMatch()
    {
        if (!IsMatchRunning || Phase == GamePhase.MatchOver) return;

        if (sync.IsAuthority)
        {
            isMatchEnding = true;
            return;
        }

        sync.RequestEndMatch();
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

        // 단계 도중에 스폰됐을 수 있다. 지금 단계에 맞는 조작 상태로 맞춰준다.
        ApplyInputGate();
    }

    public void UnregisterLocalPlayer(PlayerCombatContext context)
    {
        if (localPlayer == context) localPlayer = null;
    }

    private void ResetMatchState()
    {
        augmentManager = new AugmentManager(augmentCatalog);
        AugmentSelectionsOffered = 0;
        isMatchEnding = false;
        matchTimeLeftAfterPhase = matchDuration;
        selectionDone.Clear();
    }

    // ── 마스터 쪽 진행 ────────────────────────────────────────────────

    private IEnumerator MatchRoutine()
    {
        yield return MatchPhases();

        // MatchEnded 핸들러가 곧바로 다음 매치를 시작할 수 있도록 먼저 비워둔다.
        matchRoutine = null;
        BeginPhase(GamePhase.MatchOver, sync.Now, 0f);
        PhaseTimeLeft = 0f;
        MatchEnded?.Invoke();
    }

    // 전투 시간을 augmentSelectInterval 단위로 끊어가며 사이사이 증강 선택을 끼운다.
    // 기본값(210초 / 3회 / 60초)이면 60-선택-60-선택-60-선택-30이 된다.
    private IEnumerator MatchPhases()
    {
        float combatLeft = matchDuration;

        yield return RunPhase(GamePhase.Ready, readyDuration, combatLeft);
        if (isMatchEnding) yield break;

        for (int i = 1; i <= augmentSelectCount && combatLeft > 0f; i++)
        {
            float segment = Mathf.Min(augmentSelectInterval, combatLeft);
            combatLeft -= segment;

            yield return RunPhase(GamePhase.Combat, segment, combatLeft);
            if (isMatchEnding) yield break;

            // 남은 전투 시간이 없으면 증강을 줘도 쓸 데가 없다. 그대로 매치를 끝낸다.
            if (combatLeft <= 0f) yield break;

            yield return AugmentSelectPhase(i, combatLeft);
            if (isMatchEnding) yield break;
        }

        // 마지막 증강 선택 뒤에 남은 전투 시간(기본값이면 30초).
        if (combatLeft > 0f)
            yield return RunPhase(GamePhase.Combat, combatLeft, 0f);
    }

    private IEnumerator AugmentSelectPhase(int selectionIndex, float matchTimeLeftAfter)
    {
        selectionDone.Clear();
        AugmentSelectionsOffered = selectionIndex;

        // 마감 시각을 먼저 못박고 방송한다. 화면을 여는 것은 그다음이다.
        // 상대는 이 시각에서 여유 시간을 뺀 만큼만 화면을 열어, 양쪽 로프 타이머가 같이 탄다.
        BeginPhase(GamePhase.AugmentSelect, sync.Now + augmentSelectDuration + SelectionGrace, matchTimeLeftAfter);
        OpenSelectionView(augmentSelectDuration);

        // 둘 다 고르면 남은 시간이 있어도 넘어간다. 아무도 응답하지 않으면 마감 시각에 끊긴다.
        yield return WaitForPhaseEnd(() => selectionDone.Count >= sync.PlayerCount);

        if (isPlayerChoosingAugment)
        {
            Debug.LogWarning("증강 선택 화면이 제한 시간 안에 응답하지 않아 그냥 넘어갑니다.", this);
            CloseSelectionView();
        }
    }

    private IEnumerator RunPhase(GamePhase phase, float duration, float matchTimeLeftAfter)
    {
        BeginPhase(phase, sync.Now + duration, matchTimeLeftAfter);

        yield return WaitForPhaseEnd(null);
    }

    // until이 참이 되거나 매치 종료 요청이 들어오면 시간이 남아도 즉시 끝낸다.
    private IEnumerator WaitForPhaseEnd(Func<bool> until)
    {
        while (true)
        {
            PhaseTimeLeft = RemainingTime();
            if (PhaseTimeLeft <= 0f) break;
            if (isMatchEnding) break;
            if (until != null && until()) break;

            yield return null;
        }

        PhaseTimeLeft = 0f;
    }

    // 단계에 들어간다. 마스터라면 같은 마감 시각을 상대에게도 알린다.
    // 방송을 SetPhase 안이 아니라 여기서 하는 이유: 알려야 할 것은 단계 자체가 아니라
    // "언제까지인지"와 "그때 전투 시간이 얼마 남는지"까지 묶인 한 벌이고, 그것을 아는 자리가 여기다.
    private void BeginPhase(GamePhase phase, double endTime, float matchTimeLeftAfter)
    {
        phaseEndTime = endTime;
        matchTimeLeftAfterPhase = matchTimeLeftAfter;
        PhaseTimeLeft = RemainingTime();

        if (sync.IsAuthority)
            sync.BroadcastPhase(phase, AugmentSelectionsOffered, endTime, matchTimeLeftAfter);

        SetPhase(phase);
    }

    private void SetPhase(GamePhase phase)
    {
        if (Phase == phase) return;

        Phase = phase;
        ApplyInputGate();
        PhaseChanged?.Invoke(phase);
    }

    // 전투 중에만 조작을 받는다. 증강 선택이 전투를 끊고 들어오는 구조라서,
    // 카드를 고르는 동안 상대가 계속 쏠 수 있으면 선택 시간이 그대로 불이익이 된다.
    // 준비/종료 단계에서도 같은 이유로 막는다.
    //
    // 매치가 시작되기 전(None)은 예외로 열어둔다. 전투 씬을 혼자 열어 조작을 확인하는
    // 경로(CombatNetworkManager 단독 실행)가 있는데, 여기서 막으면 그 경로가 통째로 죽는다.
    private void ApplyInputGate()
    {
        if (localPlayer == null) return;

        localPlayer.SetInputEnabled(Phase == GamePhase.Combat || Phase == GamePhase.None);
    }

    private float RemainingTime()
    {
        double left = phaseEndTime - sync.Now;
        return left > 0d ? (float)left : 0f;
    }

    // ── 따라가는 쪽 ──────────────────────────────────────────────────

    private void OnPhaseReceived(GamePhase phase, int selectionIndex, double endTime, float matchTimeLeftAfter)
    {
        // 내가 마스터면 내 코루틴이 진행을 맡는다. (교체 직후 늦게 도착한 방송)
        if (sync.IsAuthority) return;

        // 마스터의 첫 방송이 내 StartMatch보다 먼저 도착할 수 있다. 그래도 매치는 시작되어야 한다.
        if (!IsMatchRunning) StartMatch();
        // StartMatch가 실패했다면(카탈로그 누락) 따라갈 상태가 아니다.
        if (!isFollowing) return;

        // 마스터가 매치를 새로 시작했다면 증강도 시계도 처음으로 되돌린다.
        if (phase == GamePhase.Ready && Phase != GamePhase.Ready) ResetMatchState();

        phaseEndTime = endTime;
        matchTimeLeftAfterPhase = matchTimeLeftAfter;
        PhaseTimeLeft = RemainingTime();
        AugmentSelectionsOffered = selectionIndex;

        // 같은 단계가 다시 오면(재전송 등) 시각만 갱신하고 화면은 건드리지 않는다.
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
        matchTimeLeftAfterPhase = 0f;
        SetPhase(GamePhase.MatchOver);
        MatchEnded?.Invoke();
    }

    // ── 마스터가 받는 요청 ────────────────────────────────────────────

    private void OnSelectionDoneReceived(int actorNumber, int selectionIndex)
    {
        // 지난 회차에서 늦게 도착한 보고가 이번 선택을 앞당겨 끊지 않도록 막는다.
        if (!sync.IsAuthority || Phase != GamePhase.AugmentSelect) return;
        if (selectionIndex != AugmentSelectionsOffered) return;

        selectionDone.Add(actorNumber);
    }

    private void OnEndMatchRequested()
    {
        if (!sync.IsAuthority || !IsMatchRunning) return;

        isMatchEnding = true;
    }

    // ── 증강 선택 화면 ───────────────────────────────────────────────

    private void OpenSelectionView(float duration)
    {
        if (selectionView == null)
        {
            // 화면이 없다고 매치를 멈추지는 않는다. 선택 시간이 지나가면 전투로 돌아간다.
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

        // 마스터는 이 보고가 다 모여야 전투로 돌아간다. 고른 것이 없어도 보고는 해야 한다.
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

        sync.ReportSelectionDone(AugmentSelectionsOffered);
    }
}
