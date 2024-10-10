using Asmos.Bus;
using UnityEngine;
using UnityEngine.Pool;
using Shapes;
using Sirenix.OdinInspector;
using System;
public class Weapon : MonoBehaviour
{
    const int PRECISION_MIN = 30;
    const int PRECISION_MAX = 0;
    #region exposedParameters
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
    [FoldoutGroup("Base values", Expanded = true)][SerializeField] protected int baseAmmoMax;
    [FoldoutGroup("Base values")][SerializeField] protected int baseDamages;
    [FoldoutGroup("Base values")][SerializeField][Range(0, 5)] protected float baseReloadTime;
    [FoldoutGroup("Base values")][SerializeField][Range(.2f, 30)] protected float baseFireRate;
    [FoldoutGroup("Base values")][SerializeField][Range(PRECISION_MAX, PRECISION_MIN)] protected int basePrecision;
    [FoldoutGroup("Base values")][SerializeField][Range(0, 100)] protected int basePrecisionAim;
    [FoldoutGroup("Base values")][SerializeField] protected int baseBulletsFired;
    [FoldoutGroup("Base values")] public bool isAutomatic = false;
    // [FoldoutGroup("Base values")] public bool isAutomatic = false;
    #endregion
    #region private
    ObjectPool<GameObject> decalPool;
    ObjectPool<Line> linePool;
    #endregion
    #region setters
    private int _ammo, _ammoMax, _precision, _precisionAim;
    protected Timer reloadTimer, fireRateTimer;
    public int ammo
    {
        get => _ammo;
        set
        {
            _ammo = Math.Clamp(value, 0, ammoMax);
            Bus.PushData("ammo", _ammo);
        }
    }
    public int ammoMax
    {
        get => _ammoMax;
        set
        {
            _ammoMax = value;
            ammo = Math.Min(ammoMax, ammo);
            Bus.PushData("ammoMax", _ammoMax);
        }
    }
    public int precision
    {
        get => _precision;
        set
        {
            _precision = Math.Clamp(value, PRECISION_MAX, PRECISION_MIN);
            Bus.PushData("precision", _precision);
        }
    }
    public int precisionAim
    {
        get => _precisionAim;
        set
        {
            _precisionAim = Math.Clamp(value, 0, 100);
            Bus.PushData("precisionAim", _precisionAim);
        }
    }
    public int damages { get; set; }
    public int bulletsFired { get; set; }
    public float reloadTime
    {
        get => reloadTimer.endTime;
        set
        {
            reloadTimer.endTime = value;
            float animSpeed = reloadAnimation.length / reloadTime;
            Player.player?.animator?.SetFloat("ReloadSpeed", animSpeed);
        }
    }
    public float fireRate
    {
        get => 1 / fireRateTimer.endTime;
        set
        {
            fireRateTimer.endTime = 1 / value;
            float animSpeed = fireAnimation.length / (1 / value);
            Player.player?.animator?.SetFloat("FireSpeed", animSpeed * 1.05f);
        }
    }
    #endregion
    void Awake()
    {
        reloadTimer = new Timer(baseReloadTime, ReloadCompleted);
        fireRateTimer = new Timer(baseFireRate);

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

    }

    void Start()
    {
        ammoMax = baseAmmoMax;
        ammo = baseAmmoMax;
        damages = baseDamages;
        reloadTime = baseReloadTime;
        precision = basePrecision;
        bulletsFired = baseBulletsFired;
        fireRate = baseFireRate;
        precisionAim = basePrecisionAim;
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
        fireRateTimer.ResetPlay();
        Player.player.animator.SetTrigger("Fire");
        muzzleFlash.Play();

        for (int i = 0; i < bulletsFired; i++)
            Fire(GetFireDestination());

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
    public virtual void Reload()
    {
        if (reloadTimer.IsStarted())
            return;
        Player.player.CancelSprinting();
        reloadTimer.ResetPlay();
        Player.player.animator.SetTrigger("Reload");
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
    int RealPrecision()
    {
        return (int)Mathf.Lerp(precision, precision * Mathf.InverseLerp(100, 0, precisionAim), Player.player.aimValue);
    }
}
