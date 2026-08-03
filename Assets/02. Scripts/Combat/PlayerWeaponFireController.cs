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

        Vector2 direction = aim.AimDirection;
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        int count = combatContext.EffectiveProjectileCount;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float startAngle = -(count - 1) * weapon.Stats.spreadAngleDegrees / 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = (baseAngle + startAngle + i * weapon.Stats.spreadAngleDegrees) * Mathf.Deg2Rad;
            Vector2 fireDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            var proj = Instantiate(projectilePrefab, weapon.muzzle.position, Quaternion.identity);
            proj.GetComponent<Projectile>().Init(
                fireDir,
                combatContext.EffectiveProjectileSpeed,
                combatContext.EffectiveProjectileDamage,
                weapon.Stats.projectileRange);
        }
    }
}
