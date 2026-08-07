using UnityEngine;

// 투척무기 한 종류를 빈 슬롯에 해금해주는 증강. 에셋 하나가 투척무기 하나에 대응한다
// (수류탄 증강, 섬광탄 증강, 연막탄 증강 ...).
//
// 슬롯0은 로비에서 고른 기본 투척무기(PlayData.throwableWeaponId)가 룸 입장 시
// CombatNetworkManager를 통해 차지하고, 슬롯1/2가 이 증강으로 채워지는 자리다.
//
// 이미 갖고 있는 투척무기를 또 주는 꽝 카드가 되지 않도록 IsOfferable에서 걸러낸다 -
// 로비에서 수류탄을 고른 플레이어에게는 수류탄 증강이 아예 선택지에 뜨지 않는다.
[CreateAssetMenu(fileName = "ThrowableAugmentData", menuName = "Augments/Throwable Augment")]
public class ThrowableAugmentData : AugmentData
{
    [Tooltip("이 증강이 해금하는 투척무기. 비워두면 줄 것이 없으므로 선택지에 뜨지 않는다.")]
    public ThrowableWeaponData throwable;

    // 아직 이 투척무기가 없고 넣을 빈 슬롯도 남아 있을 때만 선택지에 올린다.
    // 확인할 플레이어가 없으면(흐름 테스트 씬처럼 플레이어가 없는 씬) 막지 않는다 -
    // 그 씬은 증강 효과가 아니라 흐름만 보는 곳이라 선택지가 줄어들면 곤란하다.
    public override bool IsOfferable(PlayerCombatContext context)
    {
        if (throwable == null) return false;

        var controller = context != null ? context.throwableWeaponController : null;
        if (controller == null) return true;

        return !controller.Owns(throwable.id) && controller.FirstEmptySlotIndex() >= 0;
    }

    public override void Apply(PlayerCombatContext context)
    {
        if (throwable == null)
        {
            Debug.LogWarning($"{name}: 해금할 투척무기가 지정되지 않았습니다.", this);
            return;
        }

        var controller = context.throwableWeaponController;
        if (controller == null)
        {
            Debug.LogWarning($"{name}: ThrowableWeaponController가 없어 {throwable.name}을 해금하지 못했습니다.", this);
            return;
        }

        // 선택지 단계에서 이미 걸렀지만, 슬롯 상태는 Apply 시점이 최종이므로 여기서 한 번 더 확인한다.
        if (controller.Owns(throwable.id)) return;

        int slotIndex = controller.FirstEmptySlotIndex();
        if (slotIndex < 0)
        {
            Debug.LogWarning($"{name}: 투척무기 슬롯이 모두 차 있어 {throwable.name}을 해금하지 못했습니다.", this);
            return;
        }

        controller.Equip(slotIndex, throwable);
    }
}
