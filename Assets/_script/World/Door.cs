using Asmos.Bus;
using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour
{
    [SerializeField] int wave;
    bool isDoorOpen;
    NavMeshObstacle navMeshObstacle;
    void Start()
    {
        navMeshObstacle = GetComponent<NavMeshObstacle>();
        Bus.Subscribe("wave start", (o) =>
        {
            int newWave = (int)o[0];
            if (newWave >= wave && !isDoorOpen)
                RemoveDoor();
            else if (newWave < wave && !isDoorOpen)
                DisplayDoor();
        });
    }

    void RemoveDoor()
    {
        isDoorOpen = false;
        navMeshObstacle.enabled = false;
        for (int i = 0; i < transform.childCount; i++)
        {
            gameObject.transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    void DisplayDoor()
    {
        isDoorOpen = true;
        navMeshObstacle.enabled = true;
        for (int i = 0; i < transform.childCount; i++)
        {
            gameObject.transform.GetChild(i).gameObject.SetActive(true);
        }
    }
}
