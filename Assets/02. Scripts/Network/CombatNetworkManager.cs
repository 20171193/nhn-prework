using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

// 1:1 대전 테스트용 연결/매칭 관리자.
// 고정 방 이름으로 접속해 2명이 모이면 각자 스폰 지점에서 NetworkPlayer를 생성한다.
public class CombatNetworkManager : MonoBehaviourPunCallbacks
{
    const string RoomName = "CombatTest1v1";
    const byte MaxPlayers = 2;

    public PlayerHUD playerHUD;
    public PlayerHUD enemyHUD;

    public TMP_Text statusLabel;
    public Transform[] spawnPoints;

    // 상대가 나갔다 다시 들어오는 등으로 매치가 두 번 시작되지 않게 막는다.
    bool matchStarted;

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
    }

    void Start()
    {
        // 기본값(SerializationRate 10/s)은 위치 갱신이 뜸해서 원격 캐릭터가 눈에 띄게 뒤처져 보인다.
        PhotonNetwork.SerializationRate = 20;
        PhotonNetwork.SendRate = 30;

        PlayerSetupSync.PublishLocal(LocalPlayerSetup.Current);

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

    // 두 명이 다 모인 뒤에 매치 흐름(증강 선택 → 전투 x3)을 시작한다.
    // 먼저 들어온 쪽에서 혼자 라운드가 돌기 시작하면 상대와 라운드가 어긋나기 때문이다.
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
