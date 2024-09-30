using UnityEngine;

public class PerkTraits : Perk
{
    public Traits trait;
    public override void ApplyUpgrade(Rarity rarity, bool revert = false)
    {
        switch (trait)
        {
            case Traits.AUTOMATIC:
                Player.player.weapon.isAutomatic = revert;
                break;
            case Traits.FIRE_WHILE_RUNNING:
                Player.player.fireWhileRunning = revert;
                break;
            default:
                break;
        }
    }
    public override bool CanBeApplied()
    {
        return trait switch
        {
            Traits.AUTOMATIC => !Player.player.weapon.isAutomatic,
            _ => true,
        };
    }
}

public enum Traits
{
    AUTOMATIC, FIRE_WHILE_RUNNING
}
