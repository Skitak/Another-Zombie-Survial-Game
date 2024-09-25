using Asmos.Bus;
using UnityEngine;

// Used with buttons, to play a bus key
public class BusPlayer : MonoBehaviour
{
    public string busKey;

    public void PlayBusKey()
    {
        Bus.PushData(busKey);
    }
}
