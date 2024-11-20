using System;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

public enum ModificationKind { FLAT, PERCENT, TOTAL_PERCENT }
[Serializable, HideReferenceObjectPicker]
public class StatModifier
{
    [HideInInspector, DoNotSerialize] public bool isActive;
    [SerializeField, ShowIf("@IsConditional || HasContext"), Tooltip("Will always show the value in the pause menu, even if context is removed from preview.")]
    bool isPermanent;
    public StatType stat;
    [SuffixLabel("@GetSuffix()", overlay: true)]
    public float value;
    public bool IsPermanent { get => !(IsConditional || HasContext) || isPermanent; }
    [EnumToggleButtons, HideLabel, PropertyOrder(3)]
    public ModificationKind modificationKind;
    [EnumToggleButtons, HideLabel, PropertyOrder(3)]
    public ContextButtons contextButtons;
    [ShowIfGroup("HasContext"), PropertyOrder(5), BoxGroup("HasContext/Context"), Min(0)]
    [Tooltip("Should NOT be used with percentage stat in my opinion")]
    public int appliedPerContextValue = 0;
    [PropertyOrder(6), BoxGroup("HasContext/Context")]
    public ContextStat context;
    bool HasContext { get => contextButtons.HasFlag(ContextButtons.CONTEXT); }
    bool IsConditional { get => contextButtons.HasFlag(ContextButtons.CONDITION); }
    [PropertyOrder(8), ShowIf("IsConditional")]
    public ContextCondition condition;
    public bool IsValid() => (!IsConditional || condition.IsValid()) && isActive;
    public float GetValue()
    {
        if (!IsValid())
            return 0;
        float endValue = value;
        if (HasContext)
            endValue *= context.GetValue() / appliedPerContextValue;
        return endValue;
    }

    #region label
    string GetSuffix() => modificationKind == ModificationKind.FLAT ? StatManager.Descriptions[stat].GetSuffix() : "%";
    public string GetLabel(bool withContext = true)
    {
        if (useCustomLabel)
            return label;
        if (withContext && isPermanent && !IsValid())
            return "";
        bool displayConditional = IsConditional && !(withContext && isPermanent && condition.IsValid());
        StatDescription description = StatManager.Descriptions[stat];
        string prefix = value > 0 ? "+" : "";
        string valueStr = $"{value}{GetSuffix()}";
        string baseLabel = $"{prefix}{valueStr} {description.displayedName}";
        prefix = "every ";
        if (HasContext && appliedPerContextValue != 1)
            prefix += $"{appliedPerContextValue} ";
        string contextStr = HasContext ? $" {prefix}{context.GetLabel(withContext, appliedPerContextValue != 1)}" : "";
        string conStr = displayConditional ? $" {condition.GetLabel(withContext)}" : "";
        return $"{baseLabel}{contextStr}{conStr}.";
    }
    [SerializeField, HideInInspector, PropertyOrder(11), OnInspectorGUI("UpdateLabelPreview")]
    [InlineButton("@useCustomLabel = !useCustomLabel", "@useCustomLabel?\"Custom\":\"Generated\"")]
    string label = "";
    void UpdateLabelPreview() => label = useCustomLabel ? label : GetLabel(false);
    [SerializeField, HideInInspector] bool useCustomLabel;
    #endregion

    #region preview


    [ShowInInspector, PropertyOrder(12), DisplayAsString(false), OnInspectorGUI("GetEstimatedValue"),]
    int estimatedValue;
    public int GetEstimatedValue()
    {
        if (modificationKind == ModificationKind.FLAT)
            estimatedValue = (int)(StatManager.Descriptions[stat].estimatedValue * value);
        else
            estimatedValue = (int)(StatManager.Descriptions[stat].estimationPercent * value / 100);

        if (StatManager.Descriptions[stat].isNegative)
            estimatedValue *= -1;
        return estimatedValue;
    }
    #endregion
    #region initialization
    public void Initialize()
    {
        isActive = true;
        if (HasContext)
            context.Initialize();
        if (IsConditional)
            condition.Initialize();
    }
    public StatModifier Clone() => new()
    {
        stat = stat,
        contextButtons = contextButtons,
        isPermanent = isPermanent,
        value = value,
        modificationKind = modificationKind,
        appliedPerContextValue = appliedPerContextValue,
        context = context.Clone(),
        condition = condition.Clone(),
    };
    [OnInspectorInit("InitContext")]
    void InitContext() { context ??= new ContextStatFlat(); condition ??= new(); }
    #endregion
}

[Flags]
public enum ContextButtons
{
    CONDITION = 1 << 1,
    CONTEXT = 1 << 2,
}