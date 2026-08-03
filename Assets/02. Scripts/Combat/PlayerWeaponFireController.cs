using UnityEngine;

// 좌클릭으로 에임 방향에 발사체를 쏜다 (3안: 수동 이동 + 수동 조준 + 수동 발사).
// 프리팹은 Resources에서 이름으로 불러온다 - 나중에 네트워크로 전환할 때
// PhotonNetwork.Instantiate("Projectile", ...)로 바꾸기만 하면 되도록 맞춰둔 것.
[RequireComponent(typeof(WeaponController))]
public class PlayerWeaponFireController : MonoBehaviour
{
    public PlayerAimController aim;

    const string ProjectilePrefabName = "Projectile";

    WeaponController weapon;
    GameObject projectilePrefab;
    float fireTimer;

    void Awake()
    {
        weapon = GetComponent<WeaponController>();
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
            fireTimer = weapon.Stats.attackRate > 0f ? 1f / weapon.Stats.attackRate : 0f;
        }
    }

    void Fire()
    {
        if (projectilePrefab == null || aim == null) return;

        Vector2 direction = aim.AimDirection;
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        for (int i = 0; i < weapon.Stats.projectileCount; i++)
        {
            var proj = Instantiate(projectilePrefab, weapon.muzzle.position, Quaternion.identity);
            proj.GetComponent<Projectile>().Init(direction, weapon.ProjectileStats, weapon.Stats.projectileRange);
        }
    }
}
