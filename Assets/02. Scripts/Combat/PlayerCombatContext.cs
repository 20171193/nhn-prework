using System.Collections.Generic;
using UnityEngine;

// 증강 시스템이 실제로 붙는 지점. PlayerStats/WeaponStats/ProjectileStats의
// base 값은 절대 직접 건드리지 않고, 증강은 AddModifier()로 목록에만 쌓는다.
// 스탯이 바뀔 수 있는 시점은 "증강 선택" 하나뿐이므로(전투 중에는 base도
// modifier도 변하지 않음) 매 프레임 재계산하지 않고, 그 시점에만 미리 계산해
// Effective* 값으로 캐싱해둔다. 이동/조준/발사 등 실제 로직은 이 값을 그대로 읽어서 쓴다.
public class PlayerCombatContext : MonoBehaviour
{
    public PlayerStatsController playerStatsController;
    public WeaponController weaponController;
    public Collider2D hitCollider; // 이 플레이어를 맞힐 수 있는 콜라이더. 자신이 쏜 발사체가 이걸 무시하도록 넘겨줄 때 씀

    readonly List<StatModifier> modifiers = new List<StatModifier>();

    public float EffectiveMoveSpeed { get; private set; }
    public float EffectiveAttackRate { get; private set; }
    public float EffectiveProjectileSpeed { get; private set; }
    public float EffectiveProjectileDamage { get; private set; }
    public int EffectiveProjectileCount { get; private set; }

    void Start()
    {
        Recalculate();
    }

    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
        Recalculate();
    }

    void Recalculate()
    {
        var playerStats = playerStatsController.Stats;
        var weaponStats = weaponController.Stats;
        var projectileStats = weaponController.ProjectileStats;

        EffectiveMoveSpeed = Calculate(playerStats.moveSpeed, StatType.MoveSpeed);
        EffectiveAttackRate = Calculate(weaponStats.attackRate, StatType.AttackRate);
        EffectiveProjectileSpeed = Calculate(projectileStats.speed, StatType.ProjectileSpeed);
        EffectiveProjectileDamage = Calculate(projectileStats.damage, StatType.ProjectileDamage);
        EffectiveProjectileCount = Mathf.Max(1, Mathf.RoundToInt(Calculate(weaponStats.projectileCount, StatType.ProjectileCount)));
    }

    float Calculate(float baseValue, StatType statType)
    {
        float sumOfAdds = 0f;
        float productOfMultiplies = 1f;

        foreach (var modifier in modifiers)
        {
            if (modifier.statType != statType) continue;

            if (modifier.operation == StatOperation.Add)
                sumOfAdds += modifier.value;
            else
                productOfMultiplies *= modifier.value;
        }

        return (baseValue + sumOfAdds) * productOfMultiplies;
    }
}
