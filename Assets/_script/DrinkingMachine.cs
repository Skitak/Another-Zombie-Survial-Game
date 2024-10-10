using Asmos.Bus;
using UnityEngine;

public class DrinkingMachine : MonoBehaviour
{
    public Perk perk;
    public Rarity rarity;
    [Min(1)] public int roundsCooldown;
    int roundsInCooldown;
    Interactable interactable;
    bool _isPowerOn;
    bool isPowerOn
    {
        get => _isPowerOn;
        set
        {
            _isPowerOn = value;
            interactable.canInteract = CanDrink();
            UpdateDisabledText();
            // Play animations accordingly
        }
    }
    void Awake()
    {
        roundsInCooldown = roundsCooldown;
        interactable = GetComponent<Interactable>();
    }
    void Start()
    {
        DisableMachine();
        isPowerOn = true;
        Bus.Subscribe("wave start", (o) =>
        {
            ++roundsInCooldown;
            if (CanDrink())
                EnableMachine();
            UpdateDisabledText();
        });
        Bus.Subscribe("set power", (o) => isPowerOn = (bool)o[0]);
    }

    public void Drink()
    {
        if (!CanDrink())
            return;
        roundsInCooldown = 0;
        DisableMachine();
        PerksManager.instance.AddPerk(perk, rarity);
        Bus.PushData("bonus label", perk.GetLabel(rarity));
        Player.player.Drink();
    }
    void EnableMachine()
    {
        interactable.canInteract = true;
        // TODO: Play animations feedbacks
    }
    void DisableMachine()
    {
        interactable.canInteract = false;
        UpdateDisabledText();
        // TODO: Play animations feedbacks
    }
    private bool CanDrink() => roundsInCooldown >= roundsCooldown && isPowerOn;
    void UpdateDisabledText()
    {
        if (!isPowerOn)
            interactable.displayedTextDisabled = $"Turn the power on to use the machine.";
        else
        {
            string plural = roundsCooldown - roundsInCooldown > 1 ? "s" : "";
            interactable.displayedTextDisabled = $"Enabled in {roundsCooldown - roundsInCooldown} round{plural}.";
        }
    }
}
