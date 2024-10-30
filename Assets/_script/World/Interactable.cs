using System;
using Asmos.Bus;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(Outline))]
public class Interactable : MonoBehaviour
{
    public UnityEvent action;
    [SerializeField] Color baseColor = Color.white;
    public Color enabledColor = Color.green;
    public Color disabledColor = Color.red;
    public bool canInteract = true;
    public bool isInteractionTimed = false;
    [ShowIf("isInteractionTimed")][SerializeField][Range(1f, 10f)] float interactionTime = 1f;
    [ShowIf("isInteractionTimed")][SerializeField] Slider slider;
    [ShowIf("isInteractionTimed")][SerializeField] CinemachineVirtualCamera cameraTransition;
    [ShowIf("isInteractionTimed")][SerializeField] string animName;
    [ShowIf("isInteractionTimed")][SerializeField] AnimationClip[] interactionAnimations;
    // [ShowIf("isInteractionTimed")][SerializeField] Camera panToCamera;
    [SerializeField][TextArea] string displayedTextEnabled = "Press E to pickup item.";
    [SerializeField][TextArea] string displayedTextDisabled = "Come back in X rounds";

    Outline outline;
    const float MAX_DIST_DISPLAY = 15f;
    const float MIN_DIST_DISPLAY = 5f;
    public static LayerMask layer;
    new public static string tag = "interactable";
    float playerDistance, animatorSpeed;
    bool highlight;
    Timer cooldownTimer, interactionTimer;
    public float cooldown = 2f;
    public bool Highlight
    {
        get => highlight;
        set
        {
            if (value == Highlight)
                return;
            highlight = value;
            UpdateLabel();

        }
    }
    void Awake()
    {
        layer = LayerMask.NameToLayer("Interactable");
        cooldownTimer = new(cooldown);
        interactionTimer = new(interactionTime, FinishInteraction);
        interactionTimer.OnTimerUpdate += () => slider.fillAmount = interactionTimer.GetPercentage();
        outline = GetComponent<Outline>();
        float totalTime = 0f;
        foreach (AnimationClip clip in interactionAnimations)
            totalTime += clip.length;
        animatorSpeed = totalTime / interactionTime;
    }
    void Update()
    {
        playerDistance = (Player.player.transform.position - transform.position).magnitude;
        playerDistance = Math.Clamp(playerDistance, MIN_DIST_DISPLAY, MAX_DIST_DISPLAY);
        UpdateColor();
    }
    void UpdateColor()
    {
        Color newColor = Highlight ? (canInteract ? enabledColor : disabledColor) : baseColor;
        if (!Highlight)
            newColor.a = 1f - (playerDistance - MIN_DIST_DISPLAY) / (MAX_DIST_DISPLAY - MIN_DIST_DISPLAY);
        outline.OutlineColor = newColor;
    }
    public virtual void Interact()
    {
        if (!cooldownTimer.IsStarted() && canInteract)
            action.Invoke();
        cooldownTimer.ResetPlay();
        UpdateLabel();
    }

    public void StartInteracting()
    {
        interactionTimer.ResetPlay();
        Player.player.animator.SetBool(animName, true);
        Player.player.animator.speed = animatorSpeed;
        if (!cameraTransition)
            return;
        cameraTransition.enabled = true;
        slider.LookAt(cameraTransition.transform);
    }

    public void CancelInteracting()
    {
        interactionTimer.Reset();
        Player.player.animator.SetBool(animName, false);
        Player.player.animator.speed = 1f;
        if (!cameraTransition)
            return;
        cameraTransition.enabled = false;
        slider.LookAt(Camera.main.transform);
    }

    void FinishInteraction()
    {
        Interact();
        Player.player.CancelInteracting();
    }

    void UpdateLabel()
    {
        if (Highlight)
            Bus.PushData("interact_label", canInteract ? displayedTextEnabled : displayedTextDisabled);
        else
            Bus.PushData("interact_label", "");
    }
    public void SetDisplayedTextDisabled(string value)
    {
        displayedTextDisabled = value;
        UpdateLabel();
    }
    public void SetDisplayedTextEnabled(string value)
    {
        displayedTextEnabled = value;
        UpdateLabel();
    }
}
