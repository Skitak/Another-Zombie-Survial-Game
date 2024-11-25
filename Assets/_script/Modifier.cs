using System;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

[Serializable, HideReferenceObjectPicker]
public abstract class Modifier
{
    [HideInInspector, DoNotSerialize] public bool isActive;
    [TitleGroup("@GetTitle()"), SuffixLabel("@GetValueSuffix()", overlay: true)] public float value;
    [TitleGroup("@GetTitle()"), EnumToggleButtons, HideLabel, PropertyOrder(3)] public ContextButtons contextButtons;
    public virtual bool IsValid() => (!IsConditional || condition.IsValid()) && isActive;
    public virtual float GetValue() => IsValid() ? value * (HasContext ? context.GetValue() / appliedPerContextValue : 1) : 0;
    public abstract Sprite GetValueSprite();
    #region context
    [ShowIf("HasContext"), TitleGroup("Context"), Min(0)]
    [Tooltip("Should NOT be used with percentage stat in my opinion")]
    public int appliedPerContextValue = 0;
    [TitleGroup("Context"), ShowIf("HasContext")]
    public ContextStat context;
    protected bool HasContext { get => contextButtons.HasFlag(ContextButtons.CONTEXT); }
    protected bool IsConditional { get => contextButtons.HasFlag(ContextButtons.CONDITION); }
    [ShowIf("IsConditional"), TitleGroup("Condition"), PropertyOrder(6)] public bool hideWhenConditionInvalid = false;
    [ShowIf("IsConditional"), TitleGroup("Condition"), PropertyOrder(7)] public ContextCondition condition;
    #endregion

    #region label
    [SerializeField, HideLabel, HideInInspector, PropertyOrder(11), OnInspectorGUI("UpdateLabelPreview"), TitleGroup("Label")]
    [InlineButton("@useCustomLabel = !useCustomLabel", "@useCustomLabel?\"Custom\":\"Generated\"")]
    string label = "";
    protected virtual string GetValueSuffix() => "";
    protected abstract string GetValueName();
    protected abstract string GetTitle();
    public virtual string GetLabel(bool withContext = true)
    {
        if (useCustomLabel)
            return label;
        if (withContext && hideWhenConditionInvalid && !IsValid())
            return "";
        bool displayConditional = IsConditional && !(withContext && condition.IsValid());
        string prefix = value > 0 ? "+" : "";
        string valueStr = $"{value}{GetValueSuffix()}";
        string baseLabel = $"{prefix}{valueStr} {GetValueName()}";
        prefix = "every ";
        if (HasContext && appliedPerContextValue != 1)
            prefix += $"{appliedPerContextValue} ";
        string contextStr = HasContext ? $" {prefix}{context.GetLabel(withContext, appliedPerContextValue != 1)}" : "";
        string conStr = displayConditional ? $" {condition.GetLabel(withContext)}" : "";
        return $"{baseLabel}{contextStr}{conStr}.";
    }
    void UpdateLabelPreview() => label = useCustomLabel ? label : GetLabel(false);
    [SerializeField, HideInInspector] bool useCustomLabel;
    #endregion

    #region estimation
    [ShowInInspector, PropertyOrder(12), DisplayAsString(false), OnInspectorGUI("GetEstimatedValue"), PropertySpace(SpaceAfter = 20)]
    protected int estimatedValue;
    public virtual int GetEstimatedValue() => 0;
    #endregion

    #region initialization
    public virtual void ApplyModifier()
    {
        isActive = true;
        if (HasContext)
            context.Initialize();
        if (IsConditional)
            condition.Initialize();
    }
    public virtual Modifier Clone()
    {
        Modifier clone = (Modifier)MemberwiseClone();
        clone.context = context != null ? context.Clone() : new();
        clone.condition = condition != null ? condition.Clone() : new();
        return clone;
    }
    [OnInspectorInit("InitContext")]
    void InitContext() { context ??= new ContextStatFlat(); condition ??= new(); }
    #endregion
}
