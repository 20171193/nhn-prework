using Photon.Pun;
using UnityEngine;

// 좌클릭으로 에임 방향에 발사체를 쏜다 (3안: 수동 이동 + 수동 조준 + 수동 발사).
// 발사체 개수가 2개 이상이면 조준 방향을 중심으로 부채꼴로 퍼뜨려 쏜다.
// PhotonNetwork.Instantiate로 생성해 모든 클라이언트에 발사체가 보이게 하고,
// 발사 시점 값(방향/속도/데미지/사거리/소유자)은 instantiationData로 함께 넘긴다.
// PlayerWeaponFireController는 Weapon(자식 오브젝트)에 붙어있어 MonoBehaviourPun의
// photonView가 Player 루트의 PhotonView를 못 찾으므로 직접 GetComponentInParent로 받는다.
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

        // Shoulder는 Player 원점 기준 각도로 회전하지만, 실제 탄도는 Muzzle이 Shoulder보다
        // 아래에 있는 만큼 어긋난다. 그래서 발사 방향은 aim.AimDirection이 아니라
        // Muzzle -> 마우스 월드 좌표로 다시 계산해 커서를 정확히 겨냥하게 한다.
        Vector2 direction = (Vector2)aim.MouseWorldPosition - (Vector2)weapon.muzzle.position;
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        int count = combatContext.EffectiveProjectileCount;
        int ownerViewId = ownerPhotonView != null ? ownerPhotonView.ViewID : -1;

        for (int i = 0; i < count; i++)
        {
            Vector2 fireDir = FanSpread.GetDirection(direction, i, weapon.Stats.spreadAngleDegrees);
            object[] data =
            {
                fireDir.x, fireDir.y,
                combatContext.EffectiveProjectileSpeed,
                combatContext.EffectiveProjectileDamage,
                weapon.Stats.projectileRange,
                ownerViewId,
            };

            PhotonNetwork.Instantiate(ProjectilePrefabName, weapon.muzzle.position, Quaternion.identity, 0, data);
        }
    }
}
