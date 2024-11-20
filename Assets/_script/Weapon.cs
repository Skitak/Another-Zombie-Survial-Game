using Asmos.Bus;
using UnityEngine;
using UnityEngine.Pool;
using Shapes;
using Sirenix.OdinInspector;
using System;
using DG.Tweening;
using Asmos.Timers;
public class Weapon : MonoBehaviour
{
    public static int reloads = 0;
    #region exposedParameters
    [FoldoutGroup("Boilerplate")] public StatKit statKit;
    [FoldoutGroup("Boilerplate")] public Transform fireStart;
    [FoldoutGroup("Boilerplate")][SerializeField] GameObject decal;
    [FoldoutGroup("Boilerplate")][SerializeField] GameObject linePrefab;
    [FoldoutGroup("Boilerplate")][SerializeField] AnimationClip reloadAnimation;
    [FoldoutGroup("Boilerplate")][SerializeField] AnimationClip fireAnimation;
    [FoldoutGroup("Boilerplate")][SerializeField] ParticleSystem muzzleFlash;
    [FoldoutGroup("Boilerplate")][SerializeField] float decalTimeout = 10f;
    [FoldoutGroup("Boilerplate")][SerializeField] LayerMask fireLayerMask;
    [FoldoutGroup("Boilerplate")] public int animLayer;
    [FoldoutGroup("Boilerplate")] public GameObject modelInHierarchy;
    [FoldoutGroup("Precision")] public Ease aimEase;
    [FoldoutGroup("Recoil")][SerializeField] float recoilX = .2f;
    [FoldoutGroup("Recoil")][SerializeField][Range(0, 1)] float recoilRandomness = .2f;
    [FoldoutGroup("Recoil")][SerializeField][Range(0, 1)] float baseRecoilTime = .1f;
    [FoldoutGroup("Recoil")][SerializeField][Range(0, 1)] float baseRecoilRecoveryTime = .4f;

    [FoldoutGroup("Special behaviors")] public bool isAutomatic = false;
    #endregion
    #region private
    ObjectPool<GameObject> decalPool;
    ObjectPool<Line> linePool;
    Timer recoilTimer, recoilRecoveryTimer;
    Vector2 totalRecoilAmount, currentRecoilAmount, recoilRecoveryAmount;
    #endregion
    #region setters
    private int _ammo;
    protected Timer reloadTimer, fireRateTimer;
    public int ammo
    {
        get => _ammo;
        set
        {
            _ammo = Math.Clamp(value, 0, ammoMax);
            Bus.PushData("AMMO", _ammo);
        }
    }
    public int ammoMax { get => (int)StatManager.Get(StatType.MAGAZINE_SIZE); }
    public float precision { get => StatManager.Get(StatType.SPREAD); }
    public int precisionAim { get => (int)StatManager.Get(StatType.PRECISION_AIM); }
    public int damages { get => (int)StatManager.Get(StatType.DAMAGES); }
    public int bulletsFired { get => (int)StatManager.Get(StatType.BULLET_AMOUNT); }
    public float reloadTime { get => StatManager.Get(StatType.RELOAD_TIME); }
    public float fireRate { get => StatManager.Get(StatType.FIRE_RATE); }
    public float recoil { get => (int)StatManager.Get(StatType.RECOIL); }
    #endregion
    void Awake()
    {
        reloadTimer = new Timer(1f, ReloadCompleted);
        fireRateTimer = new Timer(1f);
        decalPool = new(
            () => Instantiate(decal),
            (GameObject newDecal) => newDecal.SetActive(true),
            (GameObject oldDecal) => oldDecal.SetActive(false),
            (GameObject oldDecal) => Destroy(oldDecal),
            true, 20, 150
        );
        linePool = new(
            () => Instantiate(linePrefab).GetComponent<Line>(),
            (Line line) =>
            {
                line.Thickness = 8;
                Timer timer = new(.2f);
                timer.OnTimerEnd += () => { linePool.Release(line); TimerManager.RemoveTimer(timer); };
                timer.OnTimerUpdate += () => line.Thickness = Mathf.Lerp(8f, 0f, timer.GetPercentage());
                timer.Play();
            },
            (Line line) => line.Thickness = 0,
            (Line line) => Destroy(line.gameObject)
        );

        recoilTimer = new(baseRecoilTime, () =>
        {
            Player.player.tpsController.pitchAdjustment -= currentRecoilAmount.y;
            Player.player.tpsController.yawAdjustment -= currentRecoilAmount.x;
            currentRecoilAmount = Vector2.zero;
            recoilRecoveryTimer.ResetPlay();
        });
        recoilTimer.OnTimerUpdate += () =>
        {
            float recoilXApplied = Time.deltaTime * (totalRecoilAmount.x / baseRecoilTime);
            float recoilYApplied = Time.deltaTime * (totalRecoilAmount.y / baseRecoilTime);
            currentRecoilAmount -= new Vector2(recoilXApplied, recoilYApplied);
            Player.player.tpsController.pitchAdjustment = recoilYApplied;
            Player.player.tpsController.yawAdjustment = recoilXApplied;
        };

        recoilRecoveryTimer = new(baseRecoilRecoveryTime);
        recoilRecoveryTimer.OnTimerUpdate += () =>
        {
            float recoilXApplied = Time.deltaTime * (recoilRecoveryAmount.x / baseRecoilRecoveryTime);
            float recoilYApplied = Time.deltaTime * (recoilRecoveryAmount.y / baseRecoilRecoveryTime);
            // recoilRecoveryAmount -= new Vector2(recoilXApplied, recoilYApplied);
            Player.player.tpsController.yawAdjustment = -recoilXApplied;
            Player.player.tpsController.pitchAdjustment = -recoilYApplied;
        };
    }
    void Start()
    {
        StatManager.instance.AddStatkit(statKit);
        ammo = ammoMax;
    }
    void Update()
    {
        Bus.PushData("actualPrecision", RealPrecision());
    }

