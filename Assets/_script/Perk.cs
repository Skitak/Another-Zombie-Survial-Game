using UnityEngine;

public abstract class Perk : ScriptableObject
{
    [SerializeField] string label;
    [SerializeField] Sprite sprite;
    public Rarity rarityMin;
    public virtual string GetLabel() => label;
    public abstract void ApplyUpgrade(bool revert, Rarity rarity);
}

public enum Rarity
{
    COMMON, UNCOMMON, RARE, LEGENDARY
}
public enum SpecialTraits
{
    AUTOMATIC
}
