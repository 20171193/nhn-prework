using Photon.Pun;
using UnityEngine;

// WASD 이동. 로컬 소유(photonView.IsMine)일 때만 입력을 처리하고, 원격 플레이어는
// PhotonTransformView가 동기화해주는 위치를 그대로 따라간다.
[RequireComponent(typeof(PlayerCombatContext))]
[RequireComponent(typeof(PhotonView))]
public class PlayerMovementController : MonoBehaviourPun
{
    PlayerCombatContext combatContext;

    void Awake()
    {
        combatContext = GetComponent<PlayerCombatContext>();
    }

    void Update()
    {
        if (!photonView.IsMine || !combatContext.InputEnabled) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(h, v).normalized;
        transform.position += (Vector3)(dir * combatContext.EffectiveMoveSpeed * Time.deltaTime);
    }
}
