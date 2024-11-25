using System;
using System.Collections.Generic;
using System.Linq;
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
    [Title("Card")] public string title;
    public Sprite sprite;
    [Title("Shop")] public bool showInShop = true;
    [Tooltip("Will appear in priority, used for testing purpose")] public bool hasPrio = false;
    [LabelText("Is conditional")] public bool hasShopCondition = false;
    [ShowIf("@hasShopCondition && hasPrio")] public bool ignoreConditionWithPrio = false;
    [SerializeField, ShowIf("hasShopCondition"), OdinSerialize, NonSerialized] ContextCondition shopCondition;
    ContextCondition clonedShopCondition;
    [Title("Parameters")] public int price = 1000;
    public Rarity rarity;
    [SerializeField] bool isConditional = false;
    [ShowIf("isConditional"), OdinSerialize, NonSerialized, Title("Condition")] public ContextCondition condition = new();
    [ListDrawerSettings, OdinSerialize, NonSerialized, Title("Modifiers")] public List<Modifier> modifiers;
    public virtual bool CanBeApplied()
    {
        if (!showInShop)
            return false;
        if (hasShopCondition && !shopCondition.IsValid())
            return false;
        return true;
    }
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
        foreach (Modifier modifier in modifiers)
        {
            if (!first || isConditional)
                endLabel += "\n";
            endLabel += modifier.GetLabel(showContextData);
            first = false;
        }
        return endLabel;
    }
    public virtual void ApplyModifiers(bool isDrink = false)
    {
        List<Modifier> clonedModifiers = new();
        foreach (Modifier modifier in modifiers)
        {
            Modifier clonedModifier = modifier.Clone();
            clonedModifier.isActive = !isConditional;
            clonedModifiers.Add(clonedModifier);
            if (isDrink)
                clonedModifier.value *= StatManager.Get(StatType.DRINKS) / 100;
            clonedModifier.ApplyModifier();
        }
        if (isConditional)
        {
            ContextCondition clonedCondition = condition.Clone();
            clonedCondition.Initialize();
            clonedCondition.Listen((o) =>
            {
                bool isValid = IsValid();
                foreach (var modifier in clonedModifiers)
                    modifier.isActive = isValid;
            });
        }
    }
    public Sprite GetSprite() => sprite != null ? sprite : modifiers[0].GetValueSprite();

    #region preview

    [SerializeField, HideInInspector, MultiLineProperty(5), PropertyOrder(11), OnInspectorGUI("UpdateLabelPreview"), Title("Label"), HideLabel]
    [InlineButton("@useCustomLabel = !useCustomLabel", "@useCustomLabel?\"Custom\":\"Generated\"")]
    string label = "";
    void UpdateLabelPreview() => label = useCustomLabel ? label : GetLabel(false);
    [SerializeField, HideInInspector] bool useCustomLabel;

    [ShowInInspector, PropertyOrder(10), DisplayAsString(false), OnInspectorGUI("GetEstimatedValue")]
    int estimatedValue;
    public int GetEstimatedValue()
    {
        estimatedValue = 0;
        foreach (var modifier in modifiers)
        {
            if (modifier != null)
                estimatedValue += modifier.GetEstimatedValue();
        }
        return estimatedValue;
    }
    #endregion
    public static int GetRarityPrice(Rarity rarity) => rarity switch
    {
        Rarity.COMMON => 1000,
        Rarity.UNCOMMON => 1500,
        Rarity.RARE => 2000,
        Rarity.LEGENDARY => 3000,
        _ => 0,
    };
    public static int GetRarityEstimatedValue(Rarity rarity) => rarity switch
    {
        Rarity.COMMON => 1000,
        Rarity.UNCOMMON => 2000,
        Rarity.RARE => 3000,
        Rarity.LEGENDARY => 5000,
        _ => 0,
    };
    [OnInspectorInit]
    void OnInit()
    {
        modifiers ??= new();
        condition ??= new();
        shopCondition ??= new();
    }
    public void InitializeCondition()
    {
        if (!hasShopCondition)
            return;
        clonedShopCondition?.Destroy();
        clonedShopCondition = shopCondition.Clone();
        clonedShopCondition.Initialize();
    }
    // public bool IsValid() => !hasShopCondition || clonedShopCondition.IsValid();
    public bool IsValid()
    {
        Debug.Log("Heya");
        return !hasShopCondition || clonedShopCondition.IsValid();
    }
}

[Flags]
public enum Rarity
{
    COMMON = 1 << 0, UNCOMMON = 1 << 1, RARE = 1 << 2, LEGENDARY = 1 << 3, ALL = COMMON | UNCOMMON | RARE | LEGENDARY
}