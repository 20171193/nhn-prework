using UnityEngine;

// 무기 사거리를 Muzzle에서 조준 방향으로 항상 그려서 보여준다.
// Gizmos는 에디터 Scene 뷰에서만 보이고 빌드에는 안 나오므로, LineRenderer로 그린다.
[RequireComponent(typeof(WeaponController))]
[RequireComponent(typeof(LineRenderer))]
public class WeaponRangeIndicator : MonoBehaviour
{
    public PlayerAimController aim;

    WeaponController weapon;
    LineRenderer line;

    void Awake()
    {
        weapon = GetComponent<WeaponController>();
        line = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (aim == null || weapon.muzzle == null) return;

        Vector2 direction = aim.AimDirection.sqrMagnitude > 0.0001f ? aim.AimDirection.normalized : Vector2.right;

        Vector3 start = weapon.muzzle.position;
        Vector3 end = start + (Vector3)(direction * weapon.Stats.projectileRange);

        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }
}
