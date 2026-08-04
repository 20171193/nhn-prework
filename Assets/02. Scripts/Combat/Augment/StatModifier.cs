// 증강 시스템이 건드릴 수 있는 스탯 종류.
// PlayerStats/WeaponStats/ProjectileStats에 흩어진 수치 중 실제로
// 증강 대상이 되는 것만 모아둔 식별자.
public enum StatType
{
    MoveSpeed,
    AttackRate,
    ProjectileSpeed,
    ProjectileDamage,
    ProjectileCount,
}

public enum StatOperation
{
    Add,
    Multiply,
}

// 증강 하나가 특정 스탯에 미치는 영향 하나.
// 여러 개가 쌓일 수 있고, 적용 순서는 (base + 모든 Add 합) * (모든 Multiply 곱).
[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public StatOperation operation;
    public float value;

    public StatModifier(StatType statType, StatOperation operation, float value)
    {
        this.statType = statType;
        this.operation = operation;
        this.value = value;
    }
}
