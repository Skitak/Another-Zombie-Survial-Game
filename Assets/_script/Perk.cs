using UnityEngine;

public abstract class Perk : ScriptableObject
{
    [SerializeField] protected string label;
    public Sprite sprite;
    public Rarity rarityMin;
    public Rarity rarityMax;
    public int timesPerkCanBeApplied = 10000;
    public virtual string GetLabel(Rarity rarity) => label;
    public abstract void ApplyUpgrade(Rarity rarity, bool revert = false);
}

public enum Rarity
{
    COMMON, UNCOMMON, RARE, LEGENDARY
}
public enum SpecialTraits
{
    AUTOMATIC
}
