using Asmos.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class InGameUIManager : MonoBehaviour
{
    [SerializeField] ViewConfig pauseView;
    InputAction pauseAction;
    bool isInPause;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        pauseAction = InputSystem.actions.FindAction("Pause");
    }

    // Update is called once per frame
    void Update()
    {

        if (pauseAction.WasPressedThisFrame())
        {
            if (!isInPause)
                SetPause();
            else
                RemovePause();
        }

    }
    void SetPause()
    {
        Player.player.SetMovementEnabled(false);
        Cursor.lockState = CursorLockMode.Confined;
        isInPause = true;
        ViewManager.instance.AddView(pauseView);
        if (!PerksManager.instance.isOpened)
            Time.timeScale = 0;
    }

    void RemovePause()
    {
        ViewManager.instance.RemoveView();
        if (!PerksManager.instance.isOpened)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Time.timeScale = 0;
        }

        isInPause = false;
        Player.player.SetMovementEnabled(true);
    }
}
