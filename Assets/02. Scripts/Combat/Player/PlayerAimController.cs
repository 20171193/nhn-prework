using UnityEngine;

// 마우스 좌표 기준 에임.
// - Player 좌/우 판단만으로 Y 회전 결정: 0 = 좌측(초기 상태), 180 = 우측
// - Shoulder는 Z 회전 -90~90으로 상하 조준각 표현
public class PlayerAimController : MonoBehaviour
{
    public Transform shoulder;

    // Player 원점 기준 방향 - Shoulder 회전(시각적 팔 움직임)에만 쓴다.
    public Vector2 AimDirection { get; private set; }
    // 실제 마우스 월드 좌표 - Muzzle 등 실제 발사 방향 계산은 이 값을 기준으로 해야
    // Shoulder/Muzzle 오프셋과 무관하게 커서를 정확히 겨냥한다.
    public Vector3 MouseWorldPosition { get; private set; }

    Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        MouseWorldPosition = GetMouseWorldPosition();
        AimDirection = (Vector2)MouseWorldPosition - (Vector2)transform.position;
        ApplyFacing(AimDirection);
        ApplyShoulderAngle(AimDirection);
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPoint = Input.mousePosition;
        screenPoint.z = transform.position.z - cam.transform.position.z;
        return cam.ScreenToWorldPoint(screenPoint);
    }

    void ApplyFacing(Vector2 aimDir)
    {
        float facingY = aimDir.x < 0f ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0f, facingY, 0f);
    }

    void ApplyShoulderAngle(Vector2 aimDir)
    {
        if (shoulder == null) return;

        // atan2(y, |x|)는 항상 -90~90 범위 -> 좌/우 반전과 무관하게 상하 조준각만 표현
        float angle = Mathf.Atan2(aimDir.y, Mathf.Abs(aimDir.x)) * Mathf.Rad2Deg;
        shoulder.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
