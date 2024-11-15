using Asmos.Bus;
using Asmos.Timers;
using UnityEngine;

public class ComboManager : MonoBehaviour
{
    public static ComboManager instance;
    [SerializeField] float baseComboTime;
    int _combo;
    public float comboTime
    {
        get => comboTimer.endTime;
        set
        {
            comboTimer.endTime = value;
            comboTimer.ResetPlay();
        }
    }
    Timer comboTimer;

    public int combo
    {
        get => _combo;
        set
        {
            _combo = value;
            Bus.PushData("COMBO", _combo);
            if (_combo != 0)
                comboTimer.ResetPlay();
        }
    }
    void Awake()
    {
        instance = this;
        comboTimer = new(baseComboTime, () => combo = 0);
        comboTimer.OnTimerUpdate += () => Bus.PushData("combo timer", comboTimer.GetPercentageLeft());
        // comboTimer.OnTimerStart += () => Bus.PushData("combo timer", 1f);
        Bus.Subscribe("zombie died", (o) => combo += 1);
    }

}
