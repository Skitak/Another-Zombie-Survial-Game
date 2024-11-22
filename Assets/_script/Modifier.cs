using System;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

[Serializable, HideReferenceObjectPicker]
public abstract class Modifier
{
    [SerializeField, ShowIf("@IsConditional || HasContext"), Tooltip("Will always show the value in the pause menu, even if context is removed from preview.")]
    protected bool isPermanent;
    public bool IsPermanent { get => !(IsConditional || HasContext) || isPermanent; }
    [HideInInspector, DoNotSerialize] public bool isActive;
    [SuffixLabel("@GetValueSuffix()", overlay: true)] public float value;
    [EnumToggleButtons, HideLabel, PropertyOrder(3)] public ContextButtons contextButtons;
    public virtual bool IsValid() => (!IsConditional || condition.IsValid()) && isActive;
    public virtual float GetValue() => IsValid() ? value * (HasContext ? context.GetValue() / appliedPerContextValue : 1) : 0;
    public abstract Sprite GetValueSprite();
    #region context
    [ShowIfGroup("HasContext"), PropertyOrder(5), BoxGroup("HasContext/Context"), Min(0)]
    [Tooltip("Should NOT be used with percentage stat in my opinion")]
    public int appliedPerContextValue = 0;
    [PropertyOrder(6), BoxGroup("HasContext/Context")]
    public ContextStat context;
    protected bool HasContext { get => contextButtons.HasFlag(ContextButtons.CONTEXT); }
    protected bool IsConditional { get => contextButtons.HasFlag(ContextButtons.CONDITION); }
    [PropertyOrder(8), ShowIf("IsConditional")]
    public ContextCondition condition;
    #endregion

    #region label
    [SerializeField, HideInInspector, PropertyOrder(11), OnInspectorGUI("UpdateLabelPreview")]
    [InlineButton("@useCustomLabel = !useCustomLabel", "@useCustomLabel?\"Custom\":\"Generated\"")]
    string label = "";
    protected virtual string GetValueSuffix() => "";
    protected abstract string GetValueName();
    public virtual string GetLabel(bool withContext = true)
    {
        if (useCustomLabel)
            return label;
        if (withContext && isPermanent && !IsValid())
            return "";
        bool displayConditional = IsConditional && !(withContext && isPermanent && condition.IsValid());
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
    [ShowInInspector, PropertyOrder(12), DisplayAsString(false), OnInspectorGUI("GetEstimatedValue")]
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
