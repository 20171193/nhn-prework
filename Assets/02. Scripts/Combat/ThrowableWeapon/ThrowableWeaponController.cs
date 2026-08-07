using Photon.Pun;
using UnityEngine;

// 숫자키 1/2/3으로 발동하는 투척무기 3슬롯. 슬롯0은 룸 입장 시 CombatNetworkManager가
// PlayData의 기본 투척무기로 Equip(0, ...)을 호출해 채워준다. 슬롯1/2는 증강 등으로
// Equip()이 불려야 사용 가능하다(실제 호출부는 증강 개발자 몫).
// 어느 슬롯이든 에임 모드로 전환되면(PlayerWeaponFireController가 IsAiming을 보고
// 총 발사를 막음) 사거리 안에서 좌클릭으로 그 슬롯을 던진다 - PlayerCombatContext.Throw로
// RPC 전파되고, 각 클라이언트가 ThrowableObject를 로컬로 재생한다.
[RequireComponent(typeof(PlayerCombatContext))]
[RequireComponent(typeof(PhotonView))]
public class ThrowableWeaponController : MonoBehaviourPun
{
    public PlayerAimController aim;

    static readonly KeyCode[] SlotKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };
    const int SlotCount = 3;

    // 슬롯에 장착된 ThrowableWeaponData가 없을 때 쓰는 폴백 기본값.
    [SerializeField] float cooldown = 10f;
    [SerializeField] float throwRange = 6f;

    class ThrowableSlot
    {
        public bool unlocked;
        public float cooldownRemaining;
        public ThrowableWeaponData weaponData;
    }

    PlayerCombatContext combatContext;
    readonly ThrowableSlot[] slots = new ThrowableSlot[SlotCount];
    int activeSlotIndex = -1;
    // 던진 프레임에 activeSlotIndex를 바로 -1로 만들면, 같은 프레임에 PlayerWeaponFireController의
    // Update가 이 컨트롤러보다 나중에 돌 경우 IsAiming이 이미 꺼진 걸로 보여 총알까지 같이 나간다.
    // 실제 해제는 모든 Update가 끝난 뒤인 LateUpdate에서 하도록 미뤄서 이 프레임 동안은 계속 막는다.
    bool pendingAimEnd;

    public bool IsAiming => activeSlotIndex >= 0;
    // 에임 중인 슬롯 기준으로, 현재 마우스가 투척 사거리 안에 있는지. UI(원 색깔 등)가 참고한다.
    public bool OnRange { get; private set; }
    public float ThrowRange => IsAiming ? SlotRange(activeSlotIndex) : throwRange;

    // 슬롯 하나가 새로 장착/교체될 때(룸 입장 시 기본 투척무기, 증강으로 획득 등) 알려준다.
    // ThrowableWeaponUI가 이걸 구독해서 아이콘을 갱신한다.
    public event System.Action<int, ThrowableWeaponData> OnWeaponEquipped;

    void Awake()
    {
        combatContext = GetComponent<PlayerCombatContext>();

        for (int i = 0; i < SlotCount; i++)
            slots[i] = new ThrowableSlot();
    }

    void Update()
    {
        if (!photonView.IsMine || !combatContext.InputEnabled) return;

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i].cooldownRemaining > 0f)
                slots[i].cooldownRemaining = Mathf.Max(0f, slots[i].cooldownRemaining - Time.deltaTime);
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (Input.GetKeyDown(SlotKeys[i]))
            {
                ToggleAiming(i);
                break;
            }
        }

        if (IsAiming)
        {
            OnRange = Vector2.Distance(transform.position, aim.MouseWorldPosition) <= ThrowRange;

            if (Input.GetMouseButtonDown(0))
                TryThrow();
        }
    }

    void LateUpdate()
    {
        if (!pendingAimEnd) return;

        activeSlotIndex = -1;
        pendingAimEnd = false;
    }

    public bool IsSlotUnlocked(int slotIndex) => slots[slotIndex].unlocked;
    public ThrowableWeaponData GetWeaponData(int slotIndex) => slots[slotIndex].weaponData;
    public float GetCooldownRate(int slotIndex) => slots[slotIndex].cooldownRemaining / SlotCooldown(slotIndex);

    // 슬롯을 잠금해제하고 투척무기를 장착한다. 룸 입장 시 기본 투척무기(슬롯0)와, 증강으로
    // 추가 투척무기를 얻었을 때(슬롯1/2) 양쪽 다 이 메서드 하나로 처리한다.
    public void Equip(int slotIndex, ThrowableWeaponData data)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount || data == null) return;

        slots[slotIndex].unlocked = true;
        slots[slotIndex].weaponData = data;
        OnWeaponEquipped?.Invoke(slotIndex, data);
    }

    void ToggleAiming(int slotIndex)
    {
        if (!slots[slotIndex].unlocked) return;

        if (activeSlotIndex == slotIndex)
        {
            combatContext.ClearThrowablePreview();
            activeSlotIndex = -1;
            return;
        }

        if (slots[slotIndex].cooldownRemaining > 0f) return;

        activeSlotIndex = slotIndex;
        combatContext.SetThrowablePreview(slots[slotIndex].weaponData.id);
    }

    void TryThrow()
    {
        // 사거리 밖 클릭은 무시하고 에임 모드를 유지한다.
        if (!OnRange) return;

        var data = slots[activeSlotIndex].weaponData;
        Vector3 start = combatContext.weaponController.muzzle.position;
        combatContext.Throw(start, ClampedTarget(start), data.id);

        combatContext.ClearThrowablePreview();
        slots[activeSlotIndex].cooldownRemaining = SlotCooldown(activeSlotIndex);
        pendingAimEnd = true;
    }

    // ThrowRangeIndicator.DrawCurve와 동일한 "사거리 밖이면 경계로 클램프" 로직.
    Vector3 ClampedTarget(Vector3 start)
    {
        Vector2 toMouse = (Vector2)aim.MouseWorldPosition - (Vector2)start;
        return toMouse.magnitude > ThrowRange
            ? start + (Vector3)(toMouse.normalized * ThrowRange)
            : aim.MouseWorldPosition;
    }

    float SlotCooldown(int slotIndex) => slots[slotIndex].weaponData != null ? slots[slotIndex].weaponData.cooldown : cooldown;
    float SlotRange(int slotIndex) => slots[slotIndex].weaponData != null ? slots[slotIndex].weaponData.range : throwRange;
}
