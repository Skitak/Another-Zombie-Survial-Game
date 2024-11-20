using System.Collections.Generic;
using Asmos.Bus;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;
public class StatManager : SerializedMonoBehaviour
{
    public static StatManager instance;
    public List<StatKit> statKits;
    static Dictionary<StatType, StatDescription> descriptions;
    public static Dictionary<StatType, StatDescription> Descriptions
    {
        get
        {
            descriptions ??= Resources.Load<StatDescriptionsHolder>("Stat descriptor").statDescriptions;
            return descriptions;
        }
    }
    [HideInInspector] Dictionary<StatType, Stat> stats = new();
    void Awake()
    {
        instance = this;
        foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            stats[statType] = new(statType);

        var timer = new Asmos.Timers.Timer(2f);
        timer.OnTimerEnd += () =>
        {
            Bus.PushData("HEALTH_MAX", stats[StatType.HEALTH_MAX].GetValue());
            Bus.PushData("MAGAZINE_SIZE", stats[StatType.MAGAZINE_SIZE].GetValue());
            timer.ResetPlay();
        };
        timer.Play();
    }
    void Start()
    {
        foreach (StatType stat in stats.Keys)
            Bus.PushData(stat.ToString(), stats[stat].GetValue());

    }
    public static float Get(StatType type, bool addContextualModifier = true) =>
        Mathf.Clamp(instance.stats[type].GetValue(addContextualModifier),
        Descriptions[type].min,
        Descriptions[type].max);
    public static Stat GetStat(StatType type) => instance.stats[type];
    public static void Subscribe(StatType type, System.Action<float> action) =>
        Bus.Subscribe(type.ToString(), (o) => action((float)o[0]));
    public static void ApplyStatModifier(StatModifier modifier) =>
        instance.stats[modifier.stat].AddModifier(modifier);
    public static void ApplyMutation(MutationType mutation)
    {

    }
    public void AddStatkit(StatKit statKit)
    {
        if (statKits.Contains(statKit))
            return;
        statKits.Add(statKit);
        foreach (var stat in statKit.kit.Keys)
        {
            stats[stat].baseFlat += statKit.kit[stat].flat;
            stats[stat].basePercent += statKit.kit[stat].percent;
            Bus.PushData(stat.ToString(), stats[stat].GetValue());
        }
    }
    public void RemoveStatKit(StatKit statKit)
    {
        if (!statKits.Contains(statKit))
            return;
        statKits.Remove(statKit);
        foreach (var stat in statKit.kit.Keys)
        {
            stats[stat].baseFlat -= statKit.kit[stat].flat;
            stats[stat].basePercent -= statKit.kit[stat].percent;
            Bus.PushData(stat.ToString(), stats[stat].GetValue());
        }
    }
}
public class Stat
{
    public float baseFlat;
    public int basePercent;
    public StatType statType;
    public StatDescription description;
    public Stat(StatType statType)
    {
        this.statType = statType;
        this.description = StatManager.Descriptions[statType];
    }
    public List<StatModifier> modifiers = new();
    #region Value calculus
    public float GetValue(bool addContextualModifier = true)
    {
        float flatValue = GetFlatValue(addContextualModifier);
        float percentValue = GetPercentValue(addContextualModifier);
        float value;
        if (StatManager.Descriptions[statType].isPercent)
            value = percentValue;
        else if (percentValue > 0)
            value = flatValue * (percentValue / 100 + 1);
        else
            value = flatValue / (-percentValue / 100 + 1);
        value = ApplyTotalModifiers(addContextualModifier, value);
        return value;
    }
    public float GetFlatValue(bool addContextualModifier = true)
    {
        float value = baseFlat;
        var validModifiers = modifiers.Where(x => ShouldApplyModifier(x, addContextualModifier, ModificationKind.FLAT));
        foreach (StatModifier modifier in validModifiers)
            value += modifier.GetValue();
        return value;
    }
    public int GetPercentValue(bool addContextualModifier = true)
    {
        int value = basePercent;
        var validModifiers = modifiers.Where(x => ShouldApplyModifier(x, addContextualModifier, ModificationKind.PERCENT));
        foreach (StatModifier modifier in validModifiers)
            value += (int)modifier.GetValue();
        return value;
    }
    float ApplyTotalModifiers(bool addContextualModifier, float value)
    {
        var validModifiers = modifiers.Where(x => ShouldApplyModifier(x, addContextualModifier, ModificationKind.TOTAL_PERCENT));
        foreach (StatModifier modifier in validModifiers)
            value *= modifier.GetValue() / 100 + 1;

        return Mathf.Max(value, 0);
    }
    #endregion

    #region Modifiers
    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
    }
    bool ShouldApplyModifier(StatModifier modifier, bool addContextualModifier, ModificationKind kind)
    {
        if (modifier.modificationKind != kind)
            return false;
        if (!modifier.IsPermanent && !addContextualModifier)
            return false;
        return true;
    }
    #endregion
}

public enum ContextStatType { HEALTH, AMMO, MONEY, COMBO, WAVE, HEADSHOT, KILL, RELOAD }
public enum MutationType
{
    WEAPON_AUTOMATIC,
    GRENADES_ATTRACT,
    RELOADING_EXPLOSION,
    EXPLOSION_CRIT,
    BULLET_PIERCING,
    RELOAD_ONE_BULLET,
    FIRE_WHILE_RUNNING
}
public struct MutationDescription
{
    public bool baseValue;
    public string displayedName;
    public Sprite icon;
}

// public enum RefreshFrequence { NEVER, RARELY, SOMETIMES, FREQUENTLY, ALWAYS }