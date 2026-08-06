using UnityEngine;

public enum AugmentTier
{
    Rare,
    Epic,
    Unique,
    Legendary,
}

// 모든 증강의 공통 부모. 선택 UI가 쓰는 표시 정보(이름/설명/티어)만 들고 있고,
// 실제 효과는 자식 클래스가 Apply()에서 정의한다.
// 전투 시스템(개발1)의 IAugmentEffect를 그대로 구현하므로
// PlayerCombatContext는 증강 종류를 하나도 몰라도 된다.
// 추상 클래스는 에셋으로 만들 수 없으므로 CreateAssetMenu는 자식 쪽에만 붙인다.
public abstract class AugmentData : ScriptableObject, IAugmentEffect, IHasId
{
    // 전투 데이터(Projectile 10000~/Weapon 20000~/Skill 30000~)와 같은 ID 체계, 40000번대.
    [Header("ID (40000~)")]
    public int id;
    public int Id => id;

    public string augmentName;
    [TextArea] public string description;
    public AugmentTier tier;

    public abstract void Apply(PlayerCombatContext context);
}
