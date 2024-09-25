using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour
{
    [SerializeField] Collider headCollider;
    [SerializeField] float attackDistance;
    [SerializeField] float attackCooldown;
    [SerializeField] AnimationClip attackAnimation;

    Timer attackCooldownTimer;
    public int healthMax;
    int health;
    NavMeshAgent navMeshAgent;
    Animator animator;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = healthMax;
        attackCooldownTimer = new(attackCooldown);
        float animSpeed = attackAnimation.length / attackCooldown;
        animator.SetFloat("attack speed", animSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        navMeshAgent.destination = Player.player.playerTarget.position;
        animator.SetFloat("speed", navMeshAgent.velocity.magnitude / navMeshAgent.speed);
        float distanceWithPlayer = Player.player.DistanceWithPlayer(transform.position);
        if (distanceWithPlayer < attackDistance && !attackCooldownTimer.IsStarted())
        {
            attackCooldownTimer.ResetPlay();
            animator.SetTrigger("attack");
        }
    }

    // /!\ This is called from an animation
    public void Attack()
    {
        float distanceWithPlayer = Player.player.DistanceWithPlayer(transform.position);
        if (distanceWithPlayer < attackDistance && attackCooldownTimer.IsStarted())
        {
            Player.player.Hit();
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
        health -= damages;
        ZombieBloodPool.PlaceBlood(hit.point, Quaternion.LookRotation(hit.normal));
        if (health <= 0)
            Die();
        // Start a decal for damages
    }

    void Die()
    {
        animator.enabled = false;
        ZombieSpawnerManager.instance.ZombieDied(this);
    }

    void Spawn()
    {
        animator.enabled = true;
        // TODO : Move transform
        // TODO : sound
        // TODO : animation
        // TODO : particles
    }
    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up * 1.8f, attackDistance);
    }
}
