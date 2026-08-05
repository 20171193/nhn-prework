using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatAugmentData", menuName = "Augments/Stat Augment")]
public class StatAugmentData : AugmentData
{
    public List<StatModifier> modifiers = new List<StatModifier>();

    public override void Apply(PlayerCombatContext context)
    {
        foreach (var modifier in modifiers)
            context.AddModifier(modifier);
    }
}
