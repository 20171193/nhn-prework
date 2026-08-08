// 증강 시스템이 건드릴 수 있는 스탯 종류.
// PlayerStats/WeaponStats/ProjectileStats에 흩어진 수치 중 실제로
// 증강 대상이 되는 것만 모아둔 식별자.
// 새 항목은 반드시 끝에 추가한다 - 중간에 끼워넣으면 이미 만들어진 증강 에셋의
// statType이 정수로 직렬화되어 있어 다른 스탯을 가리키게 된다.
public enum StatType
{
    MoveSpeed,
    AttackRate,
    ProjectileSpeed,
    ProjectileDamage,
    ProjectileCount,
    ThrowableRange,    // 투척 가능 거리(에임 사거리). ThrowableWeaponData.range 기준.
    ThrowableCooldown, // 투척 후 재사용 대기시간. 줄이는 쪽이므로 Multiply 1 미만을 쓴다.
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
