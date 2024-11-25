using System;
using Sirenix.OdinInspector;
using UnityEngine;

public enum ModificationKind { FLAT, PERCENT, TOTAL_PERCENT }
[Serializable, HideReferenceObjectPicker]
public class StatModifier : Modifier
{
    [TitleGroup("Condition"), PropertyOrder(5), SerializeField, ShowIf("@IsConditional || HasContext"), Tooltip("Will always show the value in the pause menu, even if context is removed from preview.")]
    protected bool isPermanent;
    public bool IsPermanent { get => !(IsConditional || HasContext) || isPermanent; }
    [TitleGroup("@GetTitle()"), PropertyOrder(1)] public StatType stat;
    [EnumToggleButtons, HideLabel, PropertyOrder(2), TitleGroup("@GetTitle()")]
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
    protected override string GetTitle() => $"Stat : {stat}";
}

[Flags]
public enum ContextButtons
{
    CONDITION = 1 << 1,
    CONTEXT = 1 << 2,
}