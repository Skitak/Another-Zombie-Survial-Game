using Asmos.Bus;
using UnityEngine;
using UnityEngine.UI;

public class BusReaderSlider : MonoBehaviour
{
    [SerializeField] string value;
    [SerializeField] string valueMax;
    void Awake()
    {
        Slider slider = GetComponent<Slider>();
        Bus.Subscribe(value, (o) => slider.value = (float)o[0]);
        Bus.Subscribe(valueMax, (o) => slider.maxValue = (float)o[0]);
    }
}
