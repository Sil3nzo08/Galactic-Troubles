using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Controls projectile firing and cooldown behavior for a networked object. Gives the gameObject the ability to fire projectiles over the network essentially.
/// </summary>
/// <remarks>
/// Instantiates local projectiles for the owner, sends a server RPC to spawn the authoritative projectile on the server, and broadcasts the spawn to other clients.
/// </remarks>
public class FiringController : NetworkBehaviour
{
    [SerializeField] private string purpose;

    [Header("References")]
    [SerializeField] private GameObject serverProjectilePrefab; // Projectile prefab instantiated locally on the server for authoritative simulation.
    [SerializeField] private GameObject clientProjectilePrefab; // Projectile prefab instantiated on clients for visual representation.
    [SerializeField] private Transform projectileSpawnPoint; // Spawn position for projectiles.
    [SerializeField] private Transform projectileSpawnRotation; // Spawn rotation used when instantiating projectiles.

    [Header("Settings")]
    [SerializeField] private float fireCooldown = 1f; // Time in seconds between allowed single projectile fires. Note: Should be longer than the total burst duration (burstAmount * timeBetweenShots) to prevent burst overlapping. 
    [SerializeField] private BurstFireInfo burstFireInfo; // Configuration for burst firing behavior.
 
    // =============== FIRING FUNCTIONALITY BELOW ===============
    private bool isFiring = false;
    private float currentCooldownLeft;

    // ======== EXPOSED FUNCTIONALITY =========
    /// <summary>
    /// Updates whether firing input is currently active.
    /// </summary>
    /// <param name="isFiring">True when firing input is held; false otherwise.</param>
    public void UpdateFireState(bool isFiring)
    {
        this.isFiring = isFiring;
    }

    /// <summary>
    /// Attempts to fire a projectile if input is active and cooldown has elapsed.
    /// </summary>
    public void FireProjectileWithCooldown()
    {
        // Can't fire projectile yet
        if (!isFiring || currentCooldownLeft > 0) { return; }

        // Fire projectile and update cooldown
        FireProjectile();
        currentCooldownLeft = fireCooldown;
    }

    /// <summary>
    /// Attempts to fire a burst of projectiles if the cooldown has elapsed.
    /// <para><strong>Note:</strong> <see cref="fireCooldown"/> should be set longer than the total burst duration 
    /// (burstAmount × timeBetweenShots) to prevent consecutive bursts from overlapping.</para>
    /// </summary>
    public void TryBurstFire()
    {
        if (currentCooldownLeft > 0) { return; }    

        currentCooldownLeft = fireCooldown;
        StartCoroutine(BurstFire());
    }

    // ======== HIDDEN FUNCTIONALITY =========
    /// <summary>
    /// Fires multiple projectiles in rapid succession according to burst configuration.
    /// </summary>
    /// <remarks>
    /// The number of projectiles and delay between them are defined in <see cref="burstFireInfo"/>.
    /// </remarks>
    private IEnumerator BurstFire()
    {
        for (int i = 0; i < burstFireInfo.burstAmount; i++)
        {
            FireProjectile();
            yield return new WaitForSeconds(burstFireInfo.timeBetweenShots);
        }
    }

    /// <summary>
    /// Spawns a client-side projectile on remote clients.
    /// </summary>
    [ClientRpc] 
    private void SpawnProjectileClientRpc()
    {
        if (IsOwner) { return; } // Don't spawn for owner, as it already happened prior to the RPC.

        GameObject projectileInstance = Instantiate(clientProjectilePrefab, projectileSpawnPoint.position, projectileSpawnRotation.rotation);

        // Setup movement for the projectile
        if (projectileInstance.TryGetComponent(out MoveController mc))
        {
            mc.UpdateMoveDirection(Vector2.up);
            mc.Move();
        }
    }

    /// <summary>
    /// Spawns the authoritative projectile on the server and notifies clients to spawn a dummy projectile.
    /// </summary>
    [ServerRpc]
    private void SpawnProjectileServerRpc()
    {
        // Create the server projectile
        GameObject projectileInstance = Instantiate(serverProjectilePrefab, projectileSpawnPoint.position, projectileSpawnRotation.rotation);

        // Assign ownership of the projectile being shot out to this gameObject
        if (projectileInstance.TryGetComponent(out ServerProjectile sp))
        {
            sp.SetOwner(gameObject);
        }

        // Setup movement for projectile
        if (projectileInstance.TryGetComponent(out MoveController mc))
        {
            mc.UpdateMoveDirection(Vector2.up);
            mc.Move();
        }

        // Clients are commanded to spawn a dummy projectile
        SpawnProjectileClientRpc();
    }

    /// <summary>
    /// Fires a single projectile and synchronizes it across the network.
    /// </summary>
    private void FireProjectile()
    {
        // Create projectile (this will happen on the host/owner's side, then update for all other clients via the RPC call below)
        GameObject projectileInstance = Instantiate(clientProjectilePrefab, projectileSpawnPoint.position, projectileSpawnRotation.rotation);

        // Setup movement for projectile
        if (projectileInstance.TryGetComponent(out MoveController mc))
        {
            mc.UpdateMoveDirection(Vector2.up);
            mc.Move();
        }

        // Tell server to also fire a projectile
        SpawnProjectileServerRpc();
    }

    // =================== Update loop ===================
    /// <summary>
    /// Reduces the cooldown timer if above 0 (cooldown in effect).
    /// </summary>
    public void Update()
    {
        if (currentCooldownLeft > 0)
        {
            currentCooldownLeft -= Time.deltaTime;
        }
    }
}

/// <summary>
/// Configuration data for burst firing behavior.
/// </summary>
[System.Serializable] 
public struct BurstFireInfo
{
    public int burstAmount; // Number of projectiles to fire in a single burst.
    public float timeBetweenShots; // Time in seconds between consecutive shots within a burst.
} 
