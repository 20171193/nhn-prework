using Photon.Pun;
using UnityEngine;

// 좌클릭으로 에임 방향에 발사체를 쏜다. 발사체 여러 개를 한 번의 RPC로 묶어서 전파한다.
[RequireComponent(typeof(WeaponController))]
public class PlayerWeaponFireController : MonoBehaviour
{
    public Animator weaponAnimator; // 발사 애니메이션 트리거는 PlayerCombatContext.RpcFireVolley가 켠다(양쪽 클라이언트 모두 실행되므로).

    PlayerAimController aim;
    WeaponController weapon;
    PlayerCombatContext combatContext;
    PhotonView ownerPhotonView;
    ThrowableWeaponController throwableWeapon;
    float fireTimer;
    float cooldownDuration; // 방금 발사 시점 기준 재발사까지 걸리는 시간 - fireTimer와 함께 fill 계산에 쓴다.

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

        // 투척무기를 조준/투척하는 동안에도 쿨다운은 실시간으로 계속 흘러야 한다 - 여기서
        // return해버리면 fireTimer가 멈춰서, 돌아왔을 때 조준 전의 오래된 진행률이 그대로 보인다.
        fireTimer -= Time.deltaTime;

        if (throwableWeapon != null && throwableWeapon.IsAiming) return;

        if (Input.GetMouseButtonDown(0) && fireTimer <= 0f)
        {
            Fire();
            cooldownDuration = combatContext.EffectiveAttackRate > 0f ? 1f / combatContext.EffectiveAttackRate : 0f;
            fireTimer = cooldownDuration;
        }

        RefreshCursorState();
    }

    // 딜레이 중이면 Delay 상태 + 진행률(0=방금 발사 ~ 1=발사 가능), 아니면 Aiming.
    // 매 프레임 Update에서도 부르지만, 투척무기 조준을 취소/투척해서 총 모드로 막 돌아온
    // 프레임에는 ThrowableWeaponController가 직접 호출해서 다음 프레임까지 기다리지 않고
    // 곧바로 총의 실제 딜레이 상태를 반영한다.
    public void RefreshCursorState()
    {
        if (fireTimer > 0f && cooldownDuration > 0f)
        {
            CursorManager.Instance?.SetState(CursorState.Delay);
            CursorManager.Instance?.SetDelayFill(1f - fireTimer / cooldownDuration);
        }
        else
        {
            CursorManager.Instance?.SetState(CursorState.Aiming);
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
