using Photon.Pun;
using UnityEngine;

// 무기 사거리를 Muzzle에서 실제 발사 방향으로 항상 그려서 보여준다.
// Gizmos는 에디터 Scene 뷰에서만 보이고 빌드에는 안 나오므로, LineRenderer로 그린다.
// 0번(항상 존재하는 첫 발사체)은 인스펙터에 원래 붙어있는 LineRenderer 색 그대로 두고,
// 증강으로 늘어난 나머지 발사체들은 별도 LineRenderer + extraLineColor로 구분해서 보여준다.
// 발사체가 여러 개면 각 LineRenderer의 포인트 개수를 늘려서 (start, end0, start, end1, ...)
// 순서로 찍는다. 모든 발사체가 같은 지점(Muzzle)에서 나가므로 "되돌아가는" 구간은 방금
// 그린 선을 그대로 되짚어 겹치기만 하고, 결과적으로 광선 여러 개를 그린 것처럼 보인다.
// 내 조준선만 보여야 하므로, 네트워크 상 원격 플레이어(IsMine이 아님)면 아예 그리지 않는다.
[RequireComponent(typeof(WeaponController))]
[RequireComponent(typeof(LineRenderer))]
public class WeaponRangeIndicator : MonoBehaviour
{
    [SerializeField] private WeaponController weapon;
    [SerializeField] private PhotonView ownerPhotonView;
    [SerializeField] private LineRenderer baseLine;  // 0번 발사체 전용, 인스펙터 설정 색 그대로
    [SerializeField] private LineRenderer extraLine; // 1번 이후(증강으로 추가된) 발사체 전용

    PlayerAimController aim;
    PlayerCombatContext combatContext;
    ThrowableWeaponController throwableWeapon;

    void Awake()
    {
        aim = GetComponentInParent<PlayerAimController>();
        combatContext = GetComponentInParent<PlayerCombatContext>();
        ownerPhotonView = GetComponentInParent<PhotonView>();
        throwableWeapon = GetComponentInParent<ThrowableWeaponController>();

        if (ownerPhotonView != null && !ownerPhotonView.IsMine)
        {
            baseLine.enabled = false;
            extraLine.enabled = false;
        }
    }

    void Update()
    {
        if (ownerPhotonView != null && !ownerPhotonView.IsMine) return;
        if (aim == null || combatContext == null || weapon.muzzle == null) return;

        // 투척 스킬 에임 중에는 총 조준선을 숨긴다(ThrowRangeIndicator가 대신 원을 그려줌).
        bool isThrowAiming = throwableWeapon != null && throwableWeapon.IsAiming;
        baseLine.enabled = !isThrowAiming;
        extraLine.enabled = !isThrowAiming;
        if (isThrowAiming) return;

        Vector3 start = weapon.muzzle.position;

        // PlayerWeaponFireController.Fire()와 동일한 기준(Muzzle -> 마우스 월드 좌표)으로
        // 계산해야 실제 발사 방향과 표시선이 일치한다.
        Vector2 direction = (Vector2)aim.MouseWorldPosition - (Vector2)start;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        int count = combatContext.EffectiveProjectileCount;
        float spread = weapon.Stats.spreadAngleDegrees;
        float range = weapon.Stats.projectileRange;

        Vector2 baseDir = FanSpread.GetDirection(direction, 0, spread);
        baseLine.positionCount = 2;
        baseLine.SetPosition(0, start);
        baseLine.SetPosition(1, start + (Vector3)(baseDir * range));

        int extraCount = Mathf.Max(0, count - 1);
        extraLine.positionCount = extraCount * 2;
        for (int i = 0; i < extraCount; i++)
        {
            Vector2 fireDir = FanSpread.GetDirection(direction, i + 1, spread);
            Vector3 end = start + (Vector3)(fireDir * range);

            extraLine.SetPosition(i * 2, start);
            extraLine.SetPosition(i * 2 + 1, end);
        }
    }
}
