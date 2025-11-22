using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Responsible for controlling how the ship behaves in game. How it attacks the players and completes its objectives! An AI Essentially.
/// Happens server-side.
/// </summary>
public abstract class EnemyAI : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected GameObject clientProjectilePrefab;
    [SerializeField] protected GameObject serverProjectilePrefab;
    [SerializeField] protected Transform firingSpawnPoint;
    [SerializeField] protected Health health;
    [SerializeField] protected GameObject core;
    [SerializeField] protected ParticleSystem[] boostEffects;

    [Header("Settings")]
    [SerializeField] protected float sightDistance;
    [SerializeField] protected LayerMask targetLayers;

    [SerializeField] protected float maxRotationSpeed;
    [SerializeField] protected float verticalMovementSpeed;
    [SerializeField] protected float horizontalMovementSpeed;
    [SerializeField] protected float boostFactor;
    [SerializeField] protected float switchTargetCooldown;
    [SerializeField] protected float firingDistance = 12;

    [Header("Implementation-Wise")]
    public NetworkVariable<EnemyState> enemyState = new NetworkVariable<EnemyState>();
    public GameObject target;   // target to fire
    public GameObject helpingTheTarget = null; // more dynamic movement from enemy: since someone could jumping the enemy
    protected Coroutine currentBehaviour; // Coroutine that is currently executing the behaviour


    // ========================== Ship Functionality ========================
    // e.g. movement, aiming, boosting for enemy ships.
    private float currentBoost; // current amount of boost

    /// <summary>
    /// Generates raycasts that represent the ships line of sight, and returns any objects that come in this sight.
    /// </summary>
    /// <returns> Returns a list of gameObjects it saw in its line of sight, OR null if none were found. </returns>
    protected abstract List<GameObject> GenerateRaycasts();

    /// <summary>
    /// A method used to move the enemy ship towards the direction passed in.
    /// </summary>
    /// <param name="normalizedDirection"> The direction you want the ship to head towards. Must be normalized. </param>
    protected void Move(Vector2 normalizedDirection)
    {
        rb.velocity = (transform.up * (normalizedDirection.y + currentBoost) * verticalMovementSpeed) +
                        (transform.right * normalizedDirection.x * horizontalMovementSpeed);
    }

    /// <summary>
    /// A method used to aim at the target. For "waitPerCall", just pass in the time you specified in WaitForSeconds().
    /// You can offset this aim if needed. Passing a value of 30 means aiming 30 degrees to the left of the target's position (so the ship won't obviously point towards the target)
    /// Note that it rotates "maxRotationDegrees" per second.
    /// </summary>
    /// <param name="target"> The target the enemy ship will aim at </param>
    /// <param name="waitPerCall"> The time between each call of Aim() A.K.A the time you specified in WaitForSeconds(). This is needed as an "alternative" to Time.deltaTime </param>
    /// <param name="offsetRotation"> How much you want to offset the rotation by. A value of 30 means 30 degrees to the left of targetPos, and a value of -30 means 30 degrees to the right of targetPos </param>
    protected void Aim(GameObject target, float waitPerCall, float offsetRotation = 0)
    {
        if (target == null) { return; }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, calculateTargetRotation(target.transform.position, offsetRotation),
                                maxRotationSpeed * waitPerCall);
    }

    protected void Aim(Vector2 directionToLookAt, float waitPerCall, float offsetRotation = 0)
    {
        if (directionToLookAt == null) { return; }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, calculateTargetRotation(directionToLookAt, offsetRotation),
                                maxRotationSpeed * waitPerCall);
    }

    /// <summary>
    /// Calculates and returns the quaternion needed to face the target's position. You can offset this
    /// rotation if needed. Passing a value of 30 means 30 degrees to the left of the targetPos's rotation (so the ship won't obviously point towards targetPos)
    /// </summary>
    /// <param name="targetPos">The position that the returned Quaternion will face </param>
    /// <param name="offsetRotation"> How much you want to offset the rotation by. A value of 30 means 30 degrees to the left of targetPos, and a value of -30 means 30 degrees to the right of targetPos </param>
    /// <returns>The quaternion that faces the target's position </returns>
    protected Quaternion calculateTargetRotation(Vector3 targetPos, float offsetRotation = 0)
    {
        Vector2 directionToLookAt = targetPos - transform.position;
        float zDegrees = Mathf.Atan2(directionToLookAt.y, directionToLookAt.x) * (180 / Mathf.PI);

        Quaternion targetQuaternionRotation = Quaternion.Euler(0, 0, zDegrees - 90 + offsetRotation);
        return targetQuaternionRotation;
    }

    protected Quaternion calculateTargetRotation(Vector2 directionToLookAt, float offsetRotation = 0)
    {
        float zDegrees = Mathf.Atan2(directionToLookAt.y, directionToLookAt.x) * (180 / Mathf.PI);

        Quaternion targetQuaternionRotation = Quaternion.Euler(0, 0, zDegrees - 90 + offsetRotation);
        return targetQuaternionRotation;
    }

    /// <summary>
    /// Fires a projectile from the enemy ship's firing spawn point. Instantiates the server projectile, and instantiates dummy projectiles to all
    /// the clients so they can also see the projectile.
    /// </summary>
    [ServerRpc]
    protected void FireProjectileServerRpc()
    {
        GameObject serverInstance = Instantiate(serverProjectilePrefab, firingSpawnPoint.position, transform.rotation);
        if (serverInstance.TryGetComponent(out ProjectileHits projectileHits))
        {
            projectileHits.SetSourceShooter(gameObject);
        }

        FireProjectileClientRpc();
    }

    /// <summary>
    /// The Client RPC call responsbile for instantiating the enemy ship's projectile on all client screens.
    /// </summary>
    [ClientRpc]
    protected void FireProjectileClientRpc()
    {
        Instantiate(clientProjectilePrefab, firingSpawnPoint.position, transform.rotation);
    }

    /// <summary>
    /// A custom method, enabling enemy ships to do burst fire.
    /// </summary>
    /// <param name="burstAmount"> How many projectiles are fired in one burst </param>
    /// <param name="timeBetweenShots"> The time between each projectile shot in the burst. For example, if you pass in 1, it means 1 second between each
    /// shot IN the burst, NOT the time between different bursts. </param>
    /// <returns></returns>
    protected IEnumerator BurstFire(int burstAmount, float timeBetweenShots)
    {
        for (int i = 0; i < burstAmount; i++)
        {
            FireProjectileServerRpc();

            yield return new WaitForSeconds(timeBetweenShots);
        }
    }

    /// <summary>
    /// A method used to apply "boost" to the enemy ship. Adds boosting functionality to enemy ships, and responsible for showing the appropriate 
    /// effects for each client!
    /// </summary>
    /// <param name="turnOn"> true means turning the boost on, and false means turning it off </param>
    protected void Boost(bool turnOn)
    {
        if (turnOn)
        {
            currentBoost = boostFactor;
            DisplayBoostEffectsClientRpc(true);
        }
        else
        {
            currentBoost = 0;
            DisplayBoostEffectsClientRpc(false);
        }
    }

    /// <summary>
    /// A ClientRPC call to display the boosting particle effects for all the clients. 
    /// </summary>
    /// <param name="turnOn"> true means turning the boosting effects on, and false means turning them off </param>
    [ClientRpc]
    protected void DisplayBoostEffectsClientRpc(bool turnOn)
    {
        if (turnOn)
        {
            foreach (ParticleSystem system in boostEffects)
            {
                if (system.gameObject.activeInHierarchy == false)
                {
                    system.gameObject.SetActive(true);
                }

                system.Play();
            }
        }
        else
        {
            foreach (ParticleSystem system in boostEffects)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    /// <summary>
    /// Used for switching between enemy states. Pass in the new and previous values, and this method will do the rest for updating the
    /// ships behaviour accordingly.
    /// </summary>
    /// <param name="previousValue"> The previous state that the enemy ship was on </param>
    /// <param name="newValue"> The new state the enemy ship has transitioned to </param>
    protected void UpdateBehaviour(EnemyState previousValue, EnemyState newValue)
    {
        // Unsubscribe from current routine behaviour, and start new one.
        if (currentBehaviour != null)
        {
            StopCoroutine(currentBehaviour);
        }

        switch (newValue)
        {
            case EnemyState.Attacking:
                currentBehaviour = StartCoroutine(Attacking());
                break;
            case EnemyState.Retreating:
                currentBehaviour = StartCoroutine(Retreating());
                break;
            case EnemyState.Charging:
                currentBehaviour = StartCoroutine(Charging());
                break;
            case EnemyState.Scouting:
                currentBehaviour = StartCoroutine(Scouting());
                break;
            default:
                currentBehaviour = StartCoroutine(Attacking());
                break;
        }
    }

    /// <summary>
    /// A method used by external classes (particularly projectiles) to pass on information to the enemy AI about who hit them.
    /// </summary>
    /// <param name="attacker"> The ship/object that shot the projectile that managed to hit the enemy ship. The "source" of the damage if you will. </param>
    public abstract void GotHit(GameObject attacker);

    /// <summary>
    /// The coroutine used for defining scouting behaviour. 
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected abstract IEnumerator Scouting();

    /// <summary>
    /// The coroutine used for defining retreating behaviour. 
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected abstract IEnumerator Retreating();

    /// <summary>
    /// The coroutine used for defining attacking behaviour. 
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected abstract IEnumerator Attacking();

    /// <summary>
    /// The coroutine used for defining charging behaviour. 
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected abstract IEnumerator Charging();
}

/// <summary>
/// The different states that an enemy can have whilst in-game.
/// So far, there's scouting, attacking, retreating, and chasing.
/// </summary>
public enum EnemyState
{
    Scouting,
    Attacking,
    Retreating,
    Charging
}
