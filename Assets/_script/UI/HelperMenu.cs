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

// public void LookAt(Transform lookAtTransform)
// {
//     List<ConstraintSource> sources = new();
//     lookAt.GetSources(sources);
//     bool found = false;
//     for (int i = 0; i < sources.Count; i++)
//     {
//         ConstraintSource source = lookAt.GetSource(i);
//         source.weight = 0;
//         if (source.sourceTransform == lookAtTransform)
//         {
//             source.weight = 1;
//             found = true;
//         }
//         lookAt.SetSource(i, source);
//     }
//     if (!found)
//         lookAt.AddSource(new ConstraintSource()
//         {
//             sourceTransform = lookAtTransform,
//             weight = 1
//         });
// }
