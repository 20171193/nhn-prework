using System.Collections;
using UnityEngine;

// 연막탄: 게임플레이 효과는 없음(시각 전용) - ApplyEffect는 오버라이드하지 않는다.
// 작은 연기->큰 연기 재생 -> effectValue초 대기 -> 큰 연기->작은 연기 재생 -> 반납,
// 다른 타입처럼 단발 애니메이션이 아니라 2단계라 PlayAnimatorTrigger를 두 번 쓴다.
[RequireComponent(typeof(Animator))]
public class SmokeThrowable : ThrowableObject
{
    [SerializeField] private Animator animator;
    [SerializeField] private string growTrigger = "Grow";   // 작은 연기 -> 큰 연기
    [SerializeField] private string shrinkTrigger = "Shrink"; // 큰 연기 -> 작은 연기

    protected override void PlayTriggerAnimation()
    {
        StartCoroutine(SmokeRoutine());
    }

    IEnumerator SmokeRoutine()
    {
        yield return PlayAnimatorTrigger(animator, growTrigger);
        yield return new WaitForSeconds(effectValue);
        yield return PlayAnimatorTrigger(animator, shrinkTrigger);
        ReturnToPool();
    }
}
