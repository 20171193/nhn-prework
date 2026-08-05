using UnityEngine;

// 스탯 몇 개를 얼마나 건드리는지만으로 표현되는 증강을 코드 없이 에셋으로 만들기 위한 것.
// 인스펙터에서 이름/설명/StatModifier 목록만 채우면 하나의 증강이 완성된다.
[CreateAssetMenu(fileName = "AugmentDefinition", menuName = "Combat/Augment Definition")]
public class AugmentDefinition : ScriptableObject, IAugmentEffect
{
    public string displayName;
    [TextArea] public string description;
    public StatModifier[] modifiers;

    public void Apply(PlayerCombatContext context)
    {
        foreach (var modifier in modifiers)
            context.AddModifier(modifier);
    }
}
