using Asmos.Bus;
using UnityEngine;

public class ElectricityMachine : Interactable
{
    [SerializeField] int minRoundsBeforeTurningOff = 2;
    [SerializeField] int maxRoundsBeforeTurningOff = 5;

    bool _isPowerOn = false;
    int roundsBeforeTurningOff;
    bool isPowerOn
    {
        get => _isPowerOn;
        set
        {
            _isPowerOn = value;
            canInteract = !value;
            Bus.PushData("set power", value);
        }
    }
    void Start()
    {
        canInteract = true;
        Bus.Subscribe("new wave", (o) =>
        {
            if (isPowerOn)
                --roundsBeforeTurningOff;

            if (roundsBeforeTurningOff <= 0)
            {
                isPowerOn = false;
                roundsBeforeTurningOff = Random.Range(minRoundsBeforeTurningOff, maxRoundsBeforeTurningOff);
            }
        });
    }
    public void SetPowerOn() => isPowerOn = true;

}
