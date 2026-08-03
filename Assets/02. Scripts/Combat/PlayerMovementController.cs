using UnityEngine;

// 네트워크와 무관한 싱글플레이 테스트용 이동. 코어 로직 검증이 끝나면
// 이 스크립트 대신 네트워크 버전(NetworkPlayerController류)으로 교체한다.
[RequireComponent(typeof(PlayerCombatContext))]
public class PlayerMovementController : MonoBehaviour
{
    PlayerCombatContext combatContext;

    void Awake()
    {
        combatContext = GetComponent<PlayerCombatContext>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(h, v).normalized;
        transform.position += (Vector3)(dir * combatContext.EffectiveMoveSpeed * Time.deltaTime);
    }
}
