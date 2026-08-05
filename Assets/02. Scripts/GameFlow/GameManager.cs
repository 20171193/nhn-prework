using System;
using System.Collections;
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
// 씬을 넘어 살아남으므로 씬 오브젝트를 SerializeField로 들고 있지 않는다.
// 증강 선택 화면과 플레이어는 스스로 등록해준다.
public class GameManager : Singleton<GameManager>
{
    // 화면이 콜백을 놓쳐도 매치가 멈추지 않도록 두는 여유 시간(초).
    // 카드 뒤집기 연출이 끝나는 시간까지는 기다려준다.
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

    private Coroutine matchRoutine;
    private bool isPlayerChoosingAugment;
    private bool isRoundOver;

    public AugmentManager AugmentManager => augmentManager;

    public GamePhase Phase { get; private set; } = GamePhase.None;
    // 1부터 시작한다. 매치 시작 전에는 0.
    public int CurrentRound { get; private set; }
    public int RoundCount => roundCount;
    // 현재 단계에 남은 시간(초). HUD가 매 프레임 읽어 쓴다.
    public float PhaseTimeLeft { get; private set; }
    public bool IsMatchRunning => matchRoutine != null;

    public event Action<GamePhase> PhaseChanged;
    public event Action<int> RoundStarted;
    public event Action MatchEnded;

    // 매치를 시작한다. 전투 씬에 진입한 쪽(CombatNetworkManager, 흐름 테스트 HUD)이 부른다.
    //
    // 증강 상태를 Awake가 아니라 여기서 만드는 이유: AugmentManager의 수명은 앱이 아니라
    // 매치 한 판이다. Awake에서 만들면 이 오브젝트가 DontDestroyOnLoad로 계속 살아 있기 때문에,
    // 두 번째 매치에 첫 매치에서 먹은 증강이 그대로 딸려온다.
    [ContextMenu("Start Match")]
    public void StartMatch()
    {
        if (matchRoutine != null)
        {
            Debug.LogWarning("이미 매치가 진행 중입니다. 다시 시작하려면 StopMatch()를 먼저 부르세요.", this);
            return;
        }

        if (augmentCatalog == null)
        {
            Debug.LogError("AugmentCatalog가 비어 있습니다. Resources/GameManager 프리팹에 카탈로그를 연결하세요.", this);
            return;
        }

        augmentManager = new AugmentManager(augmentCatalog);
        CurrentRound = 0;
        matchRoutine = StartCoroutine(MatchRoutine());
    }

    // 진행 중인 매치를 중단하고 초기 상태로 되돌린다. 열려 있던 선택 화면도 닫는다.
    [ContextMenu("Stop Match")]
    public void StopMatch()
    {
        if (matchRoutine != null) StopCoroutine(matchRoutine);
        matchRoutine = null;

        if (isPlayerChoosingAugment) selectionView?.Hide();

        isPlayerChoosingAugment = false;
        isRoundOver = false;
        PhaseTimeLeft = 0f;
        CurrentRound = 0;
        SetPhase(GamePhase.None);
    }

    // 전투 중 승부가 갈렸을 때(사망 등) 부른다. 제한 시간이 남아 있어도 라운드를 끝낸다.
    public void EndRound()
    {
        if (Phase != GamePhase.Combat) return;

        isRoundOver = true;
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

    private IEnumerator MatchRoutine()
    {
        yield return RunPhase(GamePhase.Ready, readyDuration);

        for (int round = 1; round <= roundCount; round++)
        {
            CurrentRound = round;
            RoundStarted?.Invoke(round);

            yield return AugmentSelectPhase();
            yield return CombatPhase();

            // 마지막 라운드 뒤에는 정산 간격 없이 곧바로 매치를 끝낸다.
            if (round < roundCount)
                yield return RunPhase(GamePhase.RoundOver, roundOverDuration);
        }

        // MatchEnded 핸들러가 곧바로 다음 매치를 시작할 수 있도록 먼저 비워둔다.
        matchRoutine = null;
        PhaseTimeLeft = 0f;
        SetPhase(GamePhase.MatchOver);
        MatchEnded?.Invoke();
    }

    private IEnumerator AugmentSelectPhase()
    {
        SetPhase(GamePhase.AugmentSelect);

        if (selectionView == null)
        {
            // 화면이 없다고 매치를 멈추지는 않는다. 선택 시간만큼 비워두고 전투로 넘어간다.
            Debug.LogError("증강 선택 화면이 등록되지 않았습니다. AugmentSelectionUI가 활성 상태인지 확인하세요.", this);
            yield return RunPhase(GamePhase.AugmentSelect, augmentSelectDuration);
            yield break;
        }

        isPlayerChoosingAugment = true;
        selectionView.Show(augmentManager, augmentSelectDuration, OnAugmentChosen);

        // 제한 시간은 화면 쪽에서도 재지만, 그쪽이 콜백을 놓쳐도 매치가 멈추지 않도록
        // 여기서도 같은 시간을 잰다. 정상 흐름이면 항상 콜백이 먼저 도착한다.
        yield return RunPhase(GamePhase.AugmentSelect, augmentSelectDuration + SelectionGrace,
            () => !isPlayerChoosingAugment);

        if (isPlayerChoosingAugment)
        {
            Debug.LogWarning("증강 선택 화면이 제한 시간 안에 응답하지 않아 그냥 넘어갑니다.", this);
            isPlayerChoosingAugment = false;
            selectionView.Hide();
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
        SetPhase(phase);

        PhaseTimeLeft = duration;
        while (PhaseTimeLeft > 0f)
        {
            if (until != null && until()) break;

            yield return null;
            PhaseTimeLeft -= Time.deltaTime;
        }

        PhaseTimeLeft = 0f;
    }

    private void SetPhase(GamePhase phase)
    {
        if (Phase == phase) return;

        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }

    private void OnAugmentChosen(AugmentData data)
    {
        isPlayerChoosingAugment = false;

        // 제한 시간 안에 고를 것이 하나도 없었으면 null이 온다.
        if (data == null) return;

        // 흐름만 확인하는 씬에는 증강을 받을 플레이어가 없다. 획득 기록만 남기고 넘어간다.
        if (localPlayer == null) return;

        data.Apply(localPlayer);
    }
}
