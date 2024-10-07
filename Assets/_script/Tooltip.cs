using DG.Tweening;
using TMPro;
using UnityEngine;

public class Tooltip : MonoBehaviour
{
    [SerializeField] string text;
    [SerializeField] float distance = 4f;

    bool playerInRange;
    CanvasGroup canvasGroup;
    DOTweenAnimation[] animations;
    void Awake()
    {
        canvasGroup = GetComponentInChildren<CanvasGroup>();
        animations = GetComponentsInChildren<DOTweenAnimation>();
        GetComponentInChildren<TMP_Text>().SetText(text);
        // canvasGroup.alpha = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 cam = new(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
        transform.LookAt(cam + transform.position);
        if (!playerInRange && Vector3.Distance(transform.position, Player.player.transform.position) < distance)
        {
            playerInRange = true;
            foreach (DOTweenAnimation anim in animations)
            {
                // anim.tween.Rewind();
                anim.tween.Restart();
            }
            // Play animations
        }
        if (playerInRange && Vector3.Distance(transform.position, Player.player.transform.position) > distance)
        {
            playerInRange = false;
            foreach (DOTweenAnimation anim in animations)
            {
                // anim.tween.Rewind();
                anim.tween.PlayBackwards();
            }
            // Play animations
        }
    }
}
