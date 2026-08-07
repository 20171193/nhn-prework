using System.Collections;
using Photon.Pun;
using UnityEngine;

// 수류탄: 트리거 시 범위 내 전원(투척한 사람 포함, 아군/적군 구분 없음)에게 데미지를 준다.
// Projectile.cs와 같은 victim-authoritative 원칙 - 맞은 대상이 본인 소유일 때만
// 그 클라이언트가 데미지를 적용한다(각자 판정하면 중복/불일치가 생기기 때문).
[RequireComponent(typeof(Animator))]
public class GrenadeThrowable : ThrowableObject
{
    [SerializeField] private Animator animator;
    [SerializeField] private string explodeTrigger = "Explode";

    protected override void ApplyEffect(Collider2D[] targets)
    {
        foreach (var target in targets)
        {
            var view = target.GetComponent<PhotonView>();
            if (view == null || !view.IsMine) continue;

            target.GetComponent<IDamageable>()?.ApplyDamage(effectValue);
        }
    }

    protected override void PlayTriggerAnimation()
    {
        StartCoroutine(ExplodeRoutine());
    }

    IEnumerator ExplodeRoutine()
    {
        yield return PlayAnimatorTrigger(animator, explodeTrigger);
        ReturnToPool();
    }
}
