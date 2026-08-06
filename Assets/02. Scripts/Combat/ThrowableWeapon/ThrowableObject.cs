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

    enum State { Traveling, Waiting }
    State state;

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

    void Update()
    {
        if (state == State.Traveling) UpdateTravel();
        else UpdateWait();
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
