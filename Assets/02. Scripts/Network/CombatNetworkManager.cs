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

    public TMP_Text statusLabel;
    public Transform[] spawnPoints;

    void Start()
    {
        // 기본값(SerializationRate 10/s)은 위치 갱신이 뜸해서 원격 캐릭터가 눈에 띄게 뒤처져 보인다.
        PhotonNetwork.SerializationRate = 20;
        PhotonNetwork.SendRate = 30;

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
        int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        PhotonNetwork.Instantiate("NetworkPlayer", spawnPos, Quaternion.identity);

        SetStatus($"Room joined ({PhotonNetwork.CurrentRoom.PlayerCount}/{MaxPlayers})");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SetStatus($"Room joined ({PhotonNetwork.CurrentRoom.PlayerCount}/{MaxPlayers})");
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
