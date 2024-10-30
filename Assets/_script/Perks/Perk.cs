using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPerk", menuName = "Perks", order = 0)]
public class Perk : SerializedScriptableObject
{
    public bool showInShop = true;
    public string title;
    [SerializeField] protected string label;
    [ShowIf("label", "")][SerializeField] bool showModifiersLabel = true;
    [ShowInInspector] Sprite sprite;
    public int maxApplications = int.MaxValue;
    public int price = 1000;
    public Rarity rarity;
    [ListDrawerSettings(DraggableItems = false, ShowIndexLabels = true)]
    [OdinSerialize]
    [NonSerialized]
    public List<StatModifier> statModifiers;
    public List<MutationType> mutations;
    public virtual bool CanBeApplied() => true;
    public virtual string GetLabel(bool showContextData = true)
    {
        if (!showModifiersLabel)
            return label;
        string endLabel = "";
        if (label != "")
            endLabel += $"{label}\n";
        bool first = true;
        foreach (StatModifier modifier in statModifiers)
        {
            if (!first)
                endLabel += "\n";
            endLabel += modifier.GetLabel(showContextData);
            first = false;
        }
        return endLabel;

    }
    public virtual void ApplyUpgrades()
    {
        foreach (StatModifier modifier in statModifiers)
        {
            StatModifier clonedModifier = modifier.Clone();
            clonedModifier.Initialize();
            StatManager.ApplyStatModifier(clonedModifier);
        }
        foreach (MutationType mutation in mutations)
            StatManager.ApplyMutation(mutation);
    }
    public Sprite GetSprite() => sprite != null ? sprite : StatManager.instance.statDescriptions[statModifiers[0].stat].icon;
}

public enum Rarity { COMMON, UNCOMMON, RARE, LEGENDARY }
[Serializable]
public class StatModifier
{
    public StatType stat;
    public bool applyPercentageValue;
    public float value;
    [Tooltip("This allows the player to see this change even if he removes context dependant data.")]
    public bool appearsAsPermanent = true;
    [Tooltip("Should NOT be used with percentage stat in my opinion")]
    [Min(0)] public int appliedPerContextValue = 0;
    [HideIf("appliedPerContextValue", 0)]
    public ContextStat context;
    public bool isConditional = false;
    [ShowIf("isConditional")] public bool hideConditionIfValid = false;
    [ShowIf("isConditional")] public bool hideIfConditionInvalid = false;
    [ShowIf("isConditional")] public ContextCondition condition;
    public string GetLabel(bool withContext = true)
    {
        if (hideIfConditionInvalid && !IsValid() && withContext)
            return "";
        bool displayConditional = isConditional && !(hideConditionIfValid && condition.IsValid() && withContext);
        StatDescription description = StatManager.instance.statDescriptions[stat];
        string prefix = value > 0 ? "+" : "-";
        string baseLabel = $"{prefix}{description.ValueToString(value)} {description.displayedName}";
        prefix = "every ";
        if (appliedPerContextValue != 1)
            prefix += $"{appliedPerContextValue} ";
        string contextStr = appliedPerContextValue != 0 ? $" {prefix}{context.GetLabel(withContext, appliedPerContextValue != 1)}" : "";
        string conStr = displayConditional ? $" {condition}" : "";
        return $"{baseLabel}{contextStr}{conStr}.";
    }
    public StatModifier Clone() => new()
    {
        stat = this.stat,
        applyPercentageValue = this.applyPercentageValue,
        value = this.value,
        appearsAsPermanent = this.appearsAsPermanent,
        appliedPerContextValue = this.appliedPerContextValue,
        context = appliedPerContextValue != 0 ? context.Clone() : null,
        isConditional = this.isConditional,
        hideConditionIfValid = this.hideConditionIfValid,
        hideIfConditionInvalid = this.hideIfConditionInvalid,
        condition = isConditional ? this.condition.Clone() : null,
    };
    public void Initialize()
    {
        if (appliedPerContextValue != 0)
            context.Initialize();
        if (isConditional)
            condition.context.Initialize();
    }
    public bool IsValid() => !isConditional || condition.IsValid();
    public bool UseContextData() => isConditional || appliedPerContextValue != 0;
    public float GetValue()
    {
        if (!IsValid())
            return 0;
        float endValue = value;
        if (applyPercentageValue)
            endValue /= 100;
        if (appliedPerContextValue != 0)
            endValue *= context.GetValue() / appliedPerContextValue;
        return endValue;
    }
}

