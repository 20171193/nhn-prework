using System.Collections;
using UnityEngine;

// 투척물(수류탄/섬광탄/연막탄 등)의 공통 로직. ThrowRangeIndicator와 같은 포물선
// 공식(ThrowArcMath)으로 이동하다가, 목적지에 도착하거나 도중에 Obstacle 레이어와
// 부딪히면 그 자리에서 멈추고, triggerDelay 후 범위 내 Player 레이어 전원(투척한 사람
// 본인 포함, 아군/적군 구분 없음)에게 효과를 적용한다.
// 완전히 로컬(비-네트워크) 오브젝트다 - 투척은 RPC로 전파되고, 각 클라이언트가
// ProjectilePool에서 꺼내 Init해서 독립적으로 재생한다(Projectile.cs와 같은 원칙).
public abstract class ThrowableObject : MonoBehaviour
{
    [SerializeField] float travelSpeed = 8f;
    [SerializeField] LayerMask obstacleLayer;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] float minArcHeightRatio = 0.1f;
    [SerializeField] float maxArcHeightRatio = 0.4f;

    protected float effectValue;
    protected float effectRadius;

    Vector3 start;
    Vector3 end;
    float throwRange;
    float travelDuration;
    float elapsed;
    float triggerDelay;

    // 자식 오브젝트 2개 - 평소엔 실제 투척물 모형만 보이고, 트리거(폭발/발동) 되는 순간
    // 그게 꺼지고 애니메이션 연출용 오브젝트가 대신 보인다. 둘이 동시에 켜져있는 시점은 없어야 한다.
    [SerializeField] GameObject model;
    [SerializeField] GameObject triggerEffect;

    // 트리거 애니메이션 재생 중에만 영향 범위를 보여주는 디버그용 원. Debug.DrawRay는 Gizmos
    // 파이프라인을 타서 렌더 파이프라인/뷰 설정에 따라 Game 뷰에 안 보이는 경우가 있어,
    // 실제로 렌더링되는 LineRenderer로 대체했다 - 코드로만 생성해서 별도 프리팹 연결이 필요 없다.
    LineRenderer effectRangeLine;

    enum State { Traveling, Waiting, Triggered }
    State state;

    void Awake()
    {
        var lineObj = new GameObject("EffectRangeDebug");
        lineObj.transform.SetParent(transform, false);

        effectRangeLine = lineObj.AddComponent<LineRenderer>();
        effectRangeLine.useWorldSpace = false;
        effectRangeLine.loop = true;
        effectRangeLine.widthMultiplier = 0.05f;
        effectRangeLine.material = new Material(Shader.Find("Sprites/Default"));
        effectRangeLine.startColor = Color.red;
        effectRangeLine.endColor = Color.red;
        effectRangeLine.enabled = false;
    }

    // Get()으로 풀에서 꺼내질 때마다(SetActive(true) 직후) 항상 불린다 - 실제 투척(Init 뒤이어 호출)
    // 뿐 아니라 미리보기(Init을 안 부르고 컴포넌트만 꺼서 재사용)도 이 경로를 타므로,
    // 이전 사용에서 트리거된 채로 남아있던 연출 상태를 여기서 초기화해야 미리보기에
    // 폭발 마지막 프레임이 그대로 남아 보이는 문제가 없다.
    void OnEnable()
    {
        SetVisual(triggered: false);
        effectRangeLine.enabled = false;
    }

    public void Init(Vector3 start, Vector3 end, ThrowableWeaponData data)
    {
        this.start = start;
        this.end = end;
        effectValue = data.effectValue;
        effectRadius = data.effectRadius;
        triggerDelay = data.triggerDelay;
        throwRange = data.range;

        travelDuration = Mathf.Max(0.01f, Vector3.Distance(start, end) / travelSpeed);
        elapsed = 0f;
        state = State.Traveling;
        transform.position = start;
    }

    void SetVisual(bool triggered)
    {
        if (model != null) model.SetActive(!triggered);
        if (triggerEffect != null) triggerEffect.SetActive(triggered);
    }

    void Update()
    {
        if (state == State.Traveling) UpdateTravel();
        else if (state == State.Waiting) UpdateWait();
    }

    // 트리거 시점에 한 번만 그린다 - 위치/반경이 이후로 안 바뀌므로 매 프레임 다시 그릴 필요가 없다.
    // 애니메이션이 끝나 ReturnToPool로 오브젝트가 비활성화되면 자식인 이 LineRenderer도 같이
    // 꺼지고, 다음 재사용 시 OnEnable에서 다시 off로 초기화된다.
    void DrawEffectRadiusDebug()
    {
        const int segments = 24;
        effectRangeLine.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            effectRangeLine.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * effectRadius);
        }
        effectRangeLine.enabled = true;
    }

    void UpdateTravel()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / travelDuration);
        Vector3 next = ThrowArcMath.Evaluate(start, end, t, minArcHeightRatio, maxArcHeightRatio, throwRange);

        var hit = Physics2D.Linecast(transform.position, next, obstacleLayer);
        if (hit.collider != null)
        {
            transform.position = hit.point;
            BeginWait();
            return;
        }

        transform.position = next;
        if (t >= 1f) BeginWait();
    }

    void BeginWait()
    {
        state = State.Waiting;
        elapsed = 0f;
    }

    void UpdateWait()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= triggerDelay) Trigger();
    }

    void Trigger()
    {
        state = State.Triggered; // 한 번만 발동되도록 - 안 바꾸면 매 프레임 재호출되어 효과/애니메이션이 반복 재생됨

        SetVisual(triggered: true);
        DrawEffectRadiusDebug();

        var hits = Physics2D.OverlapCircleAll(transform.position, effectRadius, playerLayer);
        ApplyEffect(hits);
        PlayTriggerAnimation();
    }

    // 범위 내 대상에게 효과를 준다. 기본은 아무것도 안 함(연막탄처럼 게임플레이 효과가 없는
    // 타입은 오버라이드하지 않아도 됨).
    protected virtual void ApplyEffect(Collider2D[] targets) { }

    // 트리거 애니메이션 재생 + (재생이 끝나면) ReturnToPool 호출까지 자식이 책임진다.
    // 타입마다 애니메이션 흐름이 달라서(단발 재생 vs 연막탄의 2단계 재생) 여기서 강제하지 않는다.
    protected abstract void PlayTriggerAnimation();

    protected void ReturnToPool() => ProjectilePool.Release(gameObject);

    // 단발성 애니메이션(수류탄/섬광탄처럼 "재생하고 끝나면 끝")을 재생하는 자식들이 공용으로
    // 쓰는 코루틴. 한 프레임 기다렸다가 재생하는 이유는 SetTrigger 직후에는 아직
    // 애니메이터가 이전 상태에 머물러 있어서 clip length를 정확히 못 읽기 때문.
    protected IEnumerator PlayAnimatorTrigger(Animator animator, string trigger)
    {
        animator.SetTrigger(trigger);
        yield return null;
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
    }
}
