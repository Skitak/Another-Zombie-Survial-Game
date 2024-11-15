using System;
using Asmos.Bus;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(BaseButton))]
public class PriceButton : MonoBehaviour
{
    [SerializeField] bool usedOnce = false;
    [SerializeField] int price;
    [SerializeField] BaseButton button;
    [SerializeField] IncomeType incomeType = IncomeType.PERK;
    bool isUsed = false;
    void Awake() => Bus.Subscribe("MONEY", o => button.SetActive(((int)o[2] >= price) && !(isUsed && usedOnce)));
    void Start() => button.SetActive(MoneyManager.instance.GetMoney() >= price);
    public void PayPrice()
    {
        if (usedOnce)
            button.SetActive(false);
        isUsed = true;
        MoneyManager.instance.UpdateMoney(-price, incomeType);
    }
}
