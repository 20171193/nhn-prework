using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using TMPro;
using UnityEngine;

// 증강 테스트용 디버그 UI. 우측 상단에 현재 Effective 스탯과 지금까지 선택한
// 증강 목록을 표시하고, 버튼 클릭 시 실제 증강처럼 PlayerCombatContext.AddModifier를
// 호출한다. IAugmentEffect를 구현하지 않고 직접 AddModifier를 부르는 이유는
// 이 UI 자체가 "증강 하나"가 아니라 여러 증강을 자유롭게 테스트하는 도구이기 때문.
// selectedAugmentsText는 나중에 네트워크 연동 시 "내 증강" 패널이 되고,
// 같은 방식으로 텍스트 패널 하나를 더 두면 "상대 증강" 표시도 그대로 재사용 가능하다.
//
// Player가 PhotonNetwork.Instantiate로 런타임에 스폰되면서 씬에 미리 배치해둔
// Player를 인스펙터에서 정적으로 연결하는 방식이 더 이상 유효하지 않다. 그래서
// combatContext가 비어있으면 스폰된 오브젝트 중 로컬 소유(IsMine) Player를 찾아 채운다.
public class AugmentTestUI : MonoBehaviour
{
    public PlayerCombatContext combatContext;
    public TMP_Text statsText;
    public TMP_Text selectedAugmentsText;

    readonly List<string> augmentOrder = new List<string>();
    readonly Dictionary<string, int> augmentCounts = new Dictionary<string, int>();

    void Update()
    {
        if (combatContext == null) FindLocalCombatContext();
        RefreshStatsDisplay();
    }

    void FindLocalCombatContext()
    {
        foreach (var ctx in FindObjectsByType<PlayerCombatContext>(FindObjectsSortMode.None))
        {
            var view = ctx.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
            {
                combatContext = ctx;
                break;
            }
        }
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
        if (combatContext == null) return; // 아직 로컬 Player가 스폰되지 않음

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
