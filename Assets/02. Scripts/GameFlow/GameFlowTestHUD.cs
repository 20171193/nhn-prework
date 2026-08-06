using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 흐름 테스트 씬(GameFlow_Test) 전용 HUD.
//
// 클라이언트를 두 개 띄우지 않고 매치 흐름만 확인하기 위한 것이라, 네트워크도 전투도 없다.
// 씬에 들어오면 곧바로 매치를 시작하고 매치 시계/현재 단계/남은 시간을 보여준다.
// 전투는 실제 승패 판정이 없으므로 "매치 즉시 종료" 버튼이 그 자리를 대신한다.
public class GameFlowTestHUD : MonoBehaviour
{
    public TMP_Text statusLabel;
    public Button endMatchButton;
    public Button restartButton;

    [Tooltip("씬에 들어오자마자 매치를 시작한다.")]
    public bool autoStart = true;

    void Start()
    {
        if (endMatchButton != null) endMatchButton.onClick.AddListener(OnEndMatchClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 없습니다. Resources/GameManager 프리팹이 있는지 확인하세요.", this);
            return;
        }

        // 화면을 보지 않고도 흐름을 되짚을 수 있도록 단계 전환을 콘솔에 남긴다.
        GameManager.Instance.PhaseChanged += OnPhaseChanged;
        GameManager.Instance.MatchEnded += OnMatchEnded;

        if (autoStart) GameManager.Instance.StartMatch();
    }

    void OnDestroy()
    {
        // GameManager는 씬을 넘어 살아남는다. 씬이 바뀌어도 죽은 HUD가 남지 않도록 반드시 해제한다.
        if (GameManager.Instance == null) return;

        GameManager.Instance.PhaseChanged -= OnPhaseChanged;
        GameManager.Instance.MatchEnded -= OnMatchEnded;
    }

    void OnPhaseChanged(GamePhase phase)
    {
        var gameManager = GameManager.Instance;

        // 증강 선택은 몇 번째인지가 흐름을 읽을 때 가장 중요하다.
        if (phase == GamePhase.AugmentSelect)
        {
            Debug.Log($"[흐름] 증강 선택 {gameManager.AugmentSelectionsOffered}/{gameManager.AugmentSelectCount}회차" +
                      $" (남은 전투 {gameManager.MatchTimeLeft:0.0}초)");
            return;
        }

        Debug.Log($"[흐름] 단계: {PhaseName(phase)}");
    }

    void OnMatchEnded() => Debug.Log("[흐름] 매치 종료");

    void Update()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || statusLabel == null) return;

        statusLabel.text = BuildStatusText(gameManager);

        // 매치가 도는 동안에만 끊을 수 있다.
        if (endMatchButton != null)
            endMatchButton.interactable = gameManager.IsMatchRunning && gameManager.Phase != GamePhase.MatchOver;
    }

    void OnEndMatchClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.EndMatch();
    }

    void OnRestartClicked()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null) return;

        gameManager.StopMatch();
        gameManager.StartMatch();
    }

    static string BuildStatusText(GameManager gameManager)
    {
        if (gameManager.Phase == GamePhase.None)
            return "매치 대기 중";

        if (gameManager.Phase == GamePhase.MatchOver)
        {
            int owned = gameManager.AugmentManager != null ? gameManager.AugmentManager.Owned.Count : 0;
            return $"매치 종료\n획득한 증강 {owned}개";
        }

        return $"매치 시간 {FormatClock(gameManager.MatchTimeLeft)} / {FormatClock(gameManager.MatchDuration)}\n" +
               $"증강 선택 {gameManager.AugmentSelectionsOffered} / {gameManager.AugmentSelectCount}회\n" +
               $"{PhaseName(gameManager.Phase)}\n" +
               $"단계 남은 시간 {gameManager.PhaseTimeLeft:0.0}";
    }

    // 매치 시계는 분:초로 읽는 편이 3분 30초짜리 흐름을 확인하기 쉽다.
    static string FormatClock(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"{total / 60}:{total % 60:00}";
    }

    static string PhaseName(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Ready:         return "준비";
            case GamePhase.Combat:        return "전투";
            case GamePhase.AugmentSelect: return "증강 선택";
            case GamePhase.MatchOver:     return "매치 종료";
            default:                      return "대기";
        }
    }
}