    public virtual bool CanReload()
    {
        return !reloadTimer.IsStarted() && ammo != ammoMax;
    }
    public virtual bool CanFire()
    {
        return !fireRateTimer.IsStarted() && !reloadTimer.IsStarted() && ammo != 0;
    }
    public virtual void TriggerEnter()
    {
        if (CanFire())
            Trigger();
    }
    public virtual void TriggerPress()
    {
        if (isAutomatic && CanFire())
            Trigger();
    }
    public virtual void TriggerRelease() { }
    protected virtual void Trigger()
    {
        // Feedbacks
        fireRateTimer.endTime = 1 / fireRate;
        float animSpeed = fireAnimation.length / fireRateTimer.endTime;
        Player.player.animator.SetFloat("FireSpeed", animSpeed * 1.05f);
        fireRateTimer.ResetPlay();
        Player.player.animator.SetTrigger("Fire");
        muzzleFlash.Play();

        for (int i = 0; i < bulletsFired; i++)
            Fire(GetFireDestination());

        AddRecoil();

        if (--ammo == 0)
            Reload();
    }
    void Fire(Vector3 destination)
    {
        Vector3 endPos = destination;
        Vector3 direction = destination - fireStart.position;
        if (Physics.Raycast(fireStart.position, direction, out RaycastHit hit, Mathf.Infinity, fireLayerMask))
        {
            // Hitting a zombie
            if (hit.collider.gameObject.CompareTag("Zombie"))
            {
                hit.collider.GetComponentInParent<Zombie>().Hit(damages, hit);
            }
            else
            {
                GameObject newDecal = decalPool.Get();
                newDecal.transform.SetPositionAndRotation(hit.point + hit.normal * 0.1f, Quaternion.LookRotation(-hit.normal));
                new Timer(decalTimeout, () => decalPool.Release(newDecal)).Play();
            }
            endPos = hit.point;
        }
        Line line = linePool.Get();
        line.Start = fireStart.position;
        line.End = endPos;
    }
    protected virtual Vector3 GetFireDestination()
    {
        float realPrecision = RealPrecision();
        Vector3 impreciseDirection = ApplySpreadToDirection(Camera.main.transform.forward, realPrecision);
        if (Physics.Raycast(Camera.main.transform.position, impreciseDirection, out var hit, Mathf.Infinity, fireLayerMask))
        {
            return hit.point;
        }
        return Camera.main.transform.position + impreciseDirection * 1000;
    }
    public Vector3 ApplySpreadToDirection(Vector3 initialDirection, float spread)
    {
        float spreadRange = spread / 2f;
        Vector2 randomRotation = UnityEngine.Random.insideUnitCircle * spreadRange;
        Vector3 newDirection = Quaternion.AngleAxis(randomRotation.x, Vector3.up) * initialDirection;
        return Quaternion.AngleAxis(randomRotation.y, Vector3.Cross(newDirection, Vector3.up)) * newDirection;
    }
    void AddRecoil()
    {
        float recoilXAngle = recoil * recoilX;
        Vector2 recoilAmount = new Vector2(
            UnityEngine.Random.Range(-recoilXAngle, recoilXAngle),
            UnityEngine.Random.Range(recoil, recoil - recoil * recoilRandomness)
        );
        recoilRecoveryAmount = recoilAmount;
        currentRecoilAmount += recoilAmount;
        totalRecoilAmount = currentRecoilAmount;
        recoilTimer.ResetPlay();
        recoilRecoveryTimer.Pause();
    }
    public virtual void Reload()
    {
        if (reloadTimer.IsStarted())
            return;

        Player.player.CancelSprinting();
        reloadTimer.endTime = reloadTime;
        reloadTimer.ResetPlay();

        float animSpeed = reloadAnimation.length / reloadTime;
        Player.player.animator.SetFloat("ReloadSpeed", animSpeed);
        Player.player.animator.SetTrigger("Reload");

        reloads++;
        Bus.PushData("RELOAD");
        // TODO : Play sounds and animations
        // TODO : Return a value so that player knows what is hapenning
    }
    protected virtual void ReloadCompleted()
    {
        ammo = ammoMax;
        Bus.PushData("ammo", ammo);
        // TODO : Play sounds and stuff
        // TODO : Tell Player reload is done
    }
    public void CancelReload()
    {
        if (!reloadTimer.IsPlayingForward())
            return;
        reloadTimer.Reset();
        Player.player.animator.SetTrigger("cancelReload");
    }
    float RealPrecision()
        => Mathf.Lerp(precision, precision * Mathf.InverseLerp(100, 0, precisionAim), DOVirtual.EasedValue(0, 1, Player.player.aimValue, aimEase));
}
