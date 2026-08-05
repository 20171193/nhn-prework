using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// 증강 테스트용 디버그 UI. 우측 상단에 현재 Effective 스탯과 지금까지 선택한
// 증강 목록을 표시하고, 버튼 클릭 시 실제 증강처럼 PlayerCombatContext.AddModifier를
// 호출한다. IAugmentEffect를 구현하지 않고 직접 AddModifier를 부르는 이유는
// 이 UI 자체가 "증강 하나"가 아니라 여러 증강을 자유롭게 테스트하는 도구이기 때문.
// selectedAugmentsText는 나중에 네트워크 연동 시 "내 증강" 패널이 되고,
// 같은 방식으로 텍스트 패널 하나를 더 두면 "상대 증강" 표시도 그대로 재사용 가능하다.
public class AugmentTestUI : MonoBehaviour
{
    public PlayerCombatContext combatContext;
    public TMP_Text statsText;
    public TMP_Text selectedAugmentsText;

    readonly List<string> augmentOrder = new List<string>();
    readonly Dictionary<string, int> augmentCounts = new Dictionary<string, int>();

    void Update()
    {
        RefreshStatsDisplay();
    }

    void RefreshStatsDisplay()
    {
        if (combatContext == null || statsText == null) return;

        statsText.text =
            $"Move Speed : {combatContext.EffectiveMoveSpeed:F1}\n" +
            $"Attack Per Second : {combatContext.EffectiveAttackRate:F2}\n" +
            $"Projectile Speed : {combatContext.EffectiveProjectileSpeed:F1}\n" +
            $"Projectile Damage : {combatContext.EffectiveProjectileDamage:F1}\n" +
            $"Projectile Count : {combatContext.EffectiveProjectileCount}";
    }

    public void AddProjectileCount()
    {
        Apply("Projectile Count +1", new StatModifier(StatType.ProjectileCount, StatOperation.Add, 1f));
    }

    public void AddProjectileSpeed()
    {
        Apply("Projectile Speed +2", new StatModifier(StatType.ProjectileSpeed, StatOperation.Add, 2f));
    }

    public void AddAttackRate()
    {
        Apply("Attack Speed +0.5", new StatModifier(StatType.AttackRate, StatOperation.Add, 0.5f));
    }

    public void AddMoveSpeed()
    {
        Apply("Move Speed +5", new StatModifier(StatType.MoveSpeed, StatOperation.Add, 5f));
    }

    void Apply(string label, StatModifier modifier)
    {
        combatContext.AddModifier(modifier);

        if (!augmentCounts.ContainsKey(label))
        {
            augmentCounts[label] = 0;
            augmentOrder.Add(label);
        }
        augmentCounts[label]++;

        RefreshAugmentLog();
    }

    void RefreshAugmentLog()
    {
        if (selectedAugmentsText == null) return;

        var sb = new StringBuilder();
        foreach (var label in augmentOrder)
        {
            int count = augmentCounts[label];
            sb.AppendLine(count > 1 ? $"{label} x{count}" : label);
        }

        selectedAugmentsText.text = sb.ToString();
    }
}
