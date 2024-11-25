#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class PerkWindow : OdinEditorWindow
{
    [EnumToggleButtons, HideLabel, ShowInInspector, TabGroup("Listing")]
    Rarity rarities = Rarity.COMMON;

    #region estimation
    [ShowInInspector, TabGroup("Listing")]
    float estimationMinBound = 1;
    [InlineButton("ToggleEstimation", "@estimationTitle[estimationKind]")]
    [ShowInInspector, TabGroup("Listing")]
    float estimationMaxBound = 1;
    [HideInInspector, SerializeField, TabGroup("Listing")]
    int estimationKind = 0;
    void ToggleEstimation() { estimationKind = (estimationKind + 1) % 3; UpdateList(); }
    string[] estimationTitle = new[] { "In", "Out", "Ignore" };
    #endregion

    #region list filter
    [SerializeField, ShowIf("displayStatDictionnary"), TabGroup("Listing")] bool showNegativeValues;
    [SerializeField, TabGroup("Listing")] bool displayStatDictionnary;
    [SerializeField, TabGroup("Listing")] bool showPrioOnly;
    [ShowInInspector, Searchable, ShowIf("displayStatDictionnary"), TabGroup("Listing")]
    [DictionaryDrawerSettings(KeyLabel = "Stat", ValueLabel = "Perks", DisplayMode = DictionaryDisplayOptions.OneLine, IsReadOnly = true)]
    readonly Dictionary<StatType, List<Perk>> perks = new();
    [ShowInInspector, HideIf("displayStatDictionnary"), LabelText("Perks"), TabGroup("Listing")]
    readonly List<Perk> perksList = new();
    #endregion

    #region perk generation
    [ShowInInspector, TabGroup("Automatic perk generation")] string folderPath = "Assets/Settings/Perks/Base Perks/";
    [ShowInInspector, TabGroup("Automatic perk generation")] bool overridePresentPerks;
    [ShowInInspector, TabGroup("Automatic perk generation")] bool removeUnknownPerksInFolder;
    #endregion
    #region window
    [MenuItem("Tools/Perks")]
    private static void OpenWindow()
    {
        var window = GetWindow<PerkWindow>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
    }

    void OnValidate() => UpdateList();
    #endregion

    void UpdateList()
    {
        var allPerks = Asmos.UI.Utils.GetAllInstances<Perk>(new[] { "Assets/Settings/Perks" });
        perks.Clear();
        perksList.Clear();
        foreach (Perk perk in allPerks)
        {
            if (!ShouldDisplay(perk))
                continue;
            perksList.Add(perk);
            foreach (StatModifier modifier in perk.modifiers.Where(x => x is StatModifier))
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
        if (showPrioOnly)
            return perk.hasPrio;
        if (perk.modifiers == null)
            return false;
        if (!rarities.HasFlag(perk.rarity))
            return false;
        if (!perk.showInShop)
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


    [Button, TabGroup("Automatic perk generation")]
    void GenerateBasePerks()
    {
        int i = 0;
        List<string> knownPerks = new();
        foreach (var statDescriptionKeyVal in StatManager.Descriptions)
        {
            StatDescription description = statDescriptionKeyVal.Value;
            foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
            {
                if (rarity == Rarity.ALL || !description.generatedRarities.HasFlag(rarity))
                    continue;
                string fileName = $"{description.displayedName} {rarity}.asset";
                string filePath = $"{folderPath}{fileName}";
                knownPerks.Add(fileName);

                if (AssetDatabase.AssetPathExists(filePath))
                {
                    if (overridePresentPerks)
                        AssetDatabase.DeleteAsset(filePath);
                    else
                        continue;
                }

                i++;
                bool modificationAsPercent = description.isPercent || description.modificationAsPercent;
                float estimation = modificationAsPercent ? description.estimationPercent / 100 : description.estimatedValue;
                float value = Mathf.Floor(Perk.GetRarityEstimatedValue(rarity) / estimation);
                if (description.isNegative)
                    value *= -1;

                Perk newPerk = CreateInstance<Perk>();
                newPerk.title = description.displayedName;
                newPerk.rarity = rarity;
                newPerk.sprite = description.icon;
                newPerk.price = Perk.GetRarityPrice(rarity);
                newPerk.modifiers = new()
                {
                    new StatModifier()
                    {
                        stat = statDescriptionKeyVal.Key,
                        modificationKind = modificationAsPercent ? ModificationKind.PERCENT : ModificationKind.FLAT,
                        value = value,
                    }
                };
                // Debug.Log($"Creating perk : {fileName}");
                AssetDatabase.CreateAsset(newPerk, filePath);
            }
        }
        if (removeUnknownPerksInFolder)
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(folderPath);
            var directoryInfo = new DirectoryInfo(folderPath);
            foreach (var file in directoryInfo.EnumerateFiles("*.asset"))
            {
                if (!knownPerks.Any(x => file.Name.Contains(x)))
                {

                    Debug.Log($"Deleting : {folderPath}{file.Name}");
                    AssetDatabase.DeleteAsset($"{folderPath}{file.Name}");
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"{i} Perks created.");
    }

}

#endif