using UnityEngine;

public enum ConsumableType { HEALTH, AMMUNITIONS, MONEY, GRENADE }
public class ConsumableModifier : Modifier
{
    public ConsumableType consumable;
    int previousValue;
    public override Sprite GetValueSprite()
    {
        return null;
    }

    public override void ApplyModifier()
    {
        base.ApplyModifier();
        if (HasContext)
            context.Listen((o) => { UseConsumable(); });
        if (!IsConditional && !HasContext)
            UseConsumable();

        if (IsConditional)
        {
            void action(object[] o)
            {
                if (!condition.IsValid())
                    return;
                condition.StopListening(action);
                UseConsumable();
            }
            if (!HasContext)
                condition.Listen(action);
        }
    }

    void UseConsumable()
    {
        // TODO: Change that, only increment should listen actually 
        if (!IsValid()) return;
        int totalValue = (int)GetValue();
        int newValue = totalValue - previousValue;
        previousValue = totalValue;
        int timesItApplies = newValue / appliedPerContextValue;
        if (timesItApplies == 0)
            return;

        switch (consumable)
        {
            case ConsumableType.HEALTH:
                Player.player.health += newValue;
                break;
            case ConsumableType.GRENADE:
                Player.player.grenades += newValue;
                break;
            case ConsumableType.MONEY:
                MoneyManager.instance.UpdateMoney(newValue, IncomeType.PERK);
                break;
            case ConsumableType.AMMUNITIONS:
                break;
        }
    }
    protected override string GetValueName() => consumable switch
    {
        ConsumableType.HEALTH => "Health",
        ConsumableType.MONEY => "Money",
        ConsumableType.AMMUNITIONS => "Ammunitions",
        ConsumableType.GRENADE => "Grenades",
        _ => "",
    };
}
