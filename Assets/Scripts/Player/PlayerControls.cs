using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class PlayerControls : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectileFiringEffect;
    [SerializeField] private ParticleSystem[] boostParticleSystems;
    //[SerializeField] private TrailRenderer[] boostTrails;

    [Header("==== Settings ====")]

    [Header("Movement")]
    [SerializeField] private float baseHorizontalSpeed = 5f;
    [SerializeField] private float baseVerticalSpeed = 5f;
    [SerializeField] private float distanceToleranceForRotation = 0.3f;

    [Header("Boosting")]
    [SerializeField] private float boostFactor = 2f;

    [Header("Rotation")]
    [SerializeField] private float maxRotationSpeed = 2f;

    [Header("Firing")]
    [SerializeField] private float fireCooldown = 1f;   // How long to wait between each shot fired


    private Vector2 mousePos;
    private Vector2 movementInput;
    private float horizontalMovementSpeed;
    private float verticalMovementSpeed;
    private float currentBoost;
    private bool isFiring = false;
    private float currentCooldownLeft;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }

        horizontalMovementSpeed = baseHorizontalSpeed;
        verticalMovementSpeed = baseVerticalSpeed;

        inputReader.MoveEvent += Move;
        inputReader.AimEvent += Aim;
        inputReader.BoostEvent += Boost;
        inputReader.FireEvent += FireState;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }

        inputReader.MoveEvent -= Move;
        inputReader.AimEvent -= Aim;
        inputReader.BoostEvent -= Boost;
        inputReader.FireEvent -= FireState;
    }

    private void Update()
    {
        if (!IsOwner) { return; }

        if (currentCooldownLeft > 0)
        {
            currentCooldownLeft -= Time.deltaTime;
        }

        FiringProjectile();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) { return; }

        if (!MouseClose())
        {
            rb.velocity = (transform.up * (movementInput.y + currentBoost) * verticalMovementSpeed) +
                        (transform.right * movementInput.x * horizontalMovementSpeed);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) { return; }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, calculateTargetRotation(mousePos), maxRotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Sets the moving direction based on the input provided, so that this script can utilise it for movement appropriately
    /// </summary>
    /// <param name="direction"> The normalised direction given from the input action map</param>
    private void Move(Vector2 direction)
    {
        movementInput = direction;
    }

    /// <summary>
    /// Responsible for retrieving the mouse's position and updating the "mousePos" variable here.
    /// </summary>
    /// <param name="mousePos">The mouse position</param>
    private void Aim(Vector2 mousePos)
    {
        this.mousePos = (Vector2)Camera.main.ScreenToWorldPoint(mousePos);
    }

    /// <summary>
    /// Modifies the currentBoost for the player, and based on the boolean based in, will have code that increases/decreases the boost.
    /// Modifies currentBoost specifically. Visual effects included.
    /// </summary>
    /// <param name="turnOn"> true means to turn the boost on, and false means to turn the boost off</param>
    private void Boost(bool turnOn)
    {
        if (turnOn)
        {
            currentBoost = boostFactor;
            DisplayBoostEffectsServerRpc(true);
        }
        else
        {
            currentBoost = 0;
            DisplayBoostEffectsServerRpc(false);
        }
    }

    /// <summary>
    /// Calculates and returns the quaternion needed to face the target's position
    /// </summary>
    /// <param name="targetPos">The position that the returned Quaternion will face </param>
    /// <returns>The quaternion that faces the target's position </returns>
    private Quaternion calculateTargetRotation(Vector3 targetPos)
    {
        Vector2 directionToLookAt = targetPos - transform.position;
        float zDegrees = Mathf.Atan2(directionToLookAt.y, directionToLookAt.x) * (180 / Mathf.PI);

        Quaternion targetQuaternionRotation = Quaternion.Euler(0, 0, zDegrees - 90);
        return targetQuaternionRotation;
    }

    /// <summary>
    /// Returns whether the mouse's position, which you need to pass in, is close to the player's position, with tolerance distance determine through 
    /// "distanceToleranceForRotation"
    /// </summary>
    /// <returns> 
    /// 1. true if the distance between the mouse's position and the player's position is greater than or equal to the tolerance distance 
    /// 2. false if the distance between the mouse's position and the player's position is less than the tolerance distance 
    /// </returns>
    private bool MouseClose()
    {
        float dist = Vector3.Distance(mousePos, transform.position);

        if (dist > distanceToleranceForRotation)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Updates the isFiring state, letting us know if the player is currently deciding to fire or not.
    /// </summary>
    /// <param name="isFiring"> True if the player is firing, and false otherwise </param>
    private void FireState(bool isFiring)
    {
        this.isFiring = isFiring;
    }

    /// <summary>
    /// A ServerRPC call responsible for spawning the projectile server-side, and ensuring all clients also 
    /// have the projectile spawn on their screens too.
    /// </summary>
    [ServerRpc]
    private void SpawnProjectileServerRpc()
    {
        GameObject projectileInstance = Instantiate(serverProjectilePrefab, projectileSpawnPoint.position, transform.rotation);
        if (projectileInstance.TryGetComponent(out ProjectileHits projectileHits))
        {
            projectileHits.SetSourceShooter(gameObject);
        }

        SpawnProjectileClientRpc();
    }

    /// <summary>
    /// A ClientRPC call responsible for ensuring the projectile gets spawned on their screen, and that they see the firing 
    /// animation go off of the spaceship whose firing.
    /// </summary>
    [ClientRpc]
    private void SpawnProjectileClientRpc()
    {
        if (IsOwner) { return; }

        Instantiate(clientProjectilePrefab, projectileSpawnPoint.position, transform.rotation);
        projectileFiringEffect.SetActive(true);
    }

    /// <summary>
    /// Fires the projectile given the user is firing and that the cooldown is complete. It spawns the owner's projectile first, before having
    /// it run server-side to ensure gameplay is enjoyable.
    /// </summary>
    private void FiringProjectile()
    {
        if (isFiring)
        {
            if (currentCooldownLeft <= 0)
            {
                Instantiate(clientProjectilePrefab, projectileSpawnPoint.position, transform.rotation);
                projectileFiringEffect.SetActive(true);
                currentCooldownLeft = fireCooldown;

                SpawnProjectileServerRpc();
            }
        }
    }

    /// ==================== BOOSTING EFFECTS =================
    /// <summary>
    /// A ServerRPC call responsible for having all the clients see the boost effect visible on their screen when the player boosts.
    /// </summary>
    /// <param name="turnOn"> true means turn on the boost effects, and false means turn them off </param>
    [ServerRpc]
    private void DisplayBoostEffectsServerRpc(bool turnOn)
    {
        DisplayBoostEffectsClientRpc(turnOn);
    }

    /// <summary>
    /// The ClientRPC call to turn on/off the boost effects, on the client-side
    /// </summary>
    /// <param name="turnOn"> true means turn on the boost effects, and false means turn them off </param>
    [ClientRpc]
    private void DisplayBoostEffectsClientRpc(bool turnOn)
    {
        if (turnOn)
        {
            EnableBoostEffects();
        }
        else
        {
            DisableBoostEffects();
        }
    }

    /// <summary>
    /// Enables the boost particle system effects
    /// </summary>
    private void EnableBoostEffects()
    {
        foreach (ParticleSystem system in boostParticleSystems)
        {
            if (system.gameObject.activeInHierarchy == false)
            {
                system.gameObject.SetActive(true);
            }

            system.Play();
        }
    }

    /// <summary>
    /// Disables the boost particle system effects
    /// </summary>
    private void DisableBoostEffects()
    {
        foreach (ParticleSystem system in boostParticleSystems)
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
