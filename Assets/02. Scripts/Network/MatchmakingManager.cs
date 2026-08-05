using System;
using System.Collections;
using Core;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public enum MatchmakingState
{
    Idle,           // 대기. 매칭 시작 가능
    Connecting,     // 포톤 마스터 서버 접속 중
    Searching,      // 상대 탐색 중(랜덤 입장 → 실패 시 방 생성 후 대기)
    MatchFound,     // 2명 충족. 카운트다운 후 게임 씬 진입
    Failed          // 접속/입장 실패. 다시 시도 가능
}

// 1:1 매치메이킹.
//
// 흐름: 상대 찾기 → JoinRandomRoom → (빈 방 없으면) CreateRoom → 2명 모이면 게임 씬 로드.
//
// 방 이름을 고정하지 않고 랜덤 입장 → 실패 시 생성 방식을 쓰는 이유는, 고정 이름이면 동시에
// 3명 이상 붙었을 때 세 번째부터 들어갈 방이 없어 그냥 실패하기 때문이다. 이 방식은 남는
// 사람끼리 알아서 새 방을 만든다.
public class MatchmakingManager : MonoBehaviourPunCallbacks
{
    const byte RequiredPlayers = 2;

    [Tooltip("2명이 모인 뒤 게임 씬으로 넘어가기까지의 대기 시간(초).")]
    [SerializeField] float matchStartDelay = 2f;

    [Tooltip("매칭 성사 시 로드할 전투 씬 이름. Build Settings에 등록되어 있어야 한다.")]
    [SerializeField] string gameSceneName = "Network_Prototype";

    [Tooltip("빌드가 다르면 서로 매칭되지 않도록 구분하는 버전 문자열.")]
    [SerializeField] string gameVersion = "1";

    public MatchmakingState State { get; private set; } = MatchmakingState.Idle;

    public event Action<MatchmakingState> StateChanged;
    public event Action<string> StatusChanged;
    // 매칭 성사 후 게임 시작까지 남은 초. 0이면 곧바로 씬 전환.
    public event Action<int> CountdownTick;

    // 탐색 도중 취소를 누르면 이미 날아간 요청의 콜백이 뒤늦게 도착한다.
    // 그 콜백으로 다시 매칭 흐름을 타지 않도록 하는 플래그.
    bool cancelRequested;
    Coroutine startRoutine;

    void Awake()
    {
        // 마스터 클라이언트가 LoadLevel을 호출하면 나머지 클라이언트도 같은 씬을 따라 로드한다.
        // 방에 들어가기 전에 켜 두어야 한다.
        PhotonNetwork.AutomaticallySyncScene = true;

        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = $"Player{UnityEngine.Random.Range(1000, 10000)}";
    }

    // "상대 찾기" 버튼이 호출한다.
    public void FindMatch()
    {
        if (State != MatchmakingState.Idle && State != MatchmakingState.Failed) return;

        cancelRequested = false;

        if (PhotonNetwork.IsConnectedAndReady)
        {
            // 이미 마스터 서버에 붙어 있으면(취소 후 재시도 등) 접속 단계를 건너뛴다.
            SetState(MatchmakingState.Searching, "상대를 찾는 중...");
            PhotonNetwork.JoinRandomRoom();
            return;
        }

        SetState(MatchmakingState.Connecting, "서버에 접속 중...");

        // 접속이 이미 진행 중이면 중복 호출하지 않고 OnConnectedToMaster를 기다린다.
        if (PhotonNetwork.NetworkClientState == ClientState.PeerCreated ||
            PhotonNetwork.NetworkClientState == ClientState.Disconnected)
        {
            // ConnectUsingSettings는 내부에서 GameVersion을 PhotonServerSettings의 AppVersion으로
            // 덮어쓴다(그 값은 현재 비어 있음). 그래서 호출 "뒤에" 설정해야 한다.
            // 실제 인증(OpAuthenticate)은 이 호출 이후 콜백에서 일어나므로 이 시점이면 늦지 않다.
            if (PhotonNetwork.ConnectUsingSettings())
                PhotonNetwork.GameVersion = gameVersion;
        }
    }

