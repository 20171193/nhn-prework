using TMPro;
using UnityEngine;

public enum ControlMode
{
    AutoAimAutoFire = 1,      // 1안: 수동 이동만
    ManualAimAutoFire = 2,    // 2안: 수동 이동 + 수동 조준
    ManualAimManualFire = 3,  // 3안: 수동 이동 + 수동 조준 + 수동 발사
}

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float attackRate = 2f; // 초당 발사 횟수 (p/s)
    public float projectileSpeed = 12f;
    public GameObject projectilePrefab;
    public Transform[] dummyTargets;
    public ControlMode mode = ControlMode.AutoAimAutoFire;
    public FireGaugeUI fireGauge;
    public TMP_Text modeLabel;

    Camera cam;
    float fireTimer;

    float FireInterval => attackRate > 0f ? 1f / attackRate : Mathf.Infinity;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandleModeSwitch();
        HandleMovement();
        HandleAimAndFire();
        fireTimer -= Time.deltaTime;

        if (fireGauge != null)
            fireGauge.SetValue(1f - fireTimer / FireInterval);
    }

    void HandleModeSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) mode = ControlMode.AutoAimAutoFire;
        if (Input.GetKeyDown(KeyCode.Alpha2)) mode = ControlMode.ManualAimAutoFire;
        if (Input.GetKeyDown(KeyCode.Alpha3)) mode = ControlMode.ManualAimManualFire;

        if (modeLabel != null)
            modeLabel.text = $"Mode: {(int)mode} - {mode}  (Press 1/2/3 to switch)\n" +
                "Move: WASD/Arrows   Aim(2/3): Mouse   Fire(3): LMB";
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(h, v).normalized;
        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);

        if (mode == ControlMode.AutoAimAutoFire && dir.sqrMagnitude > 0.001f)
            FaceDirection(dir);
    }

    void HandleAimAndFire()
    {
        if (mode == ControlMode.AutoAimAutoFire)
        {
            Transform nearest = FindNearestTarget();
            if (nearest != null)
            {
                Vector2 aimDir = (Vector2)nearest.position - (Vector2)transform.position;
                if (aimDir.sqrMagnitude > 0.001f)
                    FaceDirection(aimDir.normalized);
            }

            if (fireTimer <= 0f)
            {
                Fire(FacingDirection());
                fireTimer = FireInterval;
            }
            return;
        }

        // 2안/3안: 마우스로 조준
        Vector2 mouseDir = GetMouseAimDirection();
        if (mouseDir.sqrMagnitude > 0.001f)
            FaceDirection(mouseDir);

        if (mode == ControlMode.ManualAimAutoFire)
        {
            if (fireTimer <= 0f)
            {
                Fire(FacingDirection());
                fireTimer = FireInterval;
            }
        }
        else // ManualAimManualFire
        {
            if (Input.GetButtonDown("Fire1") && fireTimer <= 0f)
            {
                Fire(FacingDirection());
                fireTimer = FireInterval;
            }
        }
    }

    Vector2 GetMouseAimDirection()
    {
        if (cam == null) return FacingDirection();

        Vector3 screenPoint = Input.mousePosition;
        screenPoint.z = transform.position.z - cam.transform.position.z;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(screenPoint);

        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : FacingDirection();
    }

    Transform FindNearestTarget()
    {
        if (dummyTargets == null) return null;

        Transform nearest = null;
        float best = float.MaxValue;
        foreach (var t in dummyTargets)
        {
            if (t == null) continue;
            float d = ((Vector2)t.position - (Vector2)transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearest = t;
            }
        }
        return nearest;
    }

    // 스프라이트가 기본적으로 위(+Y)를 바라본다고 가정하고 Z축 회전으로 조준 방향을 표현
    void FaceDirection(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    Vector2 FacingDirection()
    {
        return transform.up;
    }

    void Fire(Vector2 dir)
    {
        if (projectilePrefab == null) return;

        var proj = Instantiate(projectilePrefab, transform.position + (Vector3)(dir * 0.6f), transform.rotation);
        if (proj.TryGetComponent(out Projectile p))
            p.Init(dir, projectileSpeed);
    }
}
