using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// 매치 흐름을 두 클라이언트 사이에서 맞추는 창구. GameManager가 쓴다.
//
// 흐름의 주인은 마스터 클라이언트 한 명이다. 마스터가 단계를 넘길 때마다
// (단계, 증강 선택 회차, 마감 시각, 그 단계가 끝났을 때 남는 전투 시간)을 방송하고,
// 나머지는 그것을 그대로 따라간다.
// 마감 시각은 PhotonNetwork.Time 기준의 절대 시각이라 양쪽이 같은 값을 본다.
// 각자 제한 시간을 세는 방식이었다면 접속 시점 차이와 프레임 차이만큼 진행이 어긋난다.
//
// PhotonView 대신 RaiseEvent를 쓴다. GameManager는 PhotonNetwork.Instantiate로 생기지 않고
// Bootstrap이 각자 만들어 씬을 넘겨 들고 다니는 오브젝트라 붙일 ViewID가 없다.
// RaiseEvent는 뷰가 필요 없고, MonoBehaviour가 아니어도 콜백을 받을 수 있다.
//
// 방에 들어와 있지 않으면(흐름 테스트 씬, 단독 실행) 스스로를 주인으로 보고 유니티 시계를 쓴다.
// 덕분에 GameManager는 온라인/오프라인을 나눠 쓰지 않고 한 갈래로 돌아간다.
public class GameFlowSync : IOnEventCallback, IInRoomCallbacks
{
    // Photon이 200번 이상을 내부용으로 쓰므로 그 아래에서 고른다.
    const byte PhaseEventCode = 71;
    const byte SelectionDoneEventCode = 72;
    const byte EndMatchEventCode = 73;
    const byte DeathEventCode = 74;

    // 단계 방송은 놓치면 그 클라이언트의 매치가 그대로 멈춘다. 반드시 신뢰성 있게 보낸다.
    static readonly RaiseEventOptions ToOthers = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
    static readonly RaiseEventOptions ToMaster = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };

    bool registered;

    // 방 안에 있을 때만 네트워크로 맞출 것이 있다.
    public bool IsOnline => PhotonNetwork.InRoom;

    // 흐름을 진행시킬 권한. 마스터이거나, 애초에 혼자일 때 참.
    public bool IsAuthority => !IsOnline || PhotonNetwork.IsMasterClient;

    // 단계 마감 시각을 재는 기준 시계.
    // PhotonNetwork.Time은 서버 시각이라 방 안의 모두가 같은 값을 읽는다. 접속 전에는 0이라 쓸 수 없다.
    public double Now => IsOnline ? PhotonNetwork.Time : Time.timeAsDouble;

    public int PlayerCount => IsOnline ? PhotonNetwork.CurrentRoom.PlayerCount : 1;

    public int LocalActorNumber => IsOnline ? PhotonNetwork.LocalPlayer.ActorNumber : 0;

    // 마스터가 보낸 단계.
    // (단계, 증강 선택 회차, 마감 시각, 그 단계가 끝났을 때 남는 전투 시간, 승자 ActorNumber)
    // 승자는 MatchOver 방송에만 실린다. 나머지 단계에서는 0이다.
    public event Action<GamePhase, int, double, float, int> PhaseReceived;
    // 증강을 다 고른 사람이 마스터에게 알린다. (보낸 사람 ActorNumber, 증강 선택 회차)
    public event Action<int, int> SelectionDoneReceived;
    // 매치를 끊어달라는 요청이 마스터에게 도착했다.
    public event Action EndMatchRequested;
    // 죽었다는 보고가 마스터에게 도착했다. (죽은 사람 ActorNumber)
    public event Action<int> DeathReported;
    // 마스터가 바뀌었다.
    public event Action MasterClientSwitched;

    public void Enable()
    {
        if (registered) return;

        PhotonNetwork.AddCallbackTarget(this);
        registered = true;
    }

    public void Disable()
    {
        if (!registered) return;

        PhotonNetwork.RemoveCallbackTarget(this);
        registered = false;
    }

    // 마스터 전용. 지금 들어간 단계와 그 마감 시각을 나머지에게 알린다.
    public void BroadcastPhase(GamePhase phase, int selectionIndex, double endTime, float matchTimeLeftAfter,
        int winnerActorNumber)
    {
        if (!IsOnline) return;

        Raise(PhaseEventCode,
            new object[] { (byte)phase, selectionIndex, endTime, matchTimeLeftAfter, winnerActorNumber },
            ToOthers);
    }

    // 증강 선택이 끝났음을 마스터에게 알린다. 마스터는 모두의 보고를 기다렸다가 전투로 돌아간다.
    public void ReportSelectionDone(int selectionIndex)
    {
        if (!IsOnline) return;

        Raise(SelectionDoneEventCode, selectionIndex, ToMaster);
    }

    // 승부가 갈렸으니 매치를 끊어달라고 마스터에게 요청한다.
    public void RequestEndMatch()
    {
        if (!IsOnline) return;

        Raise(EndMatchEventCode, null, ToMaster);
    }

    // 내 플레이어가 죽었다고 마스터에게 알린다. 누가 죽었는지는 보낸 사람으로 알 수 있으므로
    // 따로 싣지 않는다. 승자를 정하는 것은 받는 쪽(마스터)이다.
    public void ReportDeath()
    {
        if (!IsOnline) return;

        Raise(DeathEventCode, null, ToMaster);
    }

    // 죽은 사람을 뺀 나머지. 1:1이라 한 명이다.
    // 방을 나간 직후처럼 아무도 남지 않으면 0(승자 없음)을 준다.
    public int SurvivorActorNumber(int deadActorNumber)
    {
        if (!IsOnline) return 0;

        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (player.ActorNumber != deadActorNumber) return player.ActorNumber;
        }

        return 0;
    }

    static void Raise(byte code, object content, RaiseEventOptions options)
    {
        PhotonNetwork.RaiseEvent(code, content, options, SendOptions.SendReliable);
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case PhaseEventCode:
                var data = (object[])photonEvent.CustomData;
                PhaseReceived?.Invoke((GamePhase)(byte)data[0], (int)data[1], (double)data[2], (float)data[3],
                    (int)data[4]);
                break;

            case SelectionDoneEventCode:
                SelectionDoneReceived?.Invoke(photonEvent.Sender, (int)photonEvent.CustomData);
                break;

            case EndMatchEventCode:
                EndMatchRequested?.Invoke();
                break;

            case DeathEventCode:
                DeathReported?.Invoke(photonEvent.Sender);
                break;
        }
    }

    public void OnMasterClientSwitched(Player newMasterClient) => MasterClientSwitched?.Invoke();

    // IInRoomCallbacks의 나머지는 흐름과 상관없다. 마스터 교체 하나 때문에 같이 구현한다.
    public void OnPlayerEnteredRoom(Player newPlayer) { }
    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }
    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }
}
