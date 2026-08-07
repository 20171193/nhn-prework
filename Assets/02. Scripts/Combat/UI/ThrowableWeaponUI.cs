using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 투척무기 슬롯 하나를 표시하는 UI. 로컬 플레이어 전용(상대방 쪽은 표시 안 함) -
// 슬롯당 하나씩 씬에 배치되고, CombatNetworkManager가 스폰 시 Init(controller, slotIndex)으로 연결해준다.
// 룸 입장 시 기본 투척무기 장착, 증강으로 투척무기 획득 둘 다 ThrowableWeaponController.OnWeaponEquipped
// 이벤트 하나로 들어오므로 같은 핸들러에서 아이콘/fill 이미지를 갱신한다.
public class ThrowableWeaponUI : MonoBehaviour
{
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private Image slotLockImage;
    ThrowableWeaponController throwableWeapon;
    int slotIndex;

    public void Init(ThrowableWeaponController controller, int slotIndex)
    {
        weaponIconImage.sprite =null;
        fillImage.sprite = null;
        cooldownSlider.value = 0f;

        slotLockImage.gameObject.SetActive(true);

        throwableWeapon = controller;
        this.slotIndex = slotIndex;

        throwableWeapon.OnWeaponEquipped += HandleWeaponEquipped;

        // Init이 불릴 때 이미 장착돼 있는 경우(예: 슬롯0 기본 투척무기는 CombatNetworkManager가
        // 이 UI를 연결하기 전에 Equip을 먼저 호출함) OnWeaponEquipped 이벤트를 놓치므로,
        // 여기서 현재 상태를 한 번 직접 반영해준다.
        var current = throwableWeapon.GetWeaponData(slotIndex);
        if (current != null)
        {
            slotLockImage.gameObject.SetActive(false);
            SetIcon(current.icon);
        }
    }

    void OnDestroy()
    {
        if (throwableWeapon != null)
            throwableWeapon.OnWeaponEquipped -= HandleWeaponEquipped;
    }

    void HandleWeaponEquipped(int equippedSlotIndex, ThrowableWeaponData data)
    {
        if (equippedSlotIndex != slotIndex) return;

        slotLockImage.gameObject.SetActive(false);
        SetIcon(data.icon);
    }

    void SetIcon(Sprite icon)
    {
        weaponIconImage.sprite = icon;
        fillImage.sprite = icon;
    }

    void Update()
    {
        if (throwableWeapon == null) return;

        float rate = throwableWeapon.GetCooldownRate(slotIndex);
        if (rate > 0f)
        {
            cooldownSlider.value = rate;
        }
    }
}
