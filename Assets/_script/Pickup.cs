using System;
using Asmos.Bus;
using Sirenix.OdinInspector;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    const float PICKUP_DISTANCE = 1f;
    public PickupType type;
    [ShowIf("type", PickupType.PERK)][SerializeField] Perk perk;
    [ShowIf("type", PickupType.PERK)][SerializeField] Rarity rarity;
    [ShowIf("type", PickupType.HEALTH)][SerializeField] int health;
    [ShowIf("type", PickupType.HEALTH)][SerializeField][Range(0, 1)] float healthPercentage;
    [ShowIf("type", PickupType.GRENADES)][SerializeField] int grenades;
    bool pickedUp = false;
    float minTimeFlash = 0.05f;
    float maxTimeFlash = .8f;
    float initialTime = 20f;
    float dyingTime = 10f;
    Timer initialTimer, dyingTimer, flashOnTimer, flashOffTimer;
    void Start()
    {
        GameObject mesh = transform.GetChild(0).gameObject;
        dyingTimer = new Timer(dyingTime, () => DeletePickup());
        flashOnTimer = new Timer(maxTimeFlash, () =>
        {
            flashOffTimer.endTime = Mathf.Lerp(minTimeFlash, maxTimeFlash, dyingTimer.GetPercentageLeft());
            flashOffTimer.ResetPlay();
            mesh.SetActive(false);
        });
        flashOffTimer = new Timer(maxTimeFlash, () =>
        {
            flashOnTimer.endTime = Mathf.Lerp(minTimeFlash, maxTimeFlash, dyingTimer.GetPercentageLeft());
            flashOnTimer.ResetPlay();
            mesh.SetActive(true);
        });
        initialTimer = new Timer(initialTime, () =>
        {
            dyingTimer.Play();
            flashOffTimer.Play();
            mesh.SetActive(false);
        }).Play();
    }
    void Update()
    {
        if (Vector3.Distance(Player.player.transform.position, transform.position) < PICKUP_DISTANCE && !pickedUp)
            ApplyPickup();
    }
    void ApplyPickup()
    {
        pickedUp = true;
        switch (type)
        {
            case PickupType.HEALTH:
                if (health != 0)
                {
                    Player.player.health += health;
                    Bus.PushData("bonus label", $"+{health} health");
                }
                else
                {
                    Player.player.health += (int)(Player.player.healthMax * healthPercentage);
                    Bus.PushData("bonus label", $"+{healthPercentage * 100}% health");
                }
                break;
            case PickupType.GRENADES:
                Player.player.grenades += grenades;
                Bus.PushData("bonus label", $"+{grenades} grenades");
                break;
            case PickupType.PERK:
                PerksManager.instance.AddPerk(perk, rarity);
                Bus.PushData("bonus label", "+" + perk.GetLabel(rarity));
                break;
            default:
                break;
        }
        DeletePickup();
    }

    void DeletePickup()
    {
        TimerManager.RemoveTimer(initialTimer);
        TimerManager.RemoveTimer(dyingTimer);
        TimerManager.RemoveTimer(flashOnTimer);
        TimerManager.RemoveTimer(flashOffTimer);
        Destroy(this.gameObject);
    }
}

public enum PickupType
{
    HEALTH, GRENADES, PERK,
}

[Serializable]
public struct PickupChances
{
    public float dropRate;
    public GameObject pickup;
}