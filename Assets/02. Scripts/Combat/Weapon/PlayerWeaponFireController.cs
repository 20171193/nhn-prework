using UnityEngine;

// 좌클릭으로 에임 방향에 발사체를 쏜다 (3안: 수동 이동 + 수동 조준 + 수동 발사).
// 발사체 개수가 2개 이상이면 조준 방향을 중심으로 부채꼴로 퍼뜨려 쏜다.
// 프리팹은 Resources에서 이름으로 불러온다 - 나중에 네트워크로 전환할 때
// PhotonNetwork.Instantiate("Projectile", ...)로 바꾸기만 하면 되도록 맞춰둔 것.
[RequireComponent(typeof(WeaponController))]
public class PlayerWeaponFireController : MonoBehaviour
{
    public PlayerAimController aim;

    const string ProjectilePrefabName = "Projectile";

    WeaponController weapon;
    PlayerCombatContext combatContext;
    GameObject projectilePrefab;
    float fireTimer;

    void Awake()
    {
        weapon = GetComponent<WeaponController>();
        combatContext = GetComponentInParent<PlayerCombatContext>();
        projectilePrefab = Resources.Load<GameObject>(ProjectilePrefabName);
        if (projectilePrefab == null)
            Debug.LogError($"Resources 폴더에서 '{ProjectilePrefabName}' 프리팹을 찾을 수 없습니다.");
    }

    void Update()
    {
        fireTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0) && fireTimer <= 0f)
        {
            Fire();
            fireTimer = combatContext.EffectiveAttackRate > 0f ? 1f / combatContext.EffectiveAttackRate : 0f;
        }
    }

    void Fire()
    {
        if (projectilePrefab == null || aim == null) return;

        // Shoulder는 Player 원점 기준 각도로 회전하지만, 실제 탄도는 Muzzle이 Shoulder보다
        // 아래에 있는 만큼 어긋난다. 그래서 발사 방향은 aim.AimDirection이 아니라
        // Muzzle -> 마우스 월드 좌표로 다시 계산해 커서를 정확히 겨냥하게 한다.
        Vector2 direction = (Vector2)aim.MouseWorldPosition - (Vector2)weapon.muzzle.position;
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        int count = combatContext.EffectiveProjectileCount;

        for (int i = 0; i < count; i++)
        {
            Vector2 fireDir = FanSpread.GetDirection(direction, i, weapon.Stats.spreadAngleDegrees);
            var proj = Instantiate(projectilePrefab, weapon.muzzle.position, Quaternion.identity);
            proj.GetComponent<Projectile>().Init(
                fireDir,
                combatContext.EffectiveProjectileSpeed,
                combatContext.EffectiveProjectileDamage,
                weapon.Stats.projectileRange,
                combatContext.hitCollider);
        }
    }
}
