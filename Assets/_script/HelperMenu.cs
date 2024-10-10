using UnityEngine;

public class HelperMenu : MonoBehaviour
{
    bool used = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(Player.player.transform.position, transform.position) < 2 && !used)
        {
            used = true;
            PerksManager.instance.OpenPerksMenu();
        }
    }
}
