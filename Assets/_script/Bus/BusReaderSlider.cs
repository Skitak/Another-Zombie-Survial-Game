using System.Collections.Generic;
using System.Data;
using Asmos.Bus;
using Cinemachine;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BusReaderSlider : MonoBehaviour
{
    [SerializeField] SliderKind kind;

    [SerializeField] bool usesBus = true;
    [ShowIf("usesBus")][SerializeField] string value;
    [ShowIf("usesBus")][ShowIf("kind", SliderKind.UNITY_SLIDER)][SerializeField] string valueMax;

    [SerializeField] bool fadeAfterTime;
    [ShowIf("fadeAfterTime")][SerializeField] float timeBeforeFading;
    [ShowIf("fadeAfterTime")][SerializeField] float fadeInTime;
    [ShowIf("fadeAfterTime")][SerializeField] float fadeOutTime;

    [SerializeField][Tooltip("In case slider is in world")] bool sliderLookAtMainCamera;
    [SerializeField] bool useAppearAsDissapearRevert;
    [SerializeField] DOTweenAnimation[] onSliderFilledAnimations;
    [SerializeField] DOTweenAnimation[] onSliderEmptiedAnimations;
    [SerializeField] DOTweenAnimation[] onSliderAppearAnimations;
    [HideIf("useAppearAsDissapearRevert")][SerializeField] DOTweenAnimation[] onSliderDissapearAnimations;

    Timer fadeStartTimer, fadeTimer;
    CanvasGroup canvasGroup;
    Slider slider;
    Image image;
    LookAtConstraint lookAt;
    public float fillAmount
    {
        get => kind == SliderKind.UNITY_SLIDER ? slider.value : image.fillAmount;
        set
        {
            if (kind == SliderKind.UNITY_SLIDER)
                slider.value = value;
            else
                image.fillAmount = value;

            if (fillAmount == fillMax)
                PlayFilledAnimations();
            if (fillAmount == 0f)
                PlayEmptiedAnimations();
            RefreshVisuals();
        }
    }

    public float fillMax
    {
        get => kind == SliderKind.UNITY_SLIDER ? slider.maxValue : 1;
        set
        {
            if (kind != SliderKind.UNITY_SLIDER)
                return;
            if (slider.value > value)
                slider.value = value;
            slider.maxValue = value;
        }
    }

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (kind == SliderKind.UNITY_SLIDER)
            slider = GetComponent<Slider>();
        else
            image = GetComponent<Image>();

        if (sliderLookAtMainCamera)
            lookAt = transform.parent.GetComponentInChildren<LookAtConstraint>();

        Bus.Subscribe(value, (o) => fillAmount = (float)o[0]);
        if (kind == SliderKind.UNITY_SLIDER)
            Bus.Subscribe(valueMax, (o) => fillMax = (float)o[0]);

        fadeStartTimer = new(timeBeforeFading, FadeOut);
        fadeTimer = new(fadeInTime)
        {
            useTimeScale = false,
            useUpdateAsRewindAction = true
        };
        fadeTimer.rewindSpeed = fadeInTime / fadeOutTime;
        fadeTimer.OnTimerUpdate += () => canvasGroup.alpha = fadeTimer.GetPercentage();
    }

    void Start()
    {
        if (fadeAfterTime)
            fadeStartTimer.ResetPlay();

        if (sliderLookAtMainCamera)
        {
            lookAt.AddSource(new ConstraintSource()
            {
                sourceTransform = Camera.main.transform,
                weight = 1
            });
        }
    }

    private void RefreshVisuals()
    {
        if (!fadeAfterTime) return;
        FadeIn();
        fadeStartTimer.ResetPlay();
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
    public void LookAt(Transform lookAtTransform)
    {
        List<ConstraintSource> sources = new();
        lookAt.GetSources(sources);
        bool found = false;
        for (int i = 0; i < sources.Count; i++)
        {
            ConstraintSource source = lookAt.GetSource(i);
            source.weight = 0;
            if (source.sourceTransform == lookAtTransform)
            {
                source.weight = 1;
                found = true;
            }
            lookAt.SetSource(i, source);
        }
        if (!found)
            lookAt.AddSource(new ConstraintSource()
            {
                sourceTransform = lookAtTransform,
                weight = 1
            });
    }
}

public enum SliderKind
{
    UNITY_SLIDER, TEXTURE
}