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

    [Header("Sprites")] 
    public Sprite bigIcon;
    public Sprite smallIcon;

    public abstract void Apply(PlayerCombatContext context);

    // 지금 이 플레이어에게 선택지로 내놓을 만한 증강인지. AugmentPool이 뽑기 전에 물어본다.
    // 스탯 증강처럼 언제 먹어도 의미가 있는 것은 그대로 두면 되고, 이미 갖고 있으면
    // 아무 효과가 없는 증강(투척무기 해금 등)만 재정의해서 걸러낸다.
    // context는 null일 수 있다 - 플레이어가 없는 씬(흐름 테스트)에서는 확인할 대상이 없다.
    public virtual bool IsOfferable(PlayerCombatContext context) => true;
}
