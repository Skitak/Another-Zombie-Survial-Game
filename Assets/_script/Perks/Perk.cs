using Sirenix.OdinInspector;
using UnityEngine;

public abstract class Perk : ScriptableObject
{
    public bool dontShowAsUpgrade = false;
    [SerializeField] protected string label;
    public Sprite sprite;
    public Rarity rarityMin;
    public Rarity rarityMax;
    public int timesPerkCanBeApplied = 10000;
    [InlineEditor(InlineEditorModes.FullEditor)]
    public Perk malusPerk = null;
    public virtual bool CanBeApplied() => true;
    public virtual string GetLabel(Rarity rarity) => label;
    public abstract void ApplyUpgrade(Rarity rarity, bool revert = false);
}

public enum Rarity
{
    COMMON, UNCOMMON, RARE, LEGENDARY
}
