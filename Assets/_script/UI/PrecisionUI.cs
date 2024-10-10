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
        Bus.Subscribe("actualPrecision", (o) => UpdateCursor((int)o[0]));
    }
    void UpdateCursor(int precision)
    {
        if (precision == 0)
            return;
        float hypotenuse = distanceToRealCursor / Mathf.Cos((precision / 2) * Mathf.Deg2Rad);
        disc.Radius = Mathf.Sin((int)(precision / 2) * Mathf.Deg2Rad) * hypotenuse;
    }
}
