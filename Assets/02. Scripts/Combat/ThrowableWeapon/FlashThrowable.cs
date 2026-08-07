using System.Collections;
using Photon.Pun;
using UnityEngine;

// 섬광탄: 트리거 시 범위 내에 "본인"이 있으면 로컬 화면만 밝아진다(상대는 상대 클라이언트에서
// 따로 판정됨). 데미지가 아니라 화면 연출이라 CombatNetworkManager가 들고 있는
// ScreenFadeEffect로 넘긴다 - 씬에 미리 배치된 UI라 풀링된 프리팹에서 직접 참조할 수 없다.
[RequireComponent(typeof(Animator))]
public class FlashThrowable : ThrowableObject
{
    [SerializeField] private Animator animator;
    [SerializeField] private string flashTrigger = "Flash";

    protected override void ApplyEffect(Collider2D[] targets)
    {
        foreach (var target in targets)
        {
            var view = target.GetComponent<PhotonView>();
            if (view == null || !view.IsMine) continue;

            CombatNetworkManager.Instance?.PlayScreenFade(effectValue);
            SoundManager.Instance?.PlaySfxUI(SfxId.FlashbangEffect); // 위치 무관, 맞은 로컬만
            break; // 로컬 플레이어는 하나뿐이라 한 번 찾으면 더 볼 필요 없음
        }
    }

    protected override void PlayTriggerAnimation()
    {
        // Trigger()가 클라이언트마다 로컬로 독립 실행되므로, 여기서 재생해도 양쪽
        // 클라이언트 모두 같은 타이밍에 터지는 소리가 들린다(GrenadeThrowable과 동일 원칙).
        SoundManager.Instance?.PlaySfx(SfxId.FlashbangPop, transform.position);
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        yield return PlayAnimatorTrigger(animator, flashTrigger);
        ReturnToPool();
    }
}
