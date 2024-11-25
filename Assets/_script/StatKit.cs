using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Stat Kit", menuName = "Stat Kit", order = 1)]
public class StatKit : SerializedScriptableObject
{
    [OnInspectorInit("Refresh")]
    [SerializeField, EnumToggleButtons, HideLabel] StatCategory categories;
    [Searchable, DictionaryDrawerSettings(KeyLabel = "Stat", ValueLabel = "Base values", DisplayMode = DictionaryDisplayOptions.OneLine, IsReadOnly = true)]
    public Dictionary<StatType, StatKitPair> kit;
    [Button]
    void Refresh()
    {
        kit ??= new();
        foreach (var keyValue in StatManager.Descriptions)
        {
            if (categories.HasFlag(keyValue.Value.category) && !kit.ContainsKey(keyValue.Key))
                kit[keyValue.Key] = new();
            else if (!categories.HasFlag(keyValue.Value.category) && kit.ContainsKey(keyValue.Key))
                kit.Remove(keyValue.Key);
        }
        foreach (var keyValue in kit)
            keyValue.Value.SetStatType(keyValue.Key);
    }
}

[Serializable, HideReferenceObjectPicker]
public class StatKitPair
{
    [HideInInspector] public StatType statType;
    [SerializeField, HideInInspector] bool isPercent;
    [HideLabel, HideIf("isPercent"), SuffixLabel("@GetSuffix()", overlay: true), HorizontalGroup("grp")]
    public float flat;
    [HideLabel, ShowIf("isPercent"), SuffixLabel("%", overlay: true), HorizontalGroup("grp")]
    public int percent;
    public void SetStatType(StatType type)
    {
        statType = type;
        if (StatManager.Descriptions[statType].isPercent)
        {
            flat = 1;
            isPercent = true;
        }
    }
    string GetSuffix() => StatManager.Descriptions[statType].GetSuffix();
}