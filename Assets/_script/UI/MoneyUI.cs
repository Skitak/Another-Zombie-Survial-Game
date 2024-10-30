using System;
using System.Collections.Generic;
using System.Globalization;
using Asmos.Bus;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class MoneyUI : SerializedMonoBehaviour
{
    [SerializeField] GameObject indicatorPrefab;
    [SerializeField] GameObject indicatorParent;
    [SerializeField] float indicatorHeight = 42f;
    [SerializeField] float gap = 10f;
    [SerializeField] float ttlIndicator = .6f;
    [SerializeField] Dictionary<IncomeType, Sprite> incomeIcons = new() { };
    [SerializeField] TMP_Text moneyLabel;
    [SerializeField] float moneyUpdateTime = 1f;
    [SerializeField] AnimationCurve moneyUpdateCurve;
    ObjectPool<MoneyIndicator> pool;
    List<MoneyIndicator> displayedIndicators = new();
    List<object[]> incomesDuringFrame = new();
    int moneyFrom, moneyTo, money = 0;
    Timer moneyUpdateTimer;
    void Awake()
    {
        Bus.Subscribe("money", (o) => incomesDuringFrame.Add(o));

        pool = new(
            () => Instantiate(indicatorPrefab, indicatorParent.transform).GetComponent<MoneyIndicator>(),
            (MoneyIndicator indicator) => { indicator.gameObject.SetActive(true); displayedIndicators.Add(indicator); },
            (MoneyIndicator indicator) => { indicator.gameObject.SetActive(false); displayedIndicators.Remove(indicator); },
            (MoneyIndicator indicator) => Destroy(indicator.gameObject),
            true, 40, 150
        );

        moneyUpdateTimer = new(moneyUpdateTime);
        moneyUpdateTimer.useTimeScale = false;
        moneyUpdateTimer.OnTimerUpdate += () =>
        {
            money = (int)Mathf.Lerp(moneyFrom, moneyTo, moneyUpdateCurve.Evaluate(moneyUpdateTimer.GetPercentage()));
            moneyLabel.SetText(IntFormat(money) + "$");
        };
    }

    void LateUpdate()
    {
        if (incomesDuringFrame.Count == 0)
            return;


        foreach (MoneyIndicator indicator in displayedIndicators)
        {
            indicator.MoveDown((indicatorHeight + gap) * incomesDuringFrame.Count);
        }

        int i = 0;
        foreach (object[] o in incomesDuringFrame)
        {
            MoneyIndicator indicator = pool.Get();
            Sprite sprite = incomeIcons[(IncomeType)o[1]];
            int income = (int)o[0];
            indicator.Play((indicatorHeight + gap) * i, ttlIndicator, "$" + IntFormat(income), sprite);
            Timer.OneShotTimer(ttlIndicator, () => pool.Release(indicator), false);
            ++i;
            moneyTo += income;
        }
        moneyFrom = money;
        moneyUpdateTimer.ResetPlay();
        incomesDuringFrame.Clear();
    }

    string IntFormat(int value) => value.ToString("N0", CultureInfo.CreateSpecificCulture("fr-FR"));
}
