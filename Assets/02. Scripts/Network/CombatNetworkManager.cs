using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 1:1 대전 테스트용 연결/매칭 관리자.
// 고정 방 이름으로 접속해 2명이 모이면 각자 스폰 지점에서 NetworkPlayer를 생성한다.
//
// 매치가 끝나면 결과를 잠깐 보여준 뒤 방을 나가고 로비로 돌아간다.
// 방을 나가는 것과 씬을 옮기는 것은 이 매니저의 일이다 - GameManager는 씬을 모르고,
// 결과 화면은 남은 초를 읽어 보여주기만 한다.
public class CombatNetworkManager : MonoBehaviourPunCallbacks
{
    const string RoomName = "CombatTest1v1";
    const byte MaxPlayers = 2;

    public PlayerHUD playerHUD;
    public PlayerHUD enemyHUD;

    public TMP_Text statusLabel;
    public Transform[] spawnPoints;

    [Header("매치 종료 후")]
    [Tooltip("결과 화면을 띄우고 로비로 돌아가기까지 기다리는 시간(초).")]
    [SerializeField] float returnToLobbyDelay = 5f;
    [Tooltip("돌아갈 로비 씬 이름. Build Settings에 등록되어 있어야 한다.")]
    [SerializeField] string lobbySceneName = "Lobby";

    // 상대가 나갔다 다시 들어오는 등으로 매치가 두 번 시작되지 않게 막는다.
    bool matchStarted;
    Coroutine returnRoutine;

    // 결과 화면(MatchResultUI)이 남은 초를 그대로 읽어 쓴다.
    // 카운트다운을 세는 쪽과 실제로 방을 나가는 쪽이 같아야 화면과 동작이 어긋나지 않는다.
    public bool IsReturningToLobby { get; private set; }
    public float ReturnToLobbyTimeLeft { get; private set; }

    // 씬에 미리 배치된 HUD를 들고 있는 쪽이 이 매니저이므로, 런타임에 생성되는 Player 쪽이
    // HUD를 찾아 헤매지 않고 여기로 스폰 완료만 알리면(ApplyPlayerSetup) 연결은 매니저가 한다.
    public static CombatNetworkManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // GameManager는 씬을 넘어 살아남는다. 씬이 바뀌어도 죽은 매니저가 남지 않도록 해제한다.
        if (GameManager.Instance == null) return;

        GameManager.Instance.MatchEnded -= OnMatchEnded;

