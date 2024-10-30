using UnityEngine;

public class PerkTraits : Perk
{
    public Traits trait;
    public override void ApplyUpgrades()
    {
        switch (trait)
        {
            case Traits.AUTOMATIC:
                Player.player.weapon.isAutomatic = true;
                break;
            case Traits.FIRE_WHILE_RUNNING:
                Player.player.fireWhileRunning = true;
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
