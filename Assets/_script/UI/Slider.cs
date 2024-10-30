using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

public class Slider : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] Ease fillEase = Ease.Linear;
    [SerializeField][Tooltip("In case slider is in world")] bool sliderLookAtMainCamera;

    [InfoBox("This will trigger the fading out animations if no updates are sent to the slider before this amount of time.")]
    [FoldoutGroup("Appearing and dissapearing")][SerializeField] float timeBeforeFading = 2;
    [FoldoutGroup("Appearing and dissapearing")][SerializeField] bool useAppearAsDissapearRevert;
    [FoldoutGroup("Appearing and dissapearing")][ShowIf("useAppearAsDissapearRevert")][SerializeField][Min(.01f)] float revertSpeed = 1;
    [FoldoutGroup("Appearing and dissapearing")][SerializeField] DOTweenAnimation[] onSliderAppearAnimations;
    [FoldoutGroup("Appearing and dissapearing")][HideIf("useAppearAsDissapearRevert")][SerializeField] DOTweenAnimation[] onSliderDisappearAnimations;

    [SerializeField] DOTweenAnimation[] onSliderFilledAnimations;
    [SerializeField] DOTweenAnimation[] onSliderEmptiedAnimations;

    bool isDisplayed;
    Timer fadeStartTimer;
    LookAtConstraint lookAt;
    float _fillAmount = 0, _fillMax = 1;
    public float fillAmount
    {
        get => _fillAmount;
        set
        {
            _fillAmount = value;
            image.fillAmount = Mathf.Clamp01(DOVirtual.EasedValue(0, _fillMax, _fillAmount / _fillMax, fillEase) / _fillMax);
            if (fillAmount == fillMax)
                PlayFilledAnimations();
            if (fillAmount == 0f)
                PlayEmptiedAnimations();
            RefreshVisuals();
        }
    }
    public float fillMax
    {
        get => _fillMax;
        set
        {
            _fillMax = value;
            image.fillAmount = _fillAmount / _fillMax;
        }
    }
    void Awake()
    {
        if (sliderLookAtMainCamera)
            lookAt = transform.parent.GetComponentInChildren<LookAtConstraint>();
        fadeStartTimer = new(timeBeforeFading, FadeOut);
    }
    void Start()
    {
        if (sliderLookAtMainCamera)
            LookAt(Camera.main.transform);
        InitializeSliderAnimations();
    }

    void InitializeSliderAnimations()
    {
        foreach (DOTweenAnimation anim in onSliderAppearAnimations)
            anim.tween.Complete();
        if (useAppearAsDissapearRevert)
            foreach (DOTweenAnimation anim in onSliderAppearAnimations)
                anim.tween.Rewind();
        else
            foreach (DOTweenAnimation anim in onSliderDisappearAnimations)
                anim.tween.Complete();
    }
    private void RefreshVisuals()
    {
        if (!isDisplayed)
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
        foreach (DOTweenAnimation anim in onSliderEmptiedAnimations)
            anim.tween.Restart();
    }
    void FadeOut()
    {
        isDisplayed = false;
        if (useAppearAsDissapearRevert)
            foreach (DOTweenAnimation anim in onSliderAppearAnimations)
            {
                anim.tween.timeScale = revertSpeed;
                anim.tween.PlayBackwards();
            }
        else
            foreach (DOTweenAnimation anim in onSliderDisappearAnimations)
                anim.tween.Restart();
    }
    void FadeIn()
    {
        isDisplayed = true;
        if (useAppearAsDissapearRevert)
            foreach (DOTweenAnimation anim in onSliderAppearAnimations)
            {
                anim.tween.timeScale = 1f;
                anim.tween.PlayForward();
            }
        else
        {
            ResetFadeOut();
            foreach (DOTweenAnimation anim in onSliderAppearAnimations)
            {
                anim.tween.Restart();
            }
        }
    }

    void ResetFadeOut()
    {
        foreach (DOTweenAnimation anim in onSliderDisappearAnimations)
            anim.tween.Rewind();
    }
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