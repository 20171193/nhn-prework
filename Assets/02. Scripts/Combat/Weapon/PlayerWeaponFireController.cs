using Photon.Pun;
using UnityEngine;

// 좌클릭으로 에임 방향에 발사체를 쏜다. 발사체 여러 개는 한 번의 PhotonNetwork.Instantiate로 묶어서 보낸다.
[RequireComponent(typeof(WeaponController))]
public class PlayerWeaponFireController : MonoBehaviour
{
    public PlayerAimController aim;

    const string ProjectilePrefabName = "Projectile";

    WeaponController weapon;
    PlayerCombatContext combatContext;
    PhotonView ownerPhotonView;
    float fireTimer;

    void Awake()
    {
        weapon = GetComponent<WeaponController>();
        combatContext = GetComponentInParent<PlayerCombatContext>();
        ownerPhotonView = GetComponentInParent<PhotonView>();
    }

    void Update()
    {
        if (ownerPhotonView != null && !ownerPhotonView.IsMine) return;

        fireTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0) && fireTimer <= 0f)
        {
            Fire();
            fireTimer = combatContext.EffectiveAttackRate > 0f ? 1f / combatContext.EffectiveAttackRate : 0f;
        }
    }

    void Fire()
    {
        if (aim == null) return;

        // Muzzle -> 마우스 월드 좌표 기준으로 계산해야 커서를 정확히 겨냥한다.
        Vector2 direction = (Vector2)aim.MouseWorldPosition - (Vector2)weapon.muzzle.position;
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        int count = combatContext.EffectiveProjectileCount;
        int ownerViewId = ownerPhotonView != null ? ownerPhotonView.ViewID : -1;

        var data = new object[5 + count * 2];
        data[0] = combatContext.EffectiveProjectileSpeed;
        data[1] = combatContext.EffectiveProjectileDamage;
        data[2] = weapon.Stats.projectileRange;
        data[3] = ownerViewId;
        data[4] = count;

        for (int i = 0; i < count; i++)
        {
            Vector2 fireDir = FanSpread.GetDirection(direction, i, weapon.Stats.spreadAngleDegrees);
            data[5 + i * 2] = fireDir.x;
            data[5 + i * 2 + 1] = fireDir.y;
        }

        PhotonNetwork.Instantiate(ProjectilePrefabName, weapon.muzzle.position, Quaternion.identity, 0, data);
    }
}
