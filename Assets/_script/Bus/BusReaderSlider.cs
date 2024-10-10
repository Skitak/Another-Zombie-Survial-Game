using Asmos.Bus;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BusReaderSlider : MonoBehaviour
{
    [SerializeField] SliderKind kind;
    [SerializeField] string value;
    [ShowIf("kind", SliderKind.UNITY_SLIDER)][SerializeField] string valueMax;
    [SerializeField] bool fadeAfterTime;
    [ShowIf("fadeAfterTime")][SerializeField] float timeBeforeFading;
    [ShowIf("fadeAfterTime")][SerializeField] float fadeInTime;
    [ShowIf("fadeAfterTime")][SerializeField] float fadeOutTime;

    [SerializeField] DOTweenAnimation[] onSliderFilledAnimations;
    [SerializeField] DOTweenAnimation[] onSliderEmptiedAnimations;

    Timer fadeStartTimer, fadeTimer;
    CanvasGroup canvasGroup;
    Slider slider;
    Image image;
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (kind == SliderKind.UNITY_SLIDER)
            slider = GetComponent<Slider>();
        else
            image = GetComponent<Image>();
    }

    void Start()
    {
        fadeStartTimer = new(timeBeforeFading, FadeOut);
        fadeTimer = new(fadeInTime)
        {
            useTimeScale = false,
            useUpdateAsRewindAction = true
        };
        fadeTimer.rewindSpeed = fadeInTime / fadeOutTime;
        fadeTimer.OnTimerUpdate += () => canvasGroup.alpha = fadeTimer.GetPercentage();

        if (fadeAfterTime)
            fadeStartTimer.ResetPlay();

        Bus.Subscribe(value, (o) =>
        {
            if (kind == SliderKind.UNITY_SLIDER)
            {
                slider.value = (float)o[0];
                if (slider.value == slider.maxValue)
                    PlayFilledAnimations();
                if (slider.value == 0f)
                    PlayEmptiedAnimations();
            }
            else
            {
                image.fillAmount = (float)o[0];
                if (image.fillAmount == 1f)
                    PlayFilledAnimations();
                if (image.fillAmount == 0f)
                    PlayEmptiedAnimations();
            }
            RefreshVisuals();
        });
        if (kind == SliderKind.UNITY_SLIDER)
            Bus.Subscribe(valueMax, (o) =>
            {
                slider.maxValue = (float)o[0];
                RefreshVisuals();
            });
    }

    private void RefreshVisuals()
    {
        if (fadeAfterTime)
        {
            FadeIn();
            fadeStartTimer.ResetPlay();
        }
    }
    void PlayFilledAnimations()
    {
        foreach (DOTweenAnimation anim in onSliderFilledAnimations)
            anim.tween.Restart();
    }
    void PlayEmptiedAnimations()
    {
        if (fadeTimer.IsPlayingForward() || fadeTimer.IsRewinding())
            return;
        foreach (DOTweenAnimation anim in onSliderEmptiedAnimations)
            anim.tween.Restart();
    }
    void FadeOut() => fadeTimer.Rewind();
    void FadeIn() => fadeTimer.Play();
}

public enum SliderKind
{
    UNITY_SLIDER, TEXTURE
}