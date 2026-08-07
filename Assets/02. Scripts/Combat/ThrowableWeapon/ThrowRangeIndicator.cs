using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

// 투척무기 에임 중에 두 가지를 같이 보여준다.
// 1. 플레이어 위치 기준 원 - 마우스 좌표와 무관한 전체 사거리(기존 로직 그대로)
// 2. muzzle -> 마우스 방향 포물선 - 지금 조준하면 실제로 어디로 날아갈지 궤적 미리보기
// WeaponRangeIndicator와 같은 원칙으로, 원격 플레이어(IsMine이 아님)는 그리지 않는다.
public class ThrowRangeIndicator : MonoBehaviour
{
    [FormerlySerializedAs("skill")]
    public ThrowableWeaponController throwableWeapon;

    [Header("범위 원 (플레이어 기준)")]
    [SerializeField] private LineRenderer circleLine;
    [SerializeField] private int segments = 48;

    [Header("궤적 곡선 (muzzle -> 마우스)")]
    [SerializeField] private LineRenderer curveLine;
    [SerializeField] private int curveSegments = 24;
    // 거리 대비 정점 높이 비율. 거리가 사거리에 가까울수록 maxArcHeightRatio에,
    // 가까우면 minArcHeightRatio에 가까워진다(거리 0이면 min, 사거리 끝이면 max).
    [SerializeField] private float minArcHeightRatio = 0.1f;
    [SerializeField] private float maxArcHeightRatio = 0.4f;

    PlayerAimController aim;
    PlayerCombatContext combatContext;
    PhotonView ownerPhotonView;

    void Awake()
    {
        ownerPhotonView = GetComponentInParent<PhotonView>();
        aim = GetComponentInParent<PlayerAimController>();
        combatContext = GetComponentInParent<PlayerCombatContext>();

        if (ownerPhotonView != null && !ownerPhotonView.IsMine)
        {
            circleLine.enabled = false;
            curveLine.enabled = false;
        }
    }

    void Update()
    {
        if (ownerPhotonView != null && !ownerPhotonView.IsMine) return;
        if (throwableWeapon == null) return;

        bool aiming = throwableWeapon.IsAiming;
        circleLine.enabled = aiming;
        curveLine.enabled = aiming;
        if (!aiming) return;

        DrawCircle(throwableWeapon.ThrowRange);

        if (aim != null && combatContext != null && combatContext.weaponController != null && combatContext.weaponController.muzzle != null)
            DrawCurve(combatContext.weaponController.muzzle.position, throwableWeapon.ThrowRange);

        SetEnabledLineColor(throwableWeapon.OnRange);
    }

    void DrawCircle(float radius)
    {
        circleLine.loop = true;
        circleLine.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 point = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            circleLine.SetPosition(i, point);
        }
    }

    // start(muzzle)와 end(마우스, 사거리 밖이면 사거리 경계로 클램프)를 선형보간하면서
    // 중간에서 최대가 되는 완만한 오프셋을 위로 더한다 - 실제 중력 계산 없이 "포물선 느낌"만 낸다.
    // 오프셋 높이는 거리에 비례해서, 가까우면 완만하고 멀면 더 높게 뜬다.
    void DrawCurve(Vector3 start, float range)
    {
        Vector2 toMouse = (Vector2)aim.MouseWorldPosition - (Vector2)start;
        Vector3 end = toMouse.magnitude > range
            ? start + (Vector3)(toMouse.normalized * range)
            : (Vector3)aim.MouseWorldPosition;

        curveLine.positionCount = curveSegments + 1;
        for (int i = 0; i <= curveSegments; i++)
        {
            float t = i / (float)curveSegments;
            curveLine.SetPosition(i, ThrowArcMath.Evaluate(start, end, t, minArcHeightRatio, maxArcHeightRatio, range));
        }
    }

    void SetEnabledLineColor(bool isEnabled)
    {
        Color color = isEnabled ? Color.green : Color.red;

        circleLine.startColor = color;
        circleLine.endColor = color;
        curveLine.startColor = color;
        curveLine.endColor = color;
    }
}
