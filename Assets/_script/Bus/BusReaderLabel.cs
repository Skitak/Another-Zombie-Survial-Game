using Asmos.Bus;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class BusReaderLabel : MonoBehaviour
{
    [SerializeField] string busKey;
    TMP_Text label;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        label = GetComponent<TMP_Text>();
        Bus.Subscribe(busKey, (o) => label.SetText((string)o[0]));
    }
}
