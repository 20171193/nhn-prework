using Photon.Pun;
using UnityEngine;

// 좌클릭으로 에임 방향에 발사체를 쏜다. 발사체 여러 개를 한 번의 RPC로 묶어서 전파한다.
[RequireComponent(typeof(WeaponController))]
public class PlayerWeaponFireController : MonoBehaviour
{
    PlayerAimController aim;
    WeaponController weapon;
    PlayerCombatContext combatContext;
    PhotonView ownerPhotonView;
    ThrowableWeaponController throwableWeapon;
    float fireTimer;

    void Awake()
    {
        weapon = GetComponent<WeaponController>();
        aim = GetComponentInParent<PlayerAimController>();
        combatContext = GetComponentInParent<PlayerCombatContext>();
        ownerPhotonView = GetComponentInParent<PhotonView>();
        throwableWeapon = GetComponentInParent<ThrowableWeaponController>();
    }

    void Update()
    {
        if (ownerPhotonView != null && !ownerPhotonView.IsMine) return;
        if (!combatContext.InputEnabled) return;
        if (throwableWeapon != null && throwableWeapon.IsAiming) return;

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

        var directions = new Vector2[count];
        for (int i = 0; i < count; i++)
            directions[i] = FanSpread.GetDirection(direction, i, weapon.Stats.spreadAngleDegrees);

        float[] payload = ProjectileVolleyData.Pack(
            weapon.baseData.projectileData.id,
            combatContext.EffectiveProjectileSpeed,
            combatContext.EffectiveProjectileDamage,
            weapon.Stats.projectileRange,
            directions);

        combatContext.FireVolley(weapon.muzzle.position, payload);
    }
}
