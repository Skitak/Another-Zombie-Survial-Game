using Asmos.Bus;
using Asmos.UI;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] ViewConfig mainView;
    [SerializeField] ViewConfig endView;
    public static GameManager instance;
    void Start()
    {
        instance = this;
        Bus.Subscribe("replay", RestartGame);
        Bus.Subscribe("exit", ExitGame);
    }

    public void EndGame()
    {
        ViewManager.instance.SwapTip(endView);
        Cursor.lockState = CursorLockMode.Confined;
        WaveManager.instance.EndGame();
    }

    void RestartGame(params object[] args)
    {
        ViewManager.instance.SwapTip(mainView);
        Cursor.lockState = CursorLockMode.Locked;
        WaveManager.instance.RestartGame();
        ZombieSpawnerManager.instance.RestartGame();
        Player.player.RestartGame();
    }

    void ExitGame(params object[] args)
    {
        Application.Quit();
    }
}
