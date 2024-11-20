using System;
using System.Collections.Generic;
using Asmos.Bus;
using NUnit.Framework;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewPerk", menuName = "Perks", order = 0)]
public class Perk : SerializedScriptableObject
{

    public bool showInShop = true;
    public bool hasPrio = false;
    public string title;
    [ShowInInspector] Sprite sprite;
    public int maxApplications = int.MaxValue;
    public int price = 1000;
    public Rarity rarity;
    [ListDrawerSettings(), OdinSerialize, NonSerialized,]
    public List<StatModifier> statModifiers;
    public List<MutationType> mutations;
    public virtual bool CanBeApplied() => true;
    public virtual string GetLabel(bool showContextData = true)
    {
        if (useCustomLabel)
            return label;
        string endLabel = "";
        bool first = true;
        if (isConditional)
        {
            endLabel += condition.GetLabel(showContextData) + " :";
            endLabel = Utils.FirstLetterToUpper(endLabel);
        }
        foreach (StatModifier modifier in statModifiers)
        {
            if (!first || isConditional)
                endLabel += "\n";
            endLabel += modifier.GetLabel(showContextData);
            first = false;
        }
        return endLabel;

    }
    public virtual void ApplyUpgrades(bool isDrink = false)
    {
        List<StatModifier> clonedModifiers = new();
        foreach (StatModifier modifier in statModifiers)
        {
            StatModifier clonedModifier = modifier.Clone();
            clonedModifier.Initialize();
            clonedModifiers.Add(clonedModifier);
            if (isDrink)
                clonedModifier.value *= StatManager.Get(StatType.DRINKS) / 100;
            StatManager.ApplyStatModifier(clonedModifier);
        }
        if (isConditional)
        {
            ContextCondition clonedCondition = condition.Clone();
            clonedCondition.Initialize(true);
            clonedCondition.OnUpdate += (bool isActive) =>
            {
                foreach (var modifier in clonedModifiers)
                    modifier.isActive = isActive;
            };
            clonedCondition.OnUpdate.Invoke(clonedCondition.IsValid());
        }
        foreach (MutationType mutation in mutations)
            StatManager.ApplyMutation(mutation);
    }

    public Sprite GetSprite() => sprite != null ? sprite : StatManager.Descriptions[statModifiers[0].stat].icon;
    [OnInspectorGUI] private void Space2() { GUILayout.Space(20); }

    #region perview

    [SerializeField, HideInInspector, MultiLineProperty(5), PropertyOrder(11), OnInspectorGUI("UpdateLabelPreview")]
    [InlineButton("@useCustomLabel = !useCustomLabel", "@useCustomLabel?\"Custom\":\"Generated\"")]
    string label = "";
    void UpdateLabelPreview() => label = useCustomLabel ? label : GetLabel(false);
    [SerializeField, HideInInspector] bool useCustomLabel;

    [ShowInInspector, PropertyOrder(10), DisplayAsString(false), OnInspectorGUI("GetEstimatedValue")]
    int estimatedValue;
    public int GetEstimatedValue()
    {
        estimatedValue = 0;
        foreach (var modifier in statModifiers)
            estimatedValue += modifier.GetEstimatedValue();
        return estimatedValue;
    }
    #endregion
    [SerializeField] bool isConditional = false;
    [ShowIf("isConditional"), OdinSerialize, NonSerialized] public ContextCondition condition = new();
}

public enum Rarity { COMMON, UNCOMMON, RARE, LEGENDARY }