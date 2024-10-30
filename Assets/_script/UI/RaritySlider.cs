using Asmos.Bus;
using Asmos.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class RaritySlider : MonoBehaviour
{
    public Rarity rarity;
    public float lerpTime;
    public Ease valueUpdateCurve;
    public TMP_Text valueLabel;
    int valueFrom, valueTo, value = 0;
    Slider slider;
    Timer valueUpdateTimer;
    Slice slice;
    void Awake()
    {
        slider = GetComponentInChildren<Slider>();

        valueUpdateTimer = new(lerpTime) { useTimeScale = false };
        valueUpdateTimer.OnTimerUpdate += () =>
        {
            value = (int)DOVirtual.EasedValue(valueFrom, valueTo, valueUpdateTimer.GetPercentage(), valueUpdateCurve);
            valueLabel.SetText(value + "%");
            slider.fillAmount = value;
        };
        Bus.Subscribe("drop updated", o => UpdateValue(value));
        UpdateValue(0);
        // Listen to slice event, so that it plays on arriving;
    }

    void Start()
    {
        slider.fillMax = 100f;
        slider.fillAmount = 0f;
    }

    void UpdateValue(int valueFrom)
    {
        this.valueFrom = valueFrom;
        valueTo = PerksManager.instance.rarityChances[(int)rarity];
        valueUpdateTimer.ResetPlay();
    }
}
