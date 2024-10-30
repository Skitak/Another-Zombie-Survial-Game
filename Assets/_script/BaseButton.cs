using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaseButton : MonoBehaviour
{
    TMP_Text[] texts;
    [SerializeField] Image icon;
    [SerializeField] Image hoverBackground, normalBackground;
    [SerializeField] Color hoverColor, normalColor;
    [SerializeField] DOTweenAnimation[] clickAnimations;
    [SerializeField] DOTweenAnimation[] deactivateAnimations;
    [SerializeField] DOTweenAnimation[] inactiveClickAnimations;
    [SerializeField] UnityEvent buttonAction;
    [SerializeField] bool startActive = true;
    bool isActive = true;
    void Awake()
    {
        texts = GetComponentsInChildren<TMP_Text>();
        SetActive(startActive);
    }
    public virtual void OnClick()
    {
        if (!isActive)
        {
            foreach (DOTweenAnimation anim in inactiveClickAnimations)
                anim.tween.Restart();
            return;
        }
        foreach (DOTweenAnimation anim in clickAnimations)
            anim.tween.Restart();
        buttonAction?.Invoke();
    }

    public void SetActive(bool active)
    {
        if (isActive == active)
            return;
        if (active)
            foreach (DOTweenAnimation anim in deactivateAnimations)
                anim.tween.Rewind();
        else
        {
            foreach (DOTweenAnimation anim in deactivateAnimations)
                anim.tween.Restart();
            SetHover(false);
        }
        isActive = active;
    }
    public void SetHover(bool value)
    {
        if (!isActive)
            return;
        hoverBackground.gameObject.SetActive(value);
        normalBackground.gameObject.SetActive(!value);
        foreach (TMP_Text text in texts)
            text.color = value ? hoverColor : normalColor;
        icon.color = value ? hoverColor : normalColor;
    }
}