#region Context stat
[Serializable]
public class ContextStat
{
    [HideInInspector] public bool isInitialized;
    public virtual void Initialize() => isInitialized = true;
    public virtual ContextStat Clone() => null;
    public virtual int GetValue() => 0;
    public virtual string GetStatName(bool plural = true) => "";
    public virtual string GetLabel(bool withContext, bool plural)
    {
        string currentValue = withContext ? $"(current: {GetValue()})" : "";
        return $"{GetStatName(plural)}{currentValue}";
    }
}
public class ContextStatFlat : ContextStat
{
    public enum Stat { HEALTH, HEALTH_MISSING, AMMO, AMMO_MISSING, MONEY, COMBO, WAVE, HEADSHOT, KILL, RELOAD }
    public Stat stat;
    public override string GetStatName(bool plural = true)
    {
        string pluralStr = plural ? "s" : "";
        return stat switch
        {
            Stat.HEALTH => "health left",
            Stat.HEALTH_MISSING => "missing Health",
            Stat.AMMO => $"ammunition{pluralStr} left",
            Stat.AMMO_MISSING => $"missing ammunition{pluralStr}",
            Stat.MONEY => "money",
            Stat.COMBO => "combo",
            Stat.WAVE => $"wave{pluralStr}",
            Stat.HEADSHOT => $"headshot kill{pluralStr} since game started",
            Stat.KILL => $"zombie kill{pluralStr} since game started",
            Stat.RELOAD => $"reload{pluralStr} since game started",
            _ => "",
        };
    }
    public override int GetValue() => stat switch
    {
        Stat.HEALTH => Player.player.health,
        Stat.HEALTH_MISSING => Player.player.healthMax - Player.player.health,
        Stat.AMMO => Player.player.weapon.ammo,
        Stat.AMMO_MISSING => Player.player.weapon.ammoMax - Player.player.weapon.ammo,
        Stat.MONEY => MoneyManager.instance.GetMoney(),
        Stat.COMBO => ComboManager.instance.combo,
        Stat.WAVE => WaveManager.instance.GetWaveCount(),
        Stat.HEADSHOT => Zombie.headshots,
        Stat.KILL => Zombie.kills,
        Stat.RELOAD => Weapon.reloads,
        _ => 0,
    };
    public override ContextStat Clone() => new ContextStatFlat() { stat = stat };
}
public class ContextStatPercent : ContextStat
{
    public enum Stat { HEALTH, HEALTH_MISSING, AMMO, AMMO_MISSING }
    public Stat stat;
    public override int GetValue() => stat switch
    {
        Stat.HEALTH => (int)(Player.player.health / (float)Player.player.healthMax) * 100,
        Stat.HEALTH_MISSING => (((int)(Player.player.health / (float)Player.player.healthMax) * 100) - 100) * -1,
        Stat.AMMO => (int)(Player.player.weapon.ammo / (float)Player.player.weapon.ammoMax) * 100,
        Stat.AMMO_MISSING => (((int)(Player.player.weapon.ammo / (float)Player.player.weapon.ammoMax) * 100) - 100) * -1,
        _ => 0,
    };
    public override string GetStatName(bool plural = true)
    {
        string pluralStr = plural ? "s" : "";
        return stat switch
        {
            Stat.HEALTH => "% health left",
            Stat.HEALTH_MISSING => "% missing health",
            Stat.AMMO => $"% ammunition{pluralStr} left",
            Stat.AMMO_MISSING => $"% missing ammunition{pluralStr}",
            _ => "",
        };
    }
    public override ContextStat Clone() => new ContextStatPercent() { stat = stat };
}
public class ContextStatIncremental : ContextStat
{
    public enum Stat { WAVE, HEADSHOT, KILL, RELOAD }
    public Stat stat;
    int baseValue = 0;
    public override void Initialize()
    {
        base.Initialize();
        baseValue = GetCurrentValue();
    }
    public override int GetValue() => isInitialized ? (GetCurrentValue() - baseValue) : 0;
    int GetCurrentValue() => stat switch
    {
        Stat.WAVE => WaveManager.instance.GetWaveCount(),
        Stat.HEADSHOT => Zombie.headshots,
        Stat.KILL => Zombie.kills,
        Stat.RELOAD => Weapon.reloads,
        _ => 0,
    };
    public override string GetLabel(bool withContext, bool plural)
    {
        string suffix = "";
        if (isInitialized && withContext)
            suffix += $" since purchase (current:{GetValue()}).";
        else
            suffix += " after purchase.";

        return $"{GetStatName(plural)}{suffix}";
    }
    public override string GetStatName(bool plural = true)
    {
        string pluralStr = plural ? "s" : "";
        return stat switch
        {
            Stat.WAVE => $"wave{pluralStr}",
            Stat.HEADSHOT => $"headshot kill{pluralStr}",
            Stat.KILL => $"zombie kill{pluralStr}",
            Stat.RELOAD => $"reload{pluralStr}",
            _ => "",
        };
    }
    public override ContextStat Clone() => new ContextStatIncremental() { stat = stat };
}

#endregion
[Serializable]
public class ContextCondition
{
    public ContextStat context;
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
            Type.OVER_EQUALS => !IsValid() ? $"after the next{valStr(value)}{context.GetStatName(value != 1)}" : "",
            Type.UNDER => IsValid() ? $"for the next{valStr(value + 1)}{context.GetStatName(value + 1 != 1)}" : "",
            Type.UNDER_EQUAL => IsValid() ? $"for the next{valStr(value)}{context.GetStatName(value != 1)}" : "",
            _ => "default",
        };
    }

    public ContextCondition Clone() => new()
    {
        context = this.context,
        type = this.type,
        conditionValue = this.conditionValue,
        thresholdMin = this.thresholdMin,
        thresholdMax = this.thresholdMax,
    };
}