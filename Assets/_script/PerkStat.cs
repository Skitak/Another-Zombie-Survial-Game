using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "PerkStat", menuName = "Perks/PerkStat", order = 0)]
public class PerkStat : Perk
{
    [SerializeField] bool applyPercentageValue = false;
    [SerializeField] Stat stat;

    [InfoBox("This array represents values based on rarity")]
    [ShowIf("applyPercentageValue")]
    [RequiredListLength(4)]
    [SerializeField]
    float[] valuePercent = new float[] { 0f, 0f, 0f, 0f };

    [InfoBox("This array represents values based on rarity.")]
    [HideIf("applyPercentageValue")]
    [RequiredListLength(4)]
    [SerializeField]
    int[] valueFlat = new int[] { 0, 0, 0, 0 };
    public override string GetLabel(Rarity rarity)
    {
        int rarityIndex = (int)rarity;
        if (applyPercentageValue)
            return $"{valuePercent[rarityIndex]}% {label}";
        return $"+{valueFlat[rarityIndex]} {label}";

    }
    public override void ApplyUpgrade(Rarity rarity, bool revert = false)
    {
        switch (stat)
        {
            case Stat.SPEED:
                Player.player.speed = GetStatUpgrade(Player.player.speed, rarity, revert);
                break;
            case Stat.SPRINT_SPEED:
                Player.player.speedSprint = GetStatUpgrade(Player.player.speedSprint, rarity, revert);
                break;
            case Stat.STAMINA:
                Player.player.staminaMax = GetStatUpgrade(Player.player.staminaMax, rarity, revert);
                break;
            case Stat.HEALTH:
                Player.player.healthMax = GetStatUpgrade(Player.player.healthMax, rarity, revert);
                break;
            case Stat.MAGAZIN_SIZE:
                Player.player.weapon.ammoMax = GetStatUpgrade(Player.player.weapon.ammoMax, rarity, revert);
                break;
            case Stat.PRECISION:
                Player.player.weapon.precision = GetStatUpgrade(Player.player.weapon.precision, rarity, revert);
                break;
            case Stat.DAMAGES:
                Player.player.weapon.damages = GetStatUpgrade(Player.player.weapon.damages, rarity, revert);
                break;
            // case Stat.BULLET_AMOUNT:
            //             value /= = GetStatUpgrade(//             value /=, rarity, revert);
            //     break;
            case Stat.RELOAD_TIME:
                Player.player.weapon.reloadTime = GetStatUpgrade(Player.player.weapon.reloadTime, rarity, revert);
                break;
            case Stat.FIRE_RATE:
                Player.player.weapon.fireRate = GetStatUpgrade(Player.player.weapon.fireRate, rarity, revert);
                break;
            // case Stat.EXPLOSION_RADIUS:
            //             value /= = GetStatUpgrade(//             value /=, rarity, revert);
            //     break;
            // case Stat.EXPLOSION_DAMAGES:
            //             value /= = GetStatUpgrade(//             value /=, rarity, revert);
            //     break;
            default:
                // Handle default case if stat is not recognized
                break;
        }
    }

    float GetStatUpgrade(float value, Rarity rarity, bool revert)
    {
        int rarityIndex = (int)rarity;
        if (applyPercentageValue)
            if (revert)
                value /= 1 + valuePercent[rarityIndex] / 100;
            else
                value *= 1 + valuePercent[rarityIndex] / 100;
        else
            value += revert ? -valueFlat[rarityIndex] : valueFlat[rarityIndex];
        return value;
    }
    int GetStatUpgrade(int value, Rarity rarity, bool revert) => (int)GetStatUpgrade((float)value, rarity, revert);
}

public enum Stat
{
    SPEED, SPRINT_SPEED, STAMINA, HEALTH, MAGAZIN_SIZE, PRECISION, DAMAGES, BULLET_AMOUNT, RELOAD_TIME, FIRE_RATE, EXPLOSION_RADIUS, EXPLOSION_DAMAGES
}
