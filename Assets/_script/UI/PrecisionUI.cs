using Asmos.Bus;
using Shapes;
using UnityEngine;

public class PrecisionUI : MonoBehaviour
{
    public float distanceToRealCursor = 10f;
    Disc disc;
    void Start()
    {
        disc = GetComponent<Disc>();
        Bus.Subscribe("actualPrecision", (o) => UpdateCursor((float)o[0]));
    }
    void UpdateCursor(float precision)
    {
        if (precision == 0)
            return;
        float hypotenuse = distanceToRealCursor / Mathf.Cos(precision / 2f * Mathf.Deg2Rad);
        disc.Radius = Mathf.Sin(precision / 2f * Mathf.Deg2Rad) * hypotenuse;
    }
}
