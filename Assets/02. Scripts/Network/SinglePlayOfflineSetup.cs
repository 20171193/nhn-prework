using Photon.Pun;
using UnityEngine;

// 싱글플레이 전투 씬 전용. CombatNetworkManager.Start()가 돌기 전(Awake 시점)에
// OfflineMode를 켜두면, 그 뒤는 기존 매칭/스폰 흐름이 코드 변경 없이 "나 혼자인 룸"으로
// 로컬 시뮬레이션된다 - PUN2 공식 오프라인 테스트 기능.
public class SinglePlayOfflineSetup : MonoBehaviour
{
    void Awake()
    {
        PhotonNetwork.OfflineMode = true;
    }
}
