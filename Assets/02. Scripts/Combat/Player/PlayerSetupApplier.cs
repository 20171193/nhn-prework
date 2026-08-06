using Photon.Pun;
using UnityEngine;

// Player 프리팹 루트에 부착. HUD는 씬에 미리 배치되어 CombatNetworkManager가 캐싱해두고
// 있으므로, 이 컴포넌트는 스폰 완료(Start)만 알리고 실제 연결은 매니저가 담당한다.
public class PlayerSetupApplier : MonoBehaviourPun
{
    void Start()
    {
        if (CombatNetworkManager.Instance == null)
        {
            Debug.LogWarning("CombatNetworkManager를 찾지 못해 세팅을 적용하지 못했습니다.", this);
            return;
        }

        CombatNetworkManager.Instance.ApplyPlayerSetup(photonView);
    }
}