        // 전투 씬이 사라지면 매치도 끝난 것이다. 여기서 되돌리지 않으면 GameManager가
        // MatchOver인 채로 로비까지 따라가고, 다음 매치의 전투 씬은 지난 판의 결과를
        // 들고 시작한다. 매치 상태의 수명은 전투 씬의 수명과 같아야 한다.
        GameManager.Instance.StopMatch();
    }

    void Start()
    {
        // 기본값(SerializationRate 10/s)은 위치 갱신이 뜸해서 원격 캐릭터가 눈에 띄게 뒤처져 보인다.
        PhotonNetwork.SerializationRate = 20;
        PhotonNetwork.SendRate = 30;

        PlayerSetupSync.PublishLocal(LocalPlayerSetup.Current);

        if (GameManager.Instance != null)
            GameManager.Instance.MatchEnded += OnMatchEnded;
        else
            Debug.LogError("GameManager가 없어 매치 종료 후 로비로 돌아갈 수 없습니다.", this);

        // 매치메이킹 씬(MatchmakingManager)을 거쳐 들어오면 이미 방에 들어와 있는 상태로
        // 이 씬이 로드된다. 씬 로드로 진입한 경우 OnJoinedRoom이 다시 오지 않으므로
        // 접속 절차를 건너뛰고 여기서 바로 스폰한다.
        if (PhotonNetwork.InRoom)
        {
            SpawnLocalPlayer();
            return;
        }

        // 이 씬을 단독으로 실행한 경우(매치메이킹 없이 테스트) 고정 방으로 직접 붙는다.
        SetStatus("Connecting...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        SetStatus("Connected. Joining room...");
        PhotonNetwork.JoinOrCreateRoom(RoomName, new RoomOptions { MaxPlayers = MaxPlayers }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        SpawnLocalPlayer();
    }

    void SpawnLocalPlayer()
    {
        int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        PhotonNetwork.Instantiate("Player", spawnPos, Quaternion.identity);

        SetStatus($"Room joined ({PhotonNetwork.CurrentRoom.PlayerCount}/{MaxPlayers})");
        TryStartMatch();
    }

    // 두 명이 다 모인 뒤에 매치 흐름(전투 3분 30초, 증강 선택 3회)을 시작한다.
    // 먼저 들어온 쪽에서 혼자 시계가 돌기 시작하면 상대와 진행이 어긋나기 때문이다.
    //
    // 양쪽 클라이언트가 다 불러야 한다. 단계를 실제로 넘기는 것은 마스터뿐이고,
    // 나머지는 마스터가 방송하는 마감 시각(PhotonNetwork.Time 기준)을 따라간다. GameManager 주석 참고.
    void TryStartMatch()
    {
        if (matchStarted) return;
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount < MaxPlayers) return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 없습니다. Resources/GameManager 프리팹이 있는지 확인하세요.", this);
            return;
        }

        matchStarted = true;
        GameManager.Instance.StartMatch();
    }

    // Player 프리팹의 PlayerSetupApplier가 스폰 완료(Start) 시점에 호출한다.
    // 소유권(IsMine)에 따라 미리 캐싱해둔 playerHUD/enemyHUD 중 하나에 세팅을 꽂아준다.
    public void ApplyPlayerSetup(PhotonView playerView)
    {
        if (!PlayerSetupSync.TryRead(playerView.Owner, out var info))
        {
            Debug.LogWarning($"{playerView.Owner?.NickName}의 PlayerInfo를 CustomProperties에서 찾지 못했습니다.", this);
            return;
        }

        var hud = playerView.IsMine ? playerHUD : enemyHUD;
        if (hud == null)
        {
            Debug.LogWarning("연결할 HUD가 캐싱되어 있지 않습니다.", this);
            return;
        }

        hud.Init(info, playerView.GetComponent<PlayerStatsController>(), playerView.GetComponent<PlayerCombatContext>());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SetStatus($"Room joined ({PhotonNetwork.CurrentRoom.PlayerCount}/{MaxPlayers})");
        TryStartMatch();
    }

    // 매치가 끝났다. 결과를 읽을 시간을 준 뒤 방을 나간다.
    // 양쪽이 같은 순간(마스터가 방송한 MatchOver)에 각자 세기 시작하므로 따로 맞출 필요가 없다.
    void OnMatchEnded()
    {
        if (returnRoutine != null) return;

        returnRoutine = StartCoroutine(ReturnToLobbyRoutine());
    }

    IEnumerator ReturnToLobbyRoutine()
    {
        IsReturningToLobby = true;
        ReturnToLobbyTimeLeft = returnToLobbyDelay;

        while (ReturnToLobbyTimeLeft > 0f)
        {
            yield return null;
            ReturnToLobbyTimeLeft -= Time.deltaTime;
        }

        ReturnToLobbyTimeLeft = 0f;
        returnRoutine = null;

        SetStatus("로비로 돌아갑니다...");

        // 방을 먼저 나가고, 나간 것이 확인되면(OnLeftRoom) 씬을 옮긴다.
        // AutomaticallySyncScene이 켜져 있어서, 방에 남은 채로 씬을 옮기면 상대까지 끌고 간다.
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            yield break;
        }

        // 이 씬을 단독으로 실행해 방에 들어간 적이 없는 경우. 나갈 방이 없으니 바로 옮긴다.
        LoadLobby();
    }

    public override void OnLeftRoom()
    {
        LoadLobby();
    }

    void LoadLobby()
    {
        // 씬이 Build Settings에 없으면 유니티 기본 오류만 나와 원인을 찾기 어렵다. 먼저 짚어준다.
        if (!Application.CanStreamedLevelBeLoaded(lobbySceneName))
        {
            Debug.LogError($"로비 씬 '{lobbySceneName}'을 로드할 수 없습니다. " +
                           "File > Build Settings에 씬이 등록되어 있고 체크되어 있는지 확인하세요.", this);
            return;
        }

        SceneManager.LoadScene(lobbySceneName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        SetStatus($"Opponent left ({PhotonNetwork.CurrentRoom.PlayerCount}/{MaxPlayers})");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetStatus($"Join failed: {message}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetStatus($"Create failed: {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SetStatus($"Disconnected: {cause}");
    }

    void SetStatus(string message)
    {
        if (statusLabel != null)
            statusLabel.text = message;
    }
}
