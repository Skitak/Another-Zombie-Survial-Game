using Asmos.Bus;
using Asmos.Timers;
using Sirenix.OdinInspector;
using UnityEngine;
using Timer = Asmos.Timers.Timer;

public class Pickup : MonoBehaviour
{
    const float PICKUP_DISTANCE = 1f;
    [SerializeField] Perk perk;
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
        perk.ApplyModifiers(true);
        Bus.PushData("bonus label", "+" + perk.GetLabel());
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