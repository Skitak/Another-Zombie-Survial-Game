using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// [CreateAssetMenu(fileName = "Stat Descriptor", menuName = "Stat Descriptor", order = 0)]
public class StatDescriptionsHolder : SerializedScriptableObject
{
    [Searchable]
    [DictionaryDrawerSettings(KeyLabel = "Stat", ValueLabel = "Description", DisplayMode = DictionaryDisplayOptions.OneLine)]
    public Dictionary<StatType, StatDescription> statDescriptions = new();
}
public struct StatDescription
{
    public bool isPercent;
    [Tooltip("Set to true if improving this stat would have a negative effect on the player")]
    public bool isNegative;
    [SuffixLabel("@GetSuffix()", overlay: true), HorizontalGroup("Range", MarginRight = .05f), LabelText("Range")] public float min;
    [SuffixLabel("@GetSuffix()", overlay: true), HorizontalGroup("Range"), HideLabel] public float max;
    [HideIf("isPercent")] public float estimatedValue;
    public float estimationPercent;
    [HideIf("isPercent")] public StatDisplayType displayType;
    public StatCategory category;
    public string displayedName;
    public bool onlyPushDifference;
    public Sprite icon;
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
    DROP_RATE,
    GRENADE_UPDATE,
}