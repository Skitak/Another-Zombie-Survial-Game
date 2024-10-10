using Asmos.Bus;
using UnityEngine;

// Used with buttons, to play a bus key
public class BusPlayer : MonoBehaviour
{
    public string busKey;
    public void PlayBusKey() => Bus.PushData(busKey);
    public void PlayBusKeyInt(int value) => Bus.PushData(busKey, value);
    public void PlayBusKeyFloat(float value) => Bus.PushData(busKey, value);
    public void PlayBusKeyString(string value) => Bus.PushData(busKey, value);
    public void PlayBusKeyBool(bool value) => Bus.PushData(busKey, value);
    public void PlayBusKeyVector2(Vector2 value) => Bus.PushData(busKey, value);
    public void PlayBusKeyVector3(Vector3 value) => Bus.PushData(busKey, value);
    public void PlayBusKeyVector2Int(Vector2Int value) => Bus.PushData(busKey, value);
    public void PlayBusKeyVector3Int(Vector3Int value) => Bus.PushData(busKey, value);

}
