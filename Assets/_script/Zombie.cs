using System;
using System.Collections.Generic;
using Asmos.Bus;
using Unity.AI.Navigation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour, ITakeExplosionDamages
{
    readonly static Dictionary<int, OffMeshAnimData> offMeshAnims = new()
    {
        {3, new("crawl under",.5f)},
        {2, new("climb",.5f)}
    };
    readonly static Dictionary<ZombieMovesKind, float> moveKindSpeed = new()
    {
        {ZombieMovesKind.WALK, .5f},
        {ZombieMovesKind.MEDIUM, 1.2f},
        {ZombieMovesKind.RUN, 2f},
        {ZombieMovesKind.SPRINT, 4f},
    };
    [SerializeField] Collider headCollider;
    [SerializeField] float attackDistance;
    [SerializeField] float attackCooldown;
    [SerializeField] AnimationClip attackAnimation;
    [SerializeField] AnimationClip landingAnimation;
    [SerializeField] AnimationClip endClimbAnimation;
    [SerializeField] AnimationClip spawnAnimation;
    [SerializeField] ZombieParameters baseParameters;
    [SerializeField] bool spawnOnAwake;
    Timer attackCooldownTimer, spawnTimer;
    GameObject pickup;
    int health, offMeshAnimIndex;
    bool isOnOffMesh = false;
    bool isDropFinished, isClimbFinished, climbWallHit;
    float customOffMeshAnimProgress = 0f;
    OffMeshLinkData linkData;
    OffMeshAnimData offMeshAnimData;
    Vector3 startOffMesh, endOffMesh;
    NavMeshAgent navMeshAgent;
    [HideInInspector] public Animator animator;
    Rigidbody[] rigidbodies;
    void Awake()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        spawnTimer = new(spawnAnimation.length, () =>
        {
            if (health <= 0)
                return;
            navMeshAgent.speed = moveKindSpeed[baseParameters.moveKind];
            navMeshAgent.enabled = true;

        });
        attackCooldownTimer = new(attackCooldown);
        float animSpeed = attackAnimation.length / attackCooldown;
        animator.SetFloat("attack speed", animSpeed);
        if (spawnOnAwake)
            Spawn(transform.position, baseParameters);
    }

    void Update()
    {
        if (!navMeshAgent.enabled)
            return;
        navMeshAgent.destination = Player.player.transform.position;
        animator.SetFloat("speed", navMeshAgent.velocity.magnitude);
        // animator.SetFloat("anim speed", navMeshAgent.velocity.magnitude);
        float distanceWithPlayer = Vector3.Distance(Player.player.transform.position, transform.position);

        // Attack
        if (distanceWithPlayer < attackDistance && !attackCooldownTimer.IsStarted() && !spawnTimer.IsStarted())
        {
            attackCooldownTimer.ResetPlay();
            animator.SetTrigger("attack");
        }
        HandleOffMeshLinks();
    }

    private void HandleOffMeshLinks()
    {
        // Upon entering an offMeshLink
        if (navMeshAgent.isOnOffMeshLink && !isOnOffMesh)
        {
            isOnOffMesh = true;
            isDropFinished = false;
            isClimbFinished = false;
            climbWallHit = false;
            linkData = navMeshAgent.currentOffMeshLinkData;
            if (Vector3.Distance(transform.position, linkData.startPos) < Vector3.Distance(transform.position, linkData.endPos))
            {
                startOffMesh = linkData.startPos;
                endOffMesh = linkData.endPos;
            }
            else
            {
                startOffMesh = linkData.endPos;
                endOffMesh = linkData.startPos;
            }
            Vector3 lookDirection = endOffMesh;
            lookDirection.y = 0;
            transform.LookAt(lookDirection, Vector3.up);
            customOffMeshAnimProgress = 0f;

            if (linkData.linkType == OffMeshLinkType.LinkTypeDropDown)
                animator.SetTrigger("drop");
            else
            {
                offMeshAnimIndex = ((NavMeshLink)linkData.owner).area;
                offMeshAnimData = offMeshAnims[offMeshAnimIndex];
                animator.SetBool(offMeshAnimData.name, true);
            }
        }

        // Upon exiting an offMeshLink
        if (!navMeshAgent.isOnOffMeshLink && isOnOffMesh)
        {
            isOnOffMesh = false;
        }

        if (isOnOffMesh)
        {
            Vector3 lookDirection = endOffMesh;
            lookDirection.y = transform.position.y;
            transform.LookAt(lookDirection, Vector3.up);
        }

        // Climbing
        if (isOnOffMesh && linkData.linkType == OffMeshLinkType.LinkTypeManual && offMeshAnimIndex == 2)
        {
            // TODO : Rotate towards point
            float elevation = Time.deltaTime * 2;
            float yPos = Mathf.Min(elevation + transform.position.y, endOffMesh.y);
            Vector3 newPos = transform.position;
            if (yPos >= endOffMesh.y - navMeshAgent.height && !isClimbFinished)
            {
                isClimbFinished = true;
                animator.SetBool(offMeshAnimData.name, false);
            }
            if (!climbWallHit)
            {
                customOffMeshAnimProgress += Time.deltaTime;
                newPos = Vector3.Lerp(startOffMesh, endOffMesh, customOffMeshAnimProgress * navMeshAgent.speed / 2);
                if (customOffMeshAnimProgress * navMeshAgent.speed >= .6f)
                {
                    climbWallHit = true;
                    customOffMeshAnimProgress = 0;
                }
            }
            if (isClimbFinished)
            {
                float startMovingAgainTimer = endClimbAnimation.length * 0.6f;
                customOffMeshAnimProgress += Time.deltaTime;
                float animValue = (customOffMeshAnimProgress - startMovingAgainTimer) / 2 + .3f;
                if (startMovingAgainTimer <= customOffMeshAnimProgress)
                    newPos = Vector3.Lerp(startOffMesh, endOffMesh, animValue);
            }

            newPos.y = yPos;
            transform.position = newPos;
            float distanceToEnd = Vector3.Distance(transform.position, endOffMesh);
            if (distanceToEnd <= Mathf.Epsilon)
                navMeshAgent.CompleteOffMeshLink();
        }

        // Crawling
        if (isOnOffMesh && linkData.linkType == OffMeshLinkType.LinkTypeManual && offMeshAnimIndex == 3)
        {
            // TODO : Rotate towards point
            customOffMeshAnimProgress += Time.deltaTime;
            float distance = Vector3.Distance(endOffMesh, startOffMesh);
            transform.position = Vector3.Lerp(startOffMesh, endOffMesh, customOffMeshAnimProgress * navMeshAgent.speed / distance);
            if (customOffMeshAnimProgress * navMeshAgent.speed / distance > 1f)
            {
                navMeshAgent.CompleteOffMeshLink();
                animator.SetBool(offMeshAnimData.name, false);
            }
        }

        // Dropping
        if (isOnOffMesh && linkData.linkType == OffMeshLinkType.LinkTypeDropDown)
        {
            // TODO : Rotate towards point
            customOffMeshAnimProgress += Time.deltaTime;
            Vector3 newPos = Vector3.Lerp(startOffMesh, endOffMesh, customOffMeshAnimProgress);
            if (transform.position.y - endOffMesh.y <= 0 && !isDropFinished)
            {
                isDropFinished = true;
                animator.SetTrigger("landing");
            }
            float newPosY = (customOffMeshAnimProgress < .3 ? 1 * Time.deltaTime : -10 * Time.deltaTime) + transform.position.y;
            transform.position = new Vector3(newPos.x, isDropFinished ? endOffMesh.y : newPosY, newPos.z);
            float distanceToEnd = Vector3.Distance(transform.position, endOffMesh);
            if (distanceToEnd <= Mathf.Epsilon && landingAnimation.length < customOffMeshAnimProgress)
                navMeshAgent.CompleteOffMeshLink();
        }
    }

    // /!\ This is called from an animation
    public void Attack()
    {
        float distanceWithPlayer = Vector3.Distance(Player.player.transform.position, transform.position);
        if (distanceWithPlayer < attackDistance && attackCooldownTimer.IsStarted())
        {
            if (Player.player.IsDead())
            {
                animator.SetTrigger("bite");
                navMeshAgent.speed = 0;
                return;
            }

            Player.player.Hit(1);
            // SUGGESTION : Play a zombie sound
        }

    }

    public void Hit(int damages, RaycastHit hit)
    {
        if (hit.collider == headCollider)
        {
            damages *= 2;
            animator.SetTrigger("hit head");
        }
        else
        {
            animator.SetTrigger("hit body");
        }

        TakeDamages(damages, hit.point, hit.normal);
        // Start a decal for damages
    }

    void TakeDamages(int damages, Vector3 position, Vector3 fromDirection)
    {
        Bus.PushData("zombie hit data", position, fromDirection, damages);
        if (health <= 0)
            return;
        health -= damages;
        if (health <= 0)
            Die();
    }

    void Die()
    {
        SetRagdoll(true);
        ZombieSpawnerManager.instance.ZombieDied(this);
        navMeshAgent.speed = 0;
        navMeshAgent.enabled = false;
        if (pickup)
            Instantiate(pickup, transform.position, Quaternion.identity);
    }

    public void Spawn(Vector3 spawnPoint, ZombieParameters parameters, GameObject pickup = null)
    {
        // Initialize();
        SetRagdoll(false);
        baseParameters = parameters;
        transform.position = spawnPoint;
        health = parameters.health;
        navMeshAgent.speed = 0f;
        navMeshAgent.enabled = false;
        this.pickup = pickup;

        animator.SetTrigger("spawn");
        animator.SetInteger("move kind", (int)parameters.moveKind);
        spawnTimer.ResetPlay();

        // TODO : sound
        // TODO : animation
        // TODO : particles
    }

    void SetRagdoll(bool enabled)
    {
        foreach (Rigidbody rigid in rigidbodies)
        {
            rigid.isKinematic = !enabled;
            rigid.useGravity = enabled;
        }
        animator.enabled = !enabled;

    }
    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up * 1.8f, attackDistance);
    }

    public void TakeExplosionDamages(float distancePercent)
    {
        TakeDamages((int)(Player.player.grenadeDamages * distancePercent), headCollider.transform.position, Vector3.down);
    }

    struct OffMeshAnimData
    {
        public string name;
        public float speedModifier;

        public OffMeshAnimData(string name, float speedModifier)
        {
            this.name = name;
            this.speedModifier = speedModifier;
        }
    }
}
[Serializable]
public struct ZombieParameters
{
    public int health;
    public ZombieMovesKind moveKind;
}

public enum ZombieMovesKind
{
    WALK = 0, MEDIUM = 1, RUN = 2, SPRINT = 3
}