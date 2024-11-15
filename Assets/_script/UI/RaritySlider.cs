using Asmos.Bus;
using Asmos.Timers;
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
    }

    void Start()
    {
        slider.fillMax = 100f;
        slider.fillAmount = 0f;
    }

    public void Reset()
    {
        slider.fillAmount = 0f;
        valueLabel.SetText("0%");
    }

    public void UpdateValue(int valueFrom)
    {
        this.valueFrom = valueFrom;
        valueTo = PerksManager.instance.rarityChances[(int)rarity];
        valueUpdateTimer.ResetPlay();
    }
}
