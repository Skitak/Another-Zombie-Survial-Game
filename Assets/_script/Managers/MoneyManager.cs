using Asmos.Bus;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager instance;
    public int startAmount = 0;
    float comboPercent = .1f;
    int money;
    int income { get => (int)StatManager.Get(StatType.INCOME); }
    void Awake()
    {
        instance = this;
        Bus.Subscribe("zombie died", ZombieDied);
        Bus.Subscribe("MONEY_UPDATE", (o) => UpdateMoney((int)(float)o[0], IncomeType.PERK));
    }

    void Start()
    {
        if (startAmount != 0)
        {
            UpdateMoney(startAmount, IncomeType.ROUND);
        }
    }

    void ZombieDied(params object[] args)
    {
        bool headshot = (bool)args[0];
        int amount = income;
        IncomeType type = IncomeType.KILL;
        if (headshot)
        {
            amount *= 2;
            type = IncomeType.HEADSHOT;
        }
        UpdateMoney(amount, type);
        if (ComboManager.instance.combo != 0)
            UpdateMoney((int)(ComboManager.instance.combo * amount * comboPercent), IncomeType.COMBO);
    }

    public void UpdateMoney(int value, IncomeType type)
    {
        money += value;
        Bus.PushData("MONEY", value, type, money);
    }
    public int GetMoney() => money;
}

public enum IncomeType
{
    ROUND, KILL, HEADSHOT, COMBO, PERK, GROUP
}