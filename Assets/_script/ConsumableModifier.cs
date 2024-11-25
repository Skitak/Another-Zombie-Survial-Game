using Sirenix.OdinInspector;
using UnityEngine;

public enum ConsumableType { HEAL, MAGAZINE, AMMUNITIONS, MONEY, GRENADE }
public class ConsumableModifier : Modifier
{
    [PropertyOrder(1), TitleGroup("@GetTitle()")] public ConsumableType consumable;
    [PropertyOrder(2), TitleGroup("@GetTitle()")] public bool usePercentValue = false;
    int timesApplied;
    protected override string GetValueSuffix() => usePercentValue ? "%" : "";
    bool IsContextScaling() => HasContext && context is ContextStatIncremental;
    public override Sprite GetValueSprite()
    {
        return null;
    }

    public override void ApplyModifier()
    {
        base.ApplyModifier();
        if (IsContextScaling())
            context.Listen((o) => { UseConsumable(); });
        if (!IsConditional)
            UseConsumable();
        else
        {
            void action(object[] o)
            {
                if (condition.IsValid())
                    condition.StopListening(action);
                UseConsumable();
            }
            if (!IsContextScaling())
                condition.Listen(action);
        }
    }

    void UseConsumable()
    {
        if (!IsValid())
            return;

        int timesItApplies = 1;
        int newValue = (int)GetValue();
        if (IsContextScaling())
            timesItApplies = newValue / appliedPerContextValue - timesApplied;

        if (timesItApplies <= 0)
            return;

        timesApplied += timesItApplies;

        for (int i = 0; i < timesItApplies; i++)
            switch (consumable)
            {
                case ConsumableType.HEAL:
                    int heal = newValue;
                    if (usePercentValue)
                        heal = (int)(Player.player.healthMax * (newValue / 100f));
                    Player.player.health += heal;
                    break;
                case ConsumableType.GRENADE:
                    int grenades = newValue;
                    if (usePercentValue)
                        grenades = (int)(Player.player.grenades * (newValue / 100f));
                    Player.player.grenades += grenades;
                    break;
                case ConsumableType.MONEY:
                    int money = newValue;
                    if (usePercentValue)
                        money = (int)(MoneyManager.instance.GetMoney() * (newValue / 100f));
                    MoneyManager.instance.UpdateMoney(money, IncomeType.PERK);
                    break;
                case ConsumableType.AMMUNITIONS:
                    int ammo = newValue;
                    if (usePercentValue)
                        ammo = (int)(Player.player.weapon.ammoMax * (newValue / 100f));
                    Player.player.weapon.ammo += ammo;
                    break;
                case ConsumableType.MAGAZINE:
                    break;
            }
    }
    protected override string GetValueName() => consumable switch
    {
        ConsumableType.HEAL => "Heal",
        ConsumableType.MONEY => "Money",
        ConsumableType.AMMUNITIONS => "Ammunitions",
        ConsumableType.MAGAZINE => "Magazine",
        ConsumableType.GRENADE => "Grenades",
        _ => "",
    };

    protected override string GetTitle() => $"Consumable : {consumable}";
}
