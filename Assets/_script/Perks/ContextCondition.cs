using System;
using System.Collections.Generic;
using System.Linq;
using Asmos.Bus;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable, HideReferenceObjectPicker]
public class ContextCondition
{
    public enum WhenConditionValid { ALL, ANY }
    [EnumToggleButtons]
    [HideIf("@conditions.Count < 2")] public WhenConditionValid whenConditionValid;
    [SerializeField, Tooltip("Condition will listen for updates, and remain valid if is valid once.")]
    bool remainsValid;
    [SerializeField] List<Condition> conditions = new();
    bool hasBeenValidated;
    void CheckValidity(params object[] args)
    {
        if (IsValid() && remainsValid && !hasBeenValidated)
        {
            hasBeenValidated = true;
            foreach (var condition in conditions)
                condition.context.StopListening(CheckValidity);
        }
    }
    public bool IsValid()
    {
        if (remainsValid && hasBeenValidated)
            return true;
        else if (whenConditionValid == WhenConditionValid.ANY)
            return conditions.Any(x => x.IsValid());
        else
            return !conditions.Any(x => !x.IsValid());
    }
    #region label
    public string GetLabel(bool withContext = true)
    {
        if (useCustomLabel)
            return label;
        bool first = true;
        string generatedLabel = "";
        foreach (var condition in conditions)
        {
            if (!first)
                generatedLabel += " and ";
            generatedLabel += condition.GetLabel(withContext);
            first = false;
        }
        return generatedLabel;
    }
    [SerializeField, HideInInspector, LabelWidth(70), PropertyOrder(11), OnInspectorGUI("UpdateLabelPreview")]
    [InlineButton("@useCustomLabel = !useCustomLabel", "@useCustomLabel?\"Custom\":\"Generated\"")]
    string label = "";
    void UpdateLabelPreview() => label = useCustomLabel ? label : GetLabel(false);
    [SerializeField, HideInInspector] bool useCustomLabel;
    public void Listen(Bus.GenericDelegate onUpdate)
    {
        foreach (var condition in conditions)
            condition.context.Listen(onUpdate);
    }
    public void StopListening(Bus.GenericDelegate onUpdate)
    {
        foreach (var condition in conditions)
            condition.context.StopListening(onUpdate);
    }
    #endregion
    #region initialization
    public void Initialize()
    {
        foreach (var condition in conditions)
        {
            condition.context.Initialize();
            if (remainsValid)
                condition.context.Listen(CheckValidity);
        }
    }
    [OnInspectorInit("InitList")]
    void InitList()
    {
        conditions ??= new();
        if (conditions.Count == 0)
            conditions.Add(new());
    }
    public ContextCondition Clone()
    {
        ContextCondition clone = (ContextCondition)MemberwiseClone();
        if (conditions == null) return clone;
        List<Condition> clonedConditions = new();
        foreach (var condition in conditions)
            clonedConditions.Add(condition.Clone());
        clone.conditions = clonedConditions;
        return clone;
    }
    #endregion
}

[Serializable, HideReferenceObjectPicker]
public class Condition
{
    public ContextStat context = new ContextStatFlat();
    public Type type;
    [HideIf("type", Type.THRESHOLD)] public int conditionValue;
    [ShowIf("type", Type.THRESHOLD)] public int thresholdMin;
    [ShowIf("type", Type.THRESHOLD)] public int thresholdMax;
    public enum Type { THRESHOLD, OVER, OVER_EQUALS, UNDER, UNDER_EQUAL, EQUALS, EQUALS_MODULO }
    public bool IsValid()
    {
        return type switch
        {
            Type.THRESHOLD => context.GetValue() >= thresholdMin && context.GetValue() <= thresholdMax,
            Type.OVER => context.GetValue() > conditionValue,
            Type.OVER_EQUALS => context.GetValue() >= conditionValue,
            Type.UNDER => context.GetValue() < conditionValue,
            Type.UNDER_EQUAL => context.GetValue() <= conditionValue,
            Type.EQUALS => context.GetValue() == conditionValue,
            Type.EQUALS_MODULO => (context.GetValue() % conditionValue) == 0,
            _ => false,
        };
    }
    public string GetLabel(bool withContext)
    {
        if (context == null) return "";
        if (context.GetType() == typeof(ContextStatIncremental))
        {
            string incrementalString = IncrementalString(withContext);
            if (incrementalString != "default")
                return incrementalString;
        }
        string conditionStr = $"{conditionValue}";
        string thresholdMinStr = $"{thresholdMin}";
        string thresholdMaxStr = $"{thresholdMax}";
        if (context.GetType() == typeof(ContextStatPercent))
        {
            conditionStr = $"{conditionValue}%";
            thresholdMinStr = $"{thresholdMin}%";
            thresholdMaxStr = $"{thresholdMax}%";
        }
        string prefix = $"when {context.GetStatName(true)} ";
        string contextStr = withContext ? $"(current: {context.GetValue()})" : "";
        string conditionTypeStr = type switch
        {
            Type.THRESHOLD => $"is between {thresholdMinStr} and {thresholdMaxStr}",
            Type.OVER => $"is over {conditionStr}",
            Type.OVER_EQUALS => $"is over or equals {conditionStr}",
            Type.UNDER => $"is under {conditionStr}",
            Type.UNDER_EQUAL => $"is under or equals {conditionStr}",
            Type.EQUALS => $"equals {conditionStr}",
            Type.EQUALS_MODULO => $"is a multiple of {conditionStr}",
            _ => "",
        };
        return $"{prefix}{conditionTypeStr}{contextStr}";
    }
    string IncrementalString(bool withContext)
    {
        int value = conditionValue;
        if (withContext)
            value -= context.GetValue();
        string valStr(int val) => val != 1 ? $" {val} " : " ";
        return type switch
        {
            // Type.THRESHOLD => $"when {contextStat} is between {thresholdMinStr} and {thresholdMaxStr}",
            Type.OVER => !IsValid() ? $"after the next{valStr(value + 1)}{context.GetStatName(value + 1 != 1)}" : "",
            Type.OVER_EQUALS => !IsValid() ? $"after the next{valStr(value)}{context.GetStatName(value != 1)}" : "slkjfq",
            Type.UNDER => IsValid() ? $"for the next{valStr(value)}{context.GetStatName(value != 1)}" : "",
            Type.UNDER_EQUAL => IsValid() ? $"for the next{valStr(value + 1)}{context.GetStatName(value + 1 != 1)}" : "",
            _ => "default",
        };

    }
    [OnInspectorInit("InitContext")] void InitContext() => context ??= new ContextStatFlat();
    public Condition Clone()
    {
        Condition clone = (Condition)MemberwiseClone();
        clone.context = context.Clone();
        return clone;
    }

}