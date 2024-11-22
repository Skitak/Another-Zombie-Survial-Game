using System;
using Asmos.Bus;

[Serializable]
public class ContextStat
{
    protected bool isInitialized = false;
    public virtual void Initialize() => isInitialized = true;
    public virtual ContextStat Clone() => (ContextStat)MemberwiseClone();
    public virtual int GetValue() => 0;
    public virtual string GetStatName(bool plural = true) => "";
    public virtual string GetLabel(bool withContext, bool plural)
    {
        string currentValue = withContext ? $"(current: {GetValue()})" : "";
        return $"{GetStatName(plural)}{currentValue}";
    }
    public virtual void Listen(Bus.GenericDelegate action) => Bus.Subscribe(Buskey(), action);
    public void StopListening(Bus.GenericDelegate action) => Bus.Unsubscribe(Buskey(), action);
    public virtual string Buskey() => "";
}
public class ContextStatFlat : ContextStat
{
    public enum Stat { HEALTH, HEALTH_MISSING, AMMO, AMMO_MISSING, MONEY, COMBO, WAVE, HEADSHOT, KILL, RELOAD }
    public Stat stat;
    public override string GetStatName(bool plural = true)
    {
        string pluralStr = plural ? "s" : "";
        return stat switch
        {
            Stat.HEALTH => "health left",
            Stat.HEALTH_MISSING => "missing Health",
            Stat.AMMO => $"ammunition{pluralStr} left",
            Stat.AMMO_MISSING => $"missing ammunition{pluralStr}",
            Stat.MONEY => "money",
            Stat.COMBO => "combo",
            Stat.WAVE => $"wave{pluralStr}",
            Stat.HEADSHOT => $"headshot kill{pluralStr} since game started",
            Stat.KILL => $"zombie kill{pluralStr} since game started",
            Stat.RELOAD => $"reload{pluralStr} since game started",
            _ => "",
        };
    }
    public override int GetValue() => stat switch
    {
        Stat.HEALTH => Player.player.health,
        Stat.HEALTH_MISSING => Player.player.healthMax - Player.player.health,
        Stat.AMMO => Player.player.weapon.ammo,
        Stat.AMMO_MISSING => Player.player.weapon.ammoMax - Player.player.weapon.ammo,
        Stat.MONEY => MoneyManager.instance.GetMoney(),
        Stat.COMBO => ComboManager.instance.combo,
        Stat.WAVE => WaveManager.instance.GetWaveCount(),
        Stat.HEADSHOT => Zombie.headshots,
        Stat.KILL => Zombie.kills,
        Stat.RELOAD => Weapon.reloads,
        _ => 0,
    };
    // public override ContextStat Clone() => (ContextStat)MemberwiseClone();
    public override string Buskey() => stat.ToString();
}
public class ContextStatPercent : ContextStat
{
    public enum Stat { HEALTH, HEALTH_MISSING, AMMO, AMMO_MISSING }
    public Stat stat;
    public override int GetValue() => stat switch
    {
        Stat.HEALTH => (int)(Player.player.health / (float)Player.player.healthMax) * 100,
        Stat.HEALTH_MISSING => (((int)(Player.player.health / (float)Player.player.healthMax) * 100) - 100) * -1,
        Stat.AMMO => (int)(Player.player.weapon.ammo / (float)Player.player.weapon.ammoMax) * 100,
        Stat.AMMO_MISSING => (((int)(Player.player.weapon.ammo / (float)Player.player.weapon.ammoMax) * 100) - 100) * -1,
        _ => 0,
    };
    public override string GetStatName(bool plural = true)
    {
        string pluralStr = plural ? "s" : "";
        return stat switch
        {
            Stat.HEALTH => "health left",
            Stat.HEALTH_MISSING => "missing health",
            Stat.AMMO => $"ammunition{pluralStr} left",
            Stat.AMMO_MISSING => $"missing ammunition{pluralStr}",
            _ => "",
        };
    }
    // public override ContextStat Clone() => (ContextStat)MemberwiseClone();
    public override string Buskey() => stat.ToString();
}
public class ContextStatIncremental : ContextStat
{
    public enum Stat { WAVE, HEADSHOT, KILL, RELOAD }
    public Stat stat;
    int baseValue = 0;
    public override void Initialize()
    {
        base.Initialize();
        baseValue = GetCurrentValue();
        if (stat == Stat.WAVE)
            baseValue++;
    }
    public override int GetValue() => isInitialized ? (GetCurrentValue() - baseValue) : 0;
    int GetCurrentValue() => stat switch
    {
        Stat.WAVE => WaveManager.instance.GetWaveCount(),
        Stat.HEADSHOT => Zombie.headshots,
        Stat.KILL => Zombie.kills,
        Stat.RELOAD => Weapon.reloads,
        _ => 0,
    };
    public override string GetLabel(bool withContext, bool plural)
    {
        string suffix = "";
        if (isInitialized && withContext)
            suffix += $" since purchase (current:{GetValue()}).";
        else
            suffix += " after purchase.";

        return $"{GetStatName(plural)}{suffix}";
    }
    public override string GetStatName(bool plural = true)
    {
        string pluralStr = plural ? "s" : "";
        return stat switch
        {
            Stat.WAVE => $"wave{pluralStr}",
            Stat.HEADSHOT => $"headshot kill{pluralStr}",
            Stat.KILL => $"zombie{pluralStr} killed",
            Stat.RELOAD => $"reload{pluralStr}",
            _ => "",
        };
    }
    // public override ContextStat Clone() => (ContextStat)MemberwiseClone();
    public override string Buskey() => stat.ToString();
}
