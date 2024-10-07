using Asmos.Bus;
using UnityEngine;
using UnityEngine.UI;

public class RefreshPerksButton : MonoBehaviour
{
    Timer refreshCooldown;
    Button button;
    void Start()
    {
        button = GetComponent<Button>();
        refreshCooldown = new(.5f);
        Bus.Subscribe("refreshPerks", (o) =>
        {
            var refresh = (int)o[0];
            button.enabled = refresh > 0;
        });
    }

    public void RefreshPerks()
    {
        if (Player.player.perkRefresh <= 0 || refreshCooldown.IsStarted())
            return;
        refreshCooldown.ResetPlay();
        PerksManager.instance.RefreshPerks();
        --Player.player.perkRefresh;
    }
}
