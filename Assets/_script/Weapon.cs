using Asmos.Bus;
using UnityEngine;
using UnityEngine.Pool;
using Shapes;
public class Weapon : MonoBehaviour
{
    protected int ammo;
    [SerializeField] protected int ammoMax;
    [SerializeField] protected int damages;
    [SerializeField][Range(0, 5)] protected float reloadTime;
    [SerializeField][Range(0, 1)] protected float fireCooldownTime;
    [SerializeField] protected Transform fireStart;
    [SerializeField] GameObject decal;
    [SerializeField] float decalTimeout = 10f;
    [SerializeField] AnimationClip reloadAnimation;
    [SerializeField] AnimationClip fireAnimation;
    [SerializeField] ParticleSystem muzzleFlash;
    [SerializeField] Line fireLine;
    ObjectPool<GameObject> decalPool;
    protected Timer reloadTimer, fireCooldownTimer;
    public LayerMask fireLayerMask;
    public int animLayer;
    public string nameInHierarchy;
    public bool isAutomatic = false;
    new Collider collider;
    new Rigidbody rigidbody;
    Outline outline;
    Interactable interactable;

    public virtual bool CanReload()
    {
        return !reloadTimer.IsStarted() && ammo != ammoMax;
    }
    public virtual bool CanFire()
    {
        return !fireCooldownTimer.IsStarted() && !reloadTimer.IsStarted() && ammo != 0;
    }
    public virtual void TriggerEnter()
    {
        if (CanFire())
            Fire();
    }
    public virtual void TriggerPress()
    {
        if (isAutomatic && CanFire())
            Fire();
    }
    public virtual void TriggerRelease() { }
    protected virtual void Fire()
    {
        Vector3 fireDestination = GetFireDestination();
        if (Physics.Raycast(fireStart.position, fireDestination - fireStart.position, out RaycastHit hit, Mathf.Infinity, fireLayerMask))
        {
            // Hitting a zombie
            if (hit.collider.gameObject.CompareTag("Zombie"))
            {
                hit.collider.GetComponentInParent<Zombie>().Hit(damages, hit);
            }
            // Or placing a decal where we hit
            else
            {
                GameObject newDecal = decalPool.Get();
                newDecal.transform.SetPositionAndRotation(hit.point + hit.normal * 0.1f, Quaternion.LookRotation(-hit.normal));
                new Timer(decalTimeout, () => decalPool.Release(newDecal)).Play();
            }

            fireDestination = hit.point;
        }

        // Feedbacks
        fireCooldownTimer.ResetPlay();
        Player.player.animator.SetTrigger("Fire");
        muzzleFlash.Play();
        fireLine.Thickness = 8;
        fireLine.Start = fireStart.position;
        fireLine.End = fireDestination;
        new Timer(.05f, () => fireLine.Thickness = 0).Play();

        --ammo;
        Bus.PushData("ammo", ammo);
    }
    void Start()
    {
        ammo = ammoMax;
        reloadTimer = new Timer(reloadTime, ReloadCompleted);
        fireCooldownTimer = new Timer(fireCooldownTime);

        collider = GetComponent<Collider>();
        rigidbody = GetComponent<Rigidbody>();
        outline = GetComponent<Outline>();
        interactable = GetComponent<Interactable>();
        fireLine.gameObject.SetActive(true);
        fireLine.transform.parent = null;
        fireLine.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        decalPool = new(CreateDecal, GetDecal, ReleaseDecal, DestroyDecal, true, 20, 150);
    }

    protected virtual void ReloadCompleted()
    {
        ammo = ammoMax;
        Bus.PushData("ammo", ammo);
        // TODO : Play sounds and stuff
        // TODO : Tell Player reload is done
    }
    public virtual void Reload()
    {
        if (reloadTimer.IsStarted())
            return;
        reloadTimer.ResetPlay();
        // TODO : Play sounds and animations
        // TODO : Return a value so that player knows what is hapenning
    }

    public virtual void Pickup()
    {
        if (Player.player.weapon == this)
            return;
        Bus.PushData("ammo", ammo);
        Bus.PushData("ammoMax", ammoMax);
        rigidbody.isKinematic = true;
        collider.enabled = false;
        interactable.enabled = false;
        outline.enabled = false;
        muzzleFlash.gameObject.SetActive(true);
        Player.player.PickupWeapon(this);

        float animSpeed = reloadAnimation.length / reloadTime;
        Player.player.animator.SetFloat("ReloadSpeed", animSpeed);
        animSpeed = fireAnimation.length / fireCooldownTime;
        Player.player.animator.SetFloat("FireSpeed", animSpeed);
    }

    protected virtual Vector3 GetFireDestination()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out var hit, Mathf.Infinity, fireLayerMask))
        {
            return hit.point;
        }
        return Camera.main.transform.position + Camera.main.transform.forward * 1000;
    }

    #region decalRegion
    GameObject CreateDecal()
    {
        return Instantiate(decal);
    }
    void GetDecal(GameObject newDecal)
    {
        newDecal.SetActive(true);
    }
    void ReleaseDecal(GameObject oldDecal)
    {
        oldDecal.SetActive(false);
    }
    void DestroyDecal(GameObject oldDecal)
    {
        Destroy(oldDecal);
    }
    #endregion
}
