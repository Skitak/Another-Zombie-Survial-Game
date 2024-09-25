using System;
using Asmos.Bus;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(Outline))]
public class Interactable : MonoBehaviour
{
    public UnityEvent action;
    [SerializeField] Color baseColor = Color.white;
    [SerializeField] Color highlightColor = Color.magenta;
    [TextArea] public string displayedText = "Press E to pickup item.";
    Outline outline;
    const float MAX_DIST_DISPLAY = 15f;
    const float MIN_DIST_DISPLAY = 5f;
    float playerDistance;
    bool highlight;
    Timer cooldownTimer;
    public float cooldown = 2f;
    public bool Highlight
    {
        get => highlight;
        set
        {
            if (value == Highlight)
                return;
            if (value == true)
                Bus.PushData("interact_label", displayedText);
            else
                Bus.PushData("interact_label", "");
            highlight = value;

        }
    }

    void Start()
    {
        cooldownTimer = new(cooldown);
        outline = GetComponent<Outline>();
    }
    void Update()
    {
        playerDistance = (Player.player.playerTarget.position - transform.position).magnitude;
        playerDistance = Math.Clamp(playerDistance, MIN_DIST_DISPLAY, MAX_DIST_DISPLAY);
        UpdateColor();
    }
    void UpdateColor()
    {
        Color newColor = Highlight ? highlightColor : baseColor;
        newColor.a = 1f - (playerDistance - MIN_DIST_DISPLAY) / (MAX_DIST_DISPLAY - MIN_DIST_DISPLAY);
        outline.OutlineColor = newColor;
    }
    public void Interact()
    {
        if (!cooldownTimer.IsStarted())
            action.Invoke();
        cooldownTimer.ResetPlay();
    }
}
