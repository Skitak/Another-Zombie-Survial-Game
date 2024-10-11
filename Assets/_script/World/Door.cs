using Asmos.Bus;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] int wave;
    bool isDoorOpen;
    void Start()
    {
        Bus.Subscribe("wave start", (o) =>
        {
            int newWave = (int)o[0];
            if (newWave >= wave && !isDoorOpen)
                RemoveDoor();
            else if (newWave < wave && isDoorOpen)
                DisplayDoor();
        });
    }

    void RemoveDoor()
    {
        isDoorOpen = true;
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            child.SetActive(!gameObject.activeSelf);
        }
    }

    void DisplayDoor()
    {
        isDoorOpen = false;
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            child.SetActive(!gameObject.activeSelf);
        }
    }
}
