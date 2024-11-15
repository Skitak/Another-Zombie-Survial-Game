using System.Collections.Generic;
using Asmos.Bus;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
public class StatManager : SerializedMonoBehaviour
{
    // [ShowInInspector]
    [OnInspectorGUI("@instance = this")]
    [SerializeField]
    public static StatManager instance;
    [Searchable]
    [DictionaryDrawerSettings(KeyLabel = "Stat", ValueLabel = "Description", DisplayMode = DictionaryDisplayOptions.OneLine)]
    public Dictionary<StatType, StatDescription> statDescriptions = new();
    // [DictionaryDrawerSettings(KeyLabel = "Stat", ValueLabel = "Description", DisplayMode = DictionaryDisplayOptions.OneLine)]
    // [Searchable]
    // public Dictionary<StatType, StatDescription> statDescriptions;
    [DictionaryDrawerSettings(KeyLabel = "Mutation", ValueLabel = "Description", DisplayMode = DictionaryDisplayOptions.OneLine)]

    [Searchable]
    [SerializeField] Dictionary<StatType, MutationDescription> mutationDescriptions;

    [HideInInspector] Dictionary<StatType, Stat> stats = new();
    void Awake()
    {
        instance = this;
        foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
        {
            if (!statDescriptions.ContainsKey(statType))
                stats[statType] = new(0, statType);
            stats[statType] = new(statDescriptions[statType].baseValue, statType);
        }
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
    public static float Get(StatType type, bool addContextualModifier = true)
        => Mathf.Clamp(instance.stats[type].GetValue(addContextualModifier),
            instance.statDescriptions[type].min,
            instance.statDescriptions[type].max);
    public static Stat GetStat(StatType type) => instance.stats[type];

    public static void Subscribe(StatType type, System.Action<float> action) =>
        Bus.Subscribe(type.ToString(), (o) => action((float)o[0]));

    public static void ApplyStatModifier(StatModifier modifier) =>
        instance.stats[modifier.stat].AddModifier(modifier);
    public static void ApplyMutation(MutationType mutation)
    {

    }

}
public class Stat
{
    public StatType statType;
    public StatDescription description;
    public float baseValue, modifierFlat, modifierPercent;
    public Stat(float baseValue, StatType statType)
    {
        this.baseValue = baseValue; this.statType = statType; this.description = StatManager.instance.statDescriptions[statType];
    }
    public List<StatModifier> modifiers = new();
    public float GetValue(bool addContextualModifier = true)
    {
        float value = (GetBaseValue() + GetFlatValue(addContextualModifier)) * GetPercentValue(addContextualModifier);
        if (description.displayType == StatDisplayType.INT)
            return Mathf.Floor(value);
        return value;
    }
    public float GetBaseValue() => baseValue;
    public float GetFlatValue(bool addContextualModifier = true)
        => modifierFlat + (addContextualModifier ? GetContextualModifierFlat() : 0);
    public float GetPercentValue(bool addContextualModifier = true)
        => Mathf.Max(modifierPercent + (addContextualModifier ? GetContextualModifierPercent() : 0) + 1, 0);
    public float GetContextualModifierFlat()
    {
        float value = 0;
        foreach (StatModifier modifier in modifiers.Where(x => IsContextModifier(x, false)))
            value += modifier.GetValue();
        return value;
    }
    public float GetContextualModifierPercent()
    {
        float value = 0;
        foreach (StatModifier modifier in modifiers.Where(x => IsContextModifier(x, true)))
            value += modifier.GetValue();
        return value;
    }
    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
        if (modifier.ShouldApplyOnUpdate())
            modifier.ApplyOnUpdate(() => { ApplyModifier(modifier); });
        else if (!modifier.UseContext())
            ApplyModifier(modifier);
    }
    void ApplyModifier(StatModifier modifier)
    {
        float oldValue = description.onlyPushDifference ? GetValue() : 0;
        if (modifier.applyToBaseValue)
        {
            if (modifier.applyPercentageValue)
                baseValue *= modifier.GetUnappliedValue() + 1;
            else
                baseValue += modifier.GetUnappliedValue();
        }
        else
        {
            if (modifier.applyPercentageValue)
                modifierPercent *= modifier.GetUnappliedValue() + 1;
            else
                modifierFlat += modifier.GetUnappliedValue();
        }
        Bus.PushData(statType.ToString(), GetValue() - oldValue);
    }
    bool IsContextModifier(StatModifier modifier, bool applyPercentageValue) =>
        !modifier.ShouldApplyOnUpdate() && modifier.UseContext() && modifier.applyPercentageValue == applyPercentageValue;
}
public struct StatDescription
{
    public float baseValue;
    public float min;
    public float max;
    public StatDisplayType displayType;
    public StatCategory category;
    public string displayedName;
    public bool onlyPushDifference;
    public Sprite icon;
    public string ValueToString(float value)
    {
        switch (displayType)
        {
            case StatDisplayType.DEGREE:
                return $"{value}°";
            case StatDisplayType.PERCENT:
                return $"{(int)(value)}%";
            case StatDisplayType.INT:
                return $"{(int)value}";
            case StatDisplayType.FLOAT:
                return $"{value}";
            case StatDisplayType.SECONDS:
                return $"{value}s";
            case StatDisplayType.M_PER_SEC:
                return $"{value}m/s";
            default:
                return $"{value}";
        }
    }
}
public enum StatDisplayType { INT, FLOAT, PERCENT, DEGREE, SECONDS, M_PER_SEC }
public enum StatType
{
    // Player
    SPEED,
    STAMINA_MAX,
    SPRINT_SPEED,
    HEALTH_MAX,
    // Weapon
    MAGAZINE_SIZE,
    RELOAD_TIME,
    SPREAD,
    PRECISION_AIM,
    DAMAGES,
    HEADSHOT_DAMAGES,
    FIRE_RATE,
    BULLET_AMOUNT,
    CRIT_CHANCE,
    CRIT_DAMAGES,
    // Explosions
    EXPLOSION_RADIUS,
    EXPLOSION_DAMAGES,
    EXPLOSION_SPEED,
    // MONEY
    INCOME,
    // MISC
    PICK_DISTANCE,
    DRINKS,
    RECOIL,
    HEALTH_UPDATE,
    MONEY_UPDATE,
    DROP_RATE
}
public enum StatCategory { PLAYER, WEAPON, EXPLOSION, MISC, HIDDEN }
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