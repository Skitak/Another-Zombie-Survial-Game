#if UNITY_EDITOR

using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class PerkWindow : OdinEditorWindow
{
    [ShowInInspector]
    [EnumToggleButtons, HideLabel]
    Rarities rarities;

    #region max usage
    [InlineButton("ToggleMaxUsage", "@hasMaxUsage?\"Filter\":\"Ignore\"")]
    [ShowInInspector] int maxUsage = 1;
    [HideInInspector, SerializeField] bool hasMaxUsage;
    void ToggleMaxUsage() { hasMaxUsage = !hasMaxUsage; UpdateList(); }
    #endregion
    #region estimation
    [HorizontalGroup("estimation"), LabelText("Estimation")]
    [ShowInInspector] float estimationMinBound = 1;
    [HorizontalGroup("estimation"), HideLabel]
    [InlineButton("ToggleEstimation", "@estimationTitle[estimationKind]")]
    [ShowInInspector] float estimationMaxBound = 1;
    [HideInInspector, SerializeField] int estimationKind = 0;
    void ToggleEstimation() { estimationKind = (estimationKind + 1) % 3; UpdateList(); }
    string[] estimationTitle = new[] { "In", "Out", "Ignore" };
    #endregion

    [SerializeField] bool showNegativeValues;
    [ShowInInspector]
    [Searchable]
    [DictionaryDrawerSettings(KeyLabel = "Stat", ValueLabel = "Perks", DisplayMode = DictionaryDisplayOptions.OneLine, IsReadOnly = true)]
    readonly Dictionary<StatType, List<Perk>> perks = new();
    [MenuItem("Tools/Perks")]
    private static void OpenWindow()
    {
        var window = GetWindow<PerkWindow>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
    }

    void OnValidate() => UpdateList();
    void UpdateList()
    {
        var allPerks = Asmos.UI.Utils.GetAllInstances<Perk>(new[] { "Assets/Settings/Perks" });
        // allPerks = Asmos.UI.Utils.GetAllInstances<Perk>();
        perks.Clear();
        foreach (Perk perk in allPerks)
        {
            if (!ShouldDisplay(perk))
                continue;
            foreach (StatModifier modifier in perk.statModifiers)
            {
                if (!IsPerkModifierValid(perk, modifier))
                    continue;
                if (!perks.ContainsKey(modifier.stat))
                    perks[modifier.stat] = new();
                if (!perks[modifier.stat].Contains(perk))
                    perks[modifier.stat].Add(perk);
            }
        }
    }

    bool ShouldDisplay(Perk perk)
    {
        int enumBit = 1 << (int)perk.rarity;
        if (((int)rarities & enumBit) != enumBit)
            return false;
        if (!perk.showInShop)
            return false;
        if (hasMaxUsage && perk.maxApplications > maxUsage)
            return false;
        if (estimationKind == 1)
        {
            int estimation = perk.GetEstimatedValue();
            if (estimation > perk.price * estimationMinBound && estimation < perk.price * estimationMaxBound)
                return false;
        }
        if (estimationKind == 0)
        {
            int estimation = perk.GetEstimatedValue();
            if (estimation < perk.price * estimationMinBound || estimation > perk.price * estimationMaxBound)
                return false;
        }
        return true;
    }

    bool IsPerkModifierValid(Perk perk, StatModifier modifier)
    {
        if (!showNegativeValues && (modifier.value < 0 || (modifier.value > 0 && StatManager.Descriptions[modifier.stat].isNegative)))
            return false;
        return true;
    }
    [System.Flags]
    public enum Rarities
    {
        COMMON = 1 << 0, UNCOMMON = 1 << 1, RARE = 1 << 2, LEGENDARY = 1 << 3, All = COMMON | UNCOMMON | RARE | LEGENDARY
    }

}

#endif