using Asmos.Bus;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager instance;
    public int startAmount = 0;
    const int ZOMBIE_MONEY = 50;
    const int COMBO_MONEY = 5;
    int money;
    void Awake()
    {
        instance = this;
        Bus.Subscribe("zombie died", ZombieDied);
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
        if (headshot)
            UpdateMoney(ZOMBIE_MONEY * 2, IncomeType.HEADSHOT);
        else
            UpdateMoney(ZOMBIE_MONEY, IncomeType.KILL);
        if (ComboManager.instance.combo != 0)
            UpdateMoney(ComboManager.instance.combo * COMBO_MONEY, IncomeType.COMBO);
    }

    public void UpdateMoney(int value, IncomeType type)
    {
        money += value;
        Bus.PushData("money", value, type, money);
    }
    public int GetMoney() => money;
}

public enum IncomeType
{
    ROUND, KILL, HEADSHOT, COMBO, PERK, GROUP
}