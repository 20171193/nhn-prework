using Photon.Pun;
using TMPro;
using UnityEngine;

// 매치가 끝나면 뜨는 결과 화면. 누가 이겼는지를 보여준다.
//
// 승자는 ActorNumber로 넘어오고, 화면에 부르는 이름은 HP 바와 같은 출처(PlayerSetupSync)에서
// 찾아 쓴다. 같은 사람이 화면마다 다른 이름으로 불리면 안 되기 때문이다.
// 이름을 찾지 못하면 그때만 번호로 부른다("플레이어 1").
//
// 승자 판정은 GameManager(마스터)가 이미 끝냈다. 여기서는 읽어서 문구만 만든다.
// 판정을 UI에서 하면 두 클라이언트가 서로 다른 결과를 띄울 수 있다.
public class MatchResultUI : MonoBehaviour
{
    [Header("결과 화면")]
    [Tooltip("결과 화면 전체를 담은 오브젝트. 매치가 끝날 때만 켠다.")]
    [SerializeField] private GameObject panel;
    [Tooltip("누가 이겼는지. 예) \"플레이어 1 승리\"")]
    [SerializeField] private TMP_Text winnerLabel;
    [Tooltip("내 기준 결과. 예) \"승리!\" / \"패배\"")]
    [SerializeField] private TMP_Text outcomeLabel;
    [Tooltip("로비로 돌아가기까지 남은 시간. 비워두면 카운트다운을 표시하지 않는다.")]
    [SerializeField] private TMP_Text returnLabel;

    [Header("문구")]
    [Tooltip("{0}에 승자 이름이 들어간다.")]
    [SerializeField] private string winnerLabelFormat = "{0} 승리";
    [Tooltip("이름을 읽지 못했을 때 대신 쓸 이름. {0}에 Photon ActorNumber가 들어간다.")]
    [SerializeField] private string unknownNameFormat = "플레이어 {0}";
    [SerializeField] private string drawText = "무승부";
    [SerializeField] private string localWinText = "승리!";
    [SerializeField] private string localLoseText = "패배";
    [Tooltip("{0}에 남은 초가 들어간다.")]
    [SerializeField] private string returnFormat = "{0}초 후 로비로 돌아갑니다";

    // 마지막으로 찍은 카운트다운 문자열. 같은 값이면 TMP를 건드리지 않는다.
    private string lastReturnText;

    void Start()
    {
        SetPanelActive(false);

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 없습니다. Resources/GameManager 프리팹이 있는지 확인하세요.", this);
            return;
        }

        GameManager.Instance.MatchEnded += ShowResult;

        // 매치가 다시 시작되면(MatchOver가 아닌 단계로 넘어가면) 지난 결과를 걷어낸다.
        // MatchEnded만 듣고 있으면 띄우는 길만 있고 내리는 길이 없다.
        GameManager.Instance.PhaseChanged += OnPhaseChanged;
    }

    void OnDestroy()
    {
        // GameManager는 씬을 넘어 살아남는다. 씬이 바뀌어도 죽은 UI가 남지 않도록 반드시 해제한다.
        if (GameManager.Instance == null) return;

        GameManager.Instance.MatchEnded -= ShowResult;
        GameManager.Instance.PhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.MatchOver) return;

        SetPanelActive(false);
    }

    void ShowResult()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null) return;

        int winner = gameManager.WinnerActorNumber;

        SetPanelActive(true);
        SetText(winnerLabel, winner > 0 ? string.Format(winnerLabelFormat, ResolveDisplayName(winner)) : drawText);
        SetText(outcomeLabel, BuildOutcomeText(winner, gameManager.LocalActorNumber));
    }

    // 승자를 화면에 부를 이름. HP 바(PlayerHUD)와 같은 출처에서 읽어 두 곳의 이름이 같게 한다.
    //
    // PhotonNetwork.NickName이 아니라 CustomProperties(PlayerSetupSync)를 읽는 것이 중요하다.
    // 둘 다 "PlayerXXXX" 꼴이지만 서로 다른 난수라(MatchmakingManager가 NickName을 따로 만든다),
    // NickName을 쓰면 HP 바에 뜬 이름과 결과 화면의 이름이 달라진다.
    string ResolveDisplayName(int actorNumber)
    {
        if (PhotonNetwork.InRoom)
        {
            var player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (PlayerSetupSync.TryRead(player, out var info) && !string.IsNullOrEmpty(info.playerName))
                return info.playerName;
        }

        // 방 밖(흐름 테스트 씬)이거나 세팅이 아직 도착하지 않은 경우. 번호로 부른다.
        return string.Format(unknownNameFormat, actorNumber);
    }

    // 로비로 돌아가기까지 남은 시간. 세는 것은 CombatNetworkManager이고 여기서는 읽기만 한다.
    // 화면이 따로 세면 실제로 방을 나가는 순간과 숫자가 어긋난다.
    void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        var network = CombatNetworkManager.Instance;
        // 흐름 테스트 씬처럼 전투 네트워크 매니저가 없는 씬에서는 돌아갈 로비도 없다.
        bool counting = network != null && network.IsReturningToLobby;

        if (returnLabel != null && returnLabel.gameObject.activeSelf != counting)
            returnLabel.gameObject.SetActive(counting);

        if (!counting) return;

        // 0.1초라도 남아 있으면 1초로 올린다. 0초를 보여준 채 멈춰 있는 구간이 생기지 않는다.
        string text = string.Format(returnFormat, Mathf.CeilToInt(network.ReturnToLobbyTimeLeft));
        if (text == lastReturnText) return;

        SetText(returnLabel, text);
        lastReturnText = text;
    }

    // 방 밖(흐름 테스트 씬)에서는 내 ActorNumber가 0이라 내 기준 승패를 말할 수 없다.
    // 그때는 승자만 보여주고 이 줄은 비운다.
    string BuildOutcomeText(int winner, int localActorNumber)
    {
        if (winner <= 0) return drawText;
        if (localActorNumber <= 0) return string.Empty;

        return winner == localActorNumber ? localWinText : localLoseText;
    }

    void SetPanelActive(bool active)
    {
        // panel을 따로 두는 이유: 이 스크립트가 붙은 오브젝트를 끄면 Start도 OnDestroy도
        // 정상적으로 돌지 않아 구독이 꼬인다. 꺼지는 것은 항상 자식 쪽이어야 한다.
        if (panel != null && panel.activeSelf != active) panel.SetActive(active);

        // 다음에 다시 띄울 때 카운트다운이 지난 숫자에서 시작하지 않도록 비워둔다.
        if (!active) lastReturnText = null;
    }

    static void SetText(TMP_Text label, string text)
    {
        if (label != null) label.text = text;
    }
}
