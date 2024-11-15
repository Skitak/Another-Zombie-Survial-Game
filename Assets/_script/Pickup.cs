using Asmos.Bus;
using Asmos.Timers;
using Sirenix.OdinInspector;
using UnityEngine;
using Timer = Asmos.Timers.Timer;

public class Pickup : MonoBehaviour
{
    const float PICKUP_DISTANCE = 1f;
    public PickupType type;
    [ShowIf("type", PickupType.PERK)][SerializeField] Perk perk;
    [InlineButton("TogglePercent", "@asPercent?\"Percent\":\"Flat\"")]
    [LabelText("@type == PickupType.HEALTH ? \"Health\" : \"Grenades\"")]
    [HideIf("type", PickupType.PERK)][SerializeField] int value;
    [SerializeField][HideInInspector] bool asPercent;
    void TogglePercent() => asPercent = !asPercent;
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
                if (!asPercent)
                {
                    Player.player.health += value;
                    Bus.PushData("bonus label", $"+{value} health");
                }
                else
                {
                    Player.player.health += (int)(Player.player.healthMax * value / 100f);
                    Bus.PushData("bonus label", $"+{value}% health");
                }
                break;
            case PickupType.GRENADES:
                Player.player.grenades += value;
                Bus.PushData("bonus label", $"+{value} grenades");
                break;
            case PickupType.PERK:
                perk.ApplyUpgrades(true);
                Bus.PushData("bonus label", "+" + perk.GetLabel());
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
        Destroy(gameObject);
    }
}

public enum PickupType
{
    HEALTH, GRENADES, PERK,
}

[System.Serializable]
public struct PickupChances
{
    [TableColumnWidth(60)]
    public GameObject pickup;
    [TableColumnWidth(40)]
    public float dropRate;
}