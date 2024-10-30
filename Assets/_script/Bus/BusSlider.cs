using Asmos.Bus;
using UnityEngine;

[RequireComponent(typeof(Slider))]
public class BusSlider : MonoBehaviour
{
    [SerializeField] string fillKey;
    [SerializeField] string fillMaxKey;
    void Awake()
    {
        Slider slider = GetComponent<Slider>();
        if (fillKey != "")
            Bus.Subscribe(fillKey, (o) => slider.fillAmount = (float)o[0]);

        if (fillMaxKey != "")
            Bus.Subscribe(fillMaxKey, (o) => slider.fillMax = (float)o[0]);
    }

}