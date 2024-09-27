using UnityEngine;

public class PerksManager : MonoBehaviour
{
    public static PerksManager instance;
    void Start()
    {
        instance = this;
    }
}
