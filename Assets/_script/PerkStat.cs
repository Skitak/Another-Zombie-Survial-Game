using UnityEngine;

[CreateAssetMenu(fileName = "PerkStat", menuName = "Perk", order = 0)]
public class PerkStat : Perk
{
    [SerializeField] bool applyPercentageValue = false;
    [SerializeField] float[] valuePercent;
    [SerializeField] int[] valueFlat;
    [SerializeField] Stat stat;
    public override void ApplyUpgrade(bool revert, Rarity rarity)
    {
        int rarityIndex = (int)rarity;
        switch (stat)
        {
            case Stat.SPEED:
                if (applyPercentageValue)
                    if (revert)
                        Player.player.speed *= valuePercent[rarityIndex];
                    else
                        Player.player.speed /= valuePercent[rarityIndex];
                else
                    Player.player.speed += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
                break;
            case Stat.SPRINT_SPEED:
                if (applyPercentageValue)
                    if (revert)
                        Player.player.speedSprint *= valuePercent[rarityIndex];
                    else
                        Player.player.speedSprint /= valuePercent[rarityIndex];
                else
                    Player.player.speedSprint += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
                break;
            case Stat.STAMINA:
                if (applyPercentageValue)
                    if (revert)
                        Player.player.staminaMax *= valuePercent[rarityIndex];
                    else
                        Player.player.staminaMax /= valuePercent[rarityIndex];
                else
                    Player.player.staminaMax += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
                break;
            case Stat.HEALTH:
                if (applyPercentageValue)
                    if (revert)
                        Player.player.healthMax = (int)(Player.player.healthMax * valuePercent[rarityIndex]);
                    else
                        Player.player.healthMax = (int)(Player.player.healthMax / valuePercent[rarityIndex]);
                else
                    Player.player.healthMax += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
                break;
            case Stat.MAGAZIN_SIZE:
                if (applyPercentageValue)
                    if (revert)
                        Player.player.weapon.ammoMax = (int)(Player.player.weapon.ammoMax * valuePercent[rarityIndex]);
                    else
                        Player.player.weapon.ammoMax = (int)(Player.player.weapon.ammoMax / valuePercent[rarityIndex]);
                else
                    Player.player.weapon.ammoMax += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
                break;
            case Stat.PRECISION:
                if (applyPercentageValue)
                    if (revert)
                        Player.player.weapon.precision *= (int)(Player.player.weapon.precision * valuePercent[rarityIndex]);
                    else
                        Player.player.weapon.precision /= (int)(Player.player.weapon.precision / valuePercent[rarityIndex]);
                else
                    Player.player.weapon.precision += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
                break;
            case Stat.DAMAGES:
                if (applyPercentageValue)
                    if (revert)
                        Player.player.weapon.damages *= (int)(Player.player.weapon.damages * valuePercent[rarityIndex]);
                    else
                        Player.player.weapon.damages /= (int)(Player.player.weapon.damages / valuePercent[rarityIndex]);
                else
                    Player.player.weapon.damages += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
                break;
            // case Stat.BULLET_AMOUNT:
            //     if (applyPercentageValue)
            //         if (revert)
            //             value *= valuePercent[rarityIndex];
            //         else
            //             value /= valuePercent[rarityIndex];
            //     else
            //         value += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
            //     break;
            case Stat.RELOAD_TIME:
                if (applyPercentageValue)
                    if (revert)
                        Player.player.weapon.reloadTime *= valuePercent[rarityIndex];
                    else
                        Player.player.weapon.reloadTime /= valuePercent[rarityIndex];
                else
                    Player.player.weapon.reloadTime += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
                break;
            case Stat.FIRE_RATE:
                if (applyPercentageValue)
                    if (revert)
                        Player.player.weapon.fireRate *= valuePercent[rarityIndex];
                    else
                        Player.player.weapon.fireRate /= valuePercent[rarityIndex];
                else
                    Player.player.weapon.fireRate += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
                break;
            // case Stat.EXPLOSION_RADIUS:
            //     if (applyPercentageValue)
            //         if (revert)
            //             value *= valuePercent[rarityIndex];
            //         else
            //             value /= valuePercent[rarityIndex];
            //     else
            //         value += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
            //     break;
            // case Stat.EXPLOSION_DAMAGES:
            //     if (applyPercentageValue)
            //         if (revert)
            //             value *= valuePercent[rarityIndex];
            //         else
            //             value /= valuePercent[rarityIndex];
            //     else
            //         value += revert ? valueFlat[rarityIndex] : -valueFlat[rarityIndex];
            //     break;
            default:
                // Handle default case if stat is not recognized
                break;
        }
    }
    void OnValidate()
    {
        int raritiesLength = System.Enum.GetValues(typeof(Rarity)).Length;
        if (valueFlat.Length != raritiesLength)
        {
            Debug.LogWarning("Don't change the 'ints' field's array size!");
            System.Array.Resize(ref valueFlat, raritiesLength);
        }
        if (valuePercent.Length != raritiesLength)
        {
            Debug.LogWarning("Don't change the 'ints' field's array size!");
            System.Array.Resize(ref valuePercent, raritiesLength);
        }
    }
}

public enum Stat
{
    SPEED, SPRINT_SPEED, STAMINA, HEALTH, MAGAZIN_SIZE, PRECISION, DAMAGES, BULLET_AMOUNT, RELOAD_TIME, FIRE_RATE, EXPLOSION_RADIUS, EXPLOSION_DAMAGES
}
