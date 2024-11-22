using System;
using Sirenix.OdinInspector;
using UnityEngine;

public enum ModificationKind { FLAT, PERCENT, TOTAL_PERCENT }
[Serializable, HideReferenceObjectPicker]
public class StatModifier : Modifier
{
    public StatType stat;
    [EnumToggleButtons, HideLabel, PropertyOrder(3)]
    public ModificationKind modificationKind;

    public override void ApplyModifier()
    {
        base.ApplyModifier();
        StatManager.ApplyStatModifier(this);
    }

    protected override string GetValueSuffix()
        => modificationKind == ModificationKind.FLAT ? StatManager.Descriptions[stat].GetSuffix() : "%";

    public override int GetEstimatedValue()
    {
        if (modificationKind == ModificationKind.FLAT)
            estimatedValue = (int)(StatManager.Descriptions[stat].estimatedValue * value);
        else
            estimatedValue = (int)(StatManager.Descriptions[stat].estimationPercent * value / 100);

        if (StatManager.Descriptions[stat].isNegative)
            estimatedValue *= -1;
        return estimatedValue;
    }
    protected override string GetValueName() => StatManager.Descriptions[stat].displayedName;
    public override Sprite GetValueSprite() => StatManager.Descriptions[stat].icon;
}

[Flags]
public enum ContextButtons
{
    CONDITION = 1 << 1,
    CONTEXT = 1 << 2,
}