    // "취소" 버튼이 호출한다. 매칭이 성사되어 씬 전환이 시작된 뒤에는 받지 않는다.
    public void CancelSearch()
    {
        if (State == MatchmakingState.Idle) return;

        cancelRequested = true;
        StopStartRoutine();

        if (PhotonNetwork.InRoom)
        {
            SetStatus("취소하는 중...");
            PhotonNetwork.LeaveRoom();   // 나머지는 OnLeftRoom에서 처리
            return;
        }

        SetState(MatchmakingState.Idle, "대기 중");
    }

    public override void OnConnectedToMaster()
    {
        if (cancelRequested || State != MatchmakingState.Connecting) return;

        SetState(MatchmakingState.Searching, "상대를 찾는 중...");
        PhotonNetwork.JoinRandomRoom();
    }

    // 들어갈 만한 방이 없다 = 내가 첫 번째 플레이어다. 방을 만들고 상대를 기다린다.
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (cancelRequested || State != MatchmakingState.Searching) return;

        // 이름을 null로 두면 서버가 겹치지 않는 이름을 만들어 준다.
        var options = new RoomOptions
        {
            MaxPlayers = RequiredPlayers,
            IsOpen = true,
            IsVisible = true,
            // 방이 비면 남겨둘 이유가 없다. 유령 방이 랜덤 입장 대상으로 잡히는 것을 막는다.
            EmptyRoomTtl = 0,
            PlayerTtl = 0
        };

        PhotonNetwork.CreateRoom(null, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        // 취소 직후 입장 콜백이 도착한 경우: 곧바로 다시 나간다.
        if (cancelRequested)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        EvaluateRoom();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        EvaluateRoom();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (State != MatchmakingState.MatchFound) return;

        // 카운트다운 중에 상대가 나갔다. 씬 전환을 취소하고 다시 탐색 상태로 돌아간다.
        StopStartRoutine();
        ReopenRoom();
        SetState(MatchmakingState.Searching, "상대가 나갔습니다. 다시 찾는 중...");
    }

    public override void OnLeftRoom()
    {
        if (cancelRequested)
        {
            cancelRequested = false;
            SetState(MatchmakingState.Idle, "대기 중");
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetState(MatchmakingState.Failed, $"방 생성 실패: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetState(MatchmakingState.Failed, $"입장 실패: {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        StopStartRoutine();
        cancelRequested = false;

        // 취소 → 연결 해제 순으로 끝난 정상 종료까지 실패로 표시하지 않는다.
        if (State == MatchmakingState.Idle) return;

        SetState(MatchmakingState.Failed, $"연결이 끊어졌습니다: {cause}");
    }

    // 방 인원을 보고 대기할지 시작할지 결정한다. 입장/상대 입장 양쪽에서 호출된다.
    void EvaluateRoom()
    {
        if (!PhotonNetwork.InRoom) return;

        int count = PhotonNetwork.CurrentRoom.PlayerCount;

        if (count < RequiredPlayers)
        {
            SetState(MatchmakingState.Searching, $"상대를 기다리는 중... ({count}/{RequiredPlayers})");
            return;
        }

        if (State == MatchmakingState.MatchFound) return;   // 카운트다운 중복 시작 방지

        SetState(MatchmakingState.MatchFound, "상대를 찾았습니다!");

        // 씬 로딩 중에 다른 플레이어가 끼어들지 못하게 방을 닫는다.
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

        startRoutine = StartCoroutine(StartMatchRoutine());
    }

    IEnumerator StartMatchRoutine()
    {
        int remaining = Mathf.CeilToInt(matchStartDelay);

        while (remaining > 0)
        {
            CountdownTick?.Invoke(remaining);
            yield return YieldCache.WaitForSeconds(1f);
            remaining--;
        }

        CountdownTick?.Invoke(0);
        startRoutine = null;

        SetStatus("게임을 시작합니다...");

        // AutomaticallySyncScene이 켜져 있으므로 마스터만 호출하면 상대도 따라온다.
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(gameSceneName);
    }

    void StopStartRoutine()
    {
        if (startRoutine == null) return;

        StopCoroutine(startRoutine);
        startRoutine = null;
    }

    // 마스터가 나갔다면 남은 쪽이 마스터를 물려받으므로 여기서 다시 열 수 있다.
    void ReopenRoom()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;

        PhotonNetwork.CurrentRoom.IsOpen = true;
        PhotonNetwork.CurrentRoom.IsVisible = true;
    }

    void SetState(MatchmakingState next, string status)
    {
        if (State != next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }

        SetStatus(status);
    }

    void SetStatus(string status) => StatusChanged?.Invoke(status);
}
