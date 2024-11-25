using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

// [CreateAssetMenu(fileName = "Stat Descriptor", menuName = "Stat Descriptor", order = 0)]
public class StatDescriptionsHolder : SerializedScriptableObject
{
    [Searchable]
    [DictionaryDrawerSettings(KeyLabel = "Stat", ValueLabel = "Description", DisplayMode = DictionaryDisplayOptions.OneLine)]
    public Dictionary<StatType, StatDescription> statDescriptions = new();
}
public struct StatDescription
{
    [Title("Value description")]
    public bool isPercent;
    [Tooltip("Set to true if improving this stat would have a negative effect on the player")]
    public bool isNegative;
    [SuffixLabel("@GetSuffix()", overlay: true), HorizontalGroup("Range", MarginRight = .05f), LabelText("Range")] public float min;
    [SuffixLabel("@GetSuffix()", overlay: true), HorizontalGroup("Range"), HideLabel] public float max;
    [HideIf("isPercent"), Title("Estimation")] public float estimatedValue;
    public float estimationPercent;
    [HideIf("isPercent"), Title("Display")] public StatDisplayType displayType;
    public StatCategory category;
    public string displayedName;
    public Sprite icon;
    [Title("Automatic perk generation")]
    [EnumToggleButtons, FormerlySerializedAs("rarities"), HideLabel] public Rarity generatedRarities;
    [HideIf("isPercent")] public bool modificationAsPercent;
    public string ValueToString(float value)
    {
        return displayType switch
        {
            StatDisplayType.PERCENT => $"{(int)value}%",
            StatDisplayType.INT => $"{(int)value}",
            _ => $"{value}{GetSuffix()}",
        };
    }
    public string GetSuffix() => displayType switch
    {
        StatDisplayType.DEGREE => "°",
        StatDisplayType.PERCENT => "%",
        StatDisplayType.SECONDS => "s",
        StatDisplayType.M_PER_SEC => "m/s",
        StatDisplayType.PER_SEC => "/s",
        StatDisplayType.METERS => "meters",
        _ => "",
    };

}
public enum StatDisplayType { INT, FLOAT, PERCENT, DEGREE, SECONDS, M_PER_SEC, METERS, PER_SEC }
[Flags]
public enum StatCategory
{
    PLAYER = 1 << 0,
    WEAPON = 1 << 1,
    EXPLOSION = 1 << 2,
    MISC = 1 << 3,
}

public enum StatType
{
    // Player
    SPEED = 0,
    STAMINA_MAX = 1,
    HEALTH_MAX = 3,
    // Weapon
    MAGAZINE_SIZE = 4,
    RELOAD_TIME = 5,
    SPREAD = 6,
    PRECISION_AIM = 7,
    DAMAGES = 8,
    HEADSHOT_DAMAGES = 9,
    FIRE_RATE = 10,
    BULLET_AMOUNT = 11,
    CRIT_CHANCE = 12,
    CRIT_DAMAGES = 13,
    // Explosions
    EXPLOSION_RADIUS = 14,
    EXPLOSION_DAMAGES = 15,
    EXPLOSION_SPEED = 16,
    // MONEY
    INCOME = 17,
    // MISC
    PICK_DISTANCE = 18,
    DRINKS = 19,
    RECOIL = 20,
    DROP_RATE = 21,
}