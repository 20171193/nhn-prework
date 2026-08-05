using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 흐름 테스트 씬(GameFlow_Test) 전용 HUD.
//
// 클라이언트를 두 개 띄우지 않고 매치 흐름만 확인하기 위한 것이라, 네트워크도 전투도 없다.
// 씬에 들어오면 곧바로 매치를 시작하고 현재 단계/라운드/남은 시간을 보여준다.
// 전투는 실제 승패 판정이 없으므로 "라운드 즉시 종료" 버튼이 그 자리를 대신한다.
public class GameFlowTestHUD : MonoBehaviour
{
    public TMP_Text statusLabel;
    public Button endRoundButton;
    public Button restartButton;

    [Tooltip("씬에 들어오자마자 매치를 시작한다.")]
    public bool autoStart = true;

    void Start()
    {
        if (endRoundButton != null) endRoundButton.onClick.AddListener(OnEndRoundClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 없습니다. Resources/GameManager 프리팹이 있는지 확인하세요.", this);
            return;
        }

        // 화면을 보지 않고도 흐름을 되짚을 수 있도록 단계 전환을 콘솔에 남긴다.
        GameManager.Instance.PhaseChanged += OnPhaseChanged;
        GameManager.Instance.RoundStarted += OnRoundStarted;
        GameManager.Instance.MatchEnded += OnMatchEnded;

        if (autoStart) GameManager.Instance.StartMatch();
    }

    void OnDestroy()
    {
        // GameManager는 씬을 넘어 살아남는다. 씬이 바뀌어도 죽은 HUD가 남지 않도록 반드시 해제한다.
        if (GameManager.Instance == null) return;

        GameManager.Instance.PhaseChanged -= OnPhaseChanged;
        GameManager.Instance.RoundStarted -= OnRoundStarted;
        GameManager.Instance.MatchEnded -= OnMatchEnded;
    }

    void OnPhaseChanged(GamePhase phase) => Debug.Log($"[흐름] 단계: {PhaseName(phase)}");

    void OnRoundStarted(int round) => Debug.Log($"[흐름] 라운드 {round} 시작");

    void OnMatchEnded() => Debug.Log("[흐름] 매치 종료");

    void Update()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || statusLabel == null) return;

        statusLabel.text = BuildStatusText(gameManager);

        // 전투 중일 때만 라운드를 끊을 수 있다.
        if (endRoundButton != null)
            endRoundButton.interactable = gameManager.Phase == GamePhase.Combat;
    }

    void OnEndRoundClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.EndRound();
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

        return $"라운드 {gameManager.CurrentRound} / {gameManager.RoundCount}\n" +
               $"{PhaseName(gameManager.Phase)}\n" +
               $"남은 시간 {gameManager.PhaseTimeLeft:0.0}";
    }

    static string PhaseName(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Ready:         return "준비";
            case GamePhase.AugmentSelect: return "증강 선택";
            case GamePhase.Combat:        return "전투";
            case GamePhase.RoundOver:     return "라운드 종료";
            case GamePhase.MatchOver:     return "매치 종료";
            default:                      return "대기";
        }
    }
}
