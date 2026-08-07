using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// 로비의 싱글플레이 진입 버튼 전용. SingleGame 씬(SinglePlayOfflineSetup)은 OfflineMode를
// 켜야 하는데, PUN2는 온라인으로 연결된 상태에서는 OfflineMode 전환을 거부한다.
// 매칭취소는 방만 나갈 뿐 마스터 서버 연결은 비동기로 끊기므로, 그 순간에 바로 씬을
// 넘어가면 SingleGame 쪽에서 OfflineMode 전환에 실패한다 - 그래서 씬을 넘어가기 전에
// 여기서 미리 연결 해제를 끝내고 넘어간다.
//
// 매칭취소 직후처럼 LeaveRoom 등 다른 오퍼레이션이 아직 진행 중인 상태에서 곧바로
// Disconnect를 호출하면 Photon 내부 연결 상태가 꼬일 수 있다(다음 접속 시도에서
// "Authenticate without Token" 등 인증 오류로 나타남) - 이걸 막기 위해 안정 상태
// (완전히 끊김/마스터 서버에 안정적으로 붙어 대기 중)일 때만 Disconnect를 호출한다.
// 매 프레임 대기하지 않고, 버튼을 누른 시점에만 상태를 확인한다 - 안정 상태가 아니면
// 로그만 남기고 아무것도 하지 않으니, 유저가 잠시 후 다시 누르면 된다.
public class SinglePlayEntryButton : MonoBehaviourPunCallbacks
{
    [SerializeField] private string sceneName = "SingleGame";

    bool waitingForDisconnect;

    public void OnClickEnter()
    {
        SoundManager.Instance?.PlaySfxUI(SfxId.ClickNormalBTN);

        var state = PhotonNetwork.NetworkClientState;

        // PeerCreated는 게임 시작 후 한 번도 접속을 시도한 적 없는 최초 상태라
        // Disconnected와 마찬가지로 온라인 연결이 전혀 없다 - 바로 진입해도 안전하다.
        if (state == ClientState.Disconnected || state == ClientState.PeerCreated)
        {
            FadeManager.Instance.FadeAndLoad(sceneName);
            return;
        }

        if (state == ClientState.ConnectedToMasterServer)
        {
            waitingForDisconnect = true;
            PhotonNetwork.Disconnect();
            return;
        }

        Debug.Log($"싱글플레이 입장 불가 - 현재 연결 상태: {state}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (!waitingForDisconnect) return;

        waitingForDisconnect = false;
        FadeManager.Instance.FadeAndLoad(sceneName);
    }
}
