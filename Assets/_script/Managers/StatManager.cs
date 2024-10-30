using System;
using System.Collections.Generic;
using Asmos.Bus;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;
public class StatManager : SerializedMonoBehaviour
{
    public static StatManager instance;
    [DictionaryDrawerSettings(KeyLabel = "Stat", ValueLabel = "Description", DisplayMode = DictionaryDisplayOptions.OneLine)]
    [Searchable]
    public Dictionary<StatType, StatDescription> statDescriptions;
    [DictionaryDrawerSettings(KeyLabel = "Mutation", ValueLabel = "Description", DisplayMode = DictionaryDisplayOptions.OneLine)]
    [Searchable]
    [SerializeField] Dictionary<StatType, MutationDescription> mutationDescriptions;

    [HideInInspector] Dictionary<StatType, Stat> stats = new();
    // [HideInInspector] Dictionary<StatType, Mutat> stats;
    void Awake()
    {
        instance = this;
        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            if (!statDescriptions.ContainsKey(statType))
                stats[statType] = new(0);
            stats[statType] = new(statDescriptions[statType].baseValue);
        }
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

    public static void Subscribe(StatType type, Action<float> action) =>
        Bus.Subscribe(type.ToString(), (o) => action((float)o[0]));

    public static void ApplyStatModifier(StatModifier modifier)
    {
        instance.stats[modifier.stat].AddModifier(modifier);
        Bus.PushData(modifier.stat.ToString(), instance.stats[modifier.stat].GetValue());
    }
    public static void ApplyMutation(MutationType mutation)
    {

    }

}
public class Stat
{
    public float baseValue, modifierFlat, modifierPercent;
    public Stat(float baseValue) => this.baseValue = baseValue;
    public List<StatModifier> contextModifiers = new();
    public float GetValue(bool addContextualModifier = true) => (GetBaseValue() + GetFlatValue(addContextualModifier)) * GetPercentValue(addContextualModifier);
    public float GetBaseValue() => baseValue;
    public float GetFlatValue(bool addContextualModifier = true)
        => modifierFlat + (addContextualModifier ? GetContextualModifierFlat() : 0);
    public float GetPercentValue(bool addContextualModifier = true)
        => Mathf.Max(modifierPercent + (addContextualModifier ? GetContextualModifierPercent() : 0) + 1, 0);
    public float GetContextualModifierFlat()
    {
        float value = 0;
        foreach (StatModifier modifier in contextModifiers.Where(x => x.UseContextData() && !x.applyPercentageValue))
            value += modifier.GetValue();
        return value;
    }
    public float GetContextualModifierPercent()
    {
        float value = 0;
        foreach (StatModifier modifier in contextModifiers.Where(x => x.UseContextData() && x.applyPercentageValue))
            value += modifier.GetValue();
        return value;
    }
    public void AddModifier(StatModifier modifier)
    {
        contextModifiers.Add(modifier);
        if (!modifier.UseContextData())
        {
            if (!modifier.applyPercentageValue)
                modifierFlat += modifier.GetValue();
            else
                modifierPercent += modifier.GetValue();
        }
    }
}
public struct StatDescription
{
    public float baseValue;
    public float min;
    public float max;
    public StatDisplayType displayType;
    public StatCategory category;
    public string displayedName;
    public Sprite icon;
    public string ValueToString(float value)
    {
        switch (displayType)
        {
            case StatDisplayType.DEGREE:
                return $"{value}°";
            case StatDisplayType.PERCENT:
                return $"{(int)(value * 100)}%";
            case StatDisplayType.INT:
                return $"{(int)value}";
            case StatDisplayType.FLOAT:
                return $"{value}";
            default:
                return $"{value}";
        }
    }
}
public enum StatDisplayType
{
    INT, FLOAT, PERCENT, DEGREE
}
public struct MutationDescription
{
    public bool baseValue;
    public string displayedName;
    public Sprite icon;
    // DisplayType
}
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
    PRECISION,
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
    MONEY,
    // MISC
    PICK_DISTANCE,
    DRINKS,
    RECOIL
}
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

public enum StatCategory
{
    PLAYER, WEAPON, EXPLOSION, MISC
}