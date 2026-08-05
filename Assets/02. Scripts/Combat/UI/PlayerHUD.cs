using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 네트워크 : 플레이어 입장 시 Init - 이름/프로필(PlayerInfo)은 게임 종료까지 안 바뀌므로 1회 주입.
//   PlayerInfo는 네트워크로 전달되는 값이라 Sprite 참조 대신 profileIconId(인덱스)만 담고,
//   실제 Sprite는 여기서 profileIcons 배열로 로컬 해석한다.
// 전투 : PlayerStatsController.OnHpChanged / PlayerCombatContext.OnAugmentAcquired 구독.
//   둘 다 런타임에 바뀌는 값이라 값이 바뀔 때만 반응한다.
// PlayerHUD는 PlayerStatsController/PlayerCombatContext를 참조하지만, 반대로 그쪽은
// PlayerHUD(UI)를 전혀 모른다 - 전투 로직이 UI 존재를 전제하지 않도록 하기 위함.
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Image profileIMG;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI nameTXT;
    [SerializeField] private Sprite[] profileIcons;

    // 증강을 고르는 버튼이 아니라, 이미 획득한 증강을 아이콘으로 보여주고
    // 클릭하면 툴팁으로 설명을 보여주는 슬롯이다. 실제 선택 UI는 증강 개발자가 별도로 만든다.
    [SerializeField] private AugmentButton[] acquiredAugmentSlots;

    PlayerStatsController statsController;
    PlayerCombatContext combatContext;
    int nextAugmentSlotIndex;

    public void Init(PlayerInfo info, PlayerStatsController stats, PlayerCombatContext context)
    {
        nameTXT.text = info.playerName;
        if (info.profileIconId >= 0 && info.profileIconId < profileIcons.Length)
            profileIMG.sprite = profileIcons[info.profileIconId];

        statsController = stats;
        statsController.OnHpChanged += UpdateHpBar;
        UpdateHpBar(stats.Stats.currentHp, stats.Stats.maxHp); // 구독 시점 기준 현재값으로 1회 초기화

        combatContext = context;
        combatContext.OnAugmentAcquired += AddAcquiredAugmentIcon;
    }

    void OnDestroy()
    {
        if (statsController != null)
            statsController.OnHpChanged -= UpdateHpBar;
        if (combatContext != null)
            combatContext.OnAugmentAcquired -= AddAcquiredAugmentIcon;
    }

    void UpdateHpBar(float currentHp, float maxHp)
    {
        hpSlider.value = maxHp > 0f ? currentHp / maxHp : 0f;
    }

    void AddAcquiredAugmentIcon(AugmentData augment)
    {
        if (nextAugmentSlotIndex >= acquiredAugmentSlots.Length) return;

        acquiredAugmentSlots[nextAugmentSlotIndex].SetAugment(augment);
        nextAugmentSlotIndex++;
    }
}
