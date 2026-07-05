using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Controls projectile firing and cooldown behavior for a networked object. Manages a two-tier projectile system where the server maintains authoritative simulation while clients display visual copies.
/// </summary>
/// <remarks>
/// <para>Projectile spawning is split into two types:</para>
/// <list type="bullet">
/// <item><description><strong>Server Projectile:</strong> Spawned on the server via ServerRpc, handles all game logic (collision detection, damage calculation, etc.)</description></item>
/// <item><description><strong>Client Projectile:</strong> Non-authoritative visual copies spawned on clients for immediate visual feedback without network latency.</description></item>
/// </list>
/// <para>The <see cref="shooterAuthority"/> field determines which entity initiated the projectile and controls which clients receive spawn notifications, preventing duplicate visuals on clients that already have local copies.</para>
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
    [SerializeField] private ShooterAuthority shooterAuthority; // Determines which entity type (server or client) initiated the projectile firing. Controls which clients receive the spawn notification RPC in
 
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
    /// Attempts to fire a projectile if the player is holding fire input and cooldown has elapsed.
    /// </summary>
    /// <remarks>
    /// Used for player-controlled firing where user input is required. Call <see cref="UpdateFireState"/> to update the firing input state.
    /// </remarks>
    public void FireProjectileWithCooldownAndInputActive()
    {
        // Can't fire projectile yet
        if (!isFiring || currentCooldownLeft > 0) { return; }

        // Fire projectile and update cooldown
        FireProjectile();
        currentCooldownLeft = fireCooldown;
    }

    /// <summary>
    /// Attempts to fire a projectile if cooldown has elapsed, without requiring user input.
    /// </summary>
    /// <remarks>
    /// Used for automated firing (e.g., enemies, timed attacks) that do not depend on player input. Only cooldown is checked.
    /// </remarks>
    public void FireProjectileWithCooldown()
    {
        // Can't fire projectile yet
        if (currentCooldownLeft > 0) { return; }

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
    /// Spawns a client-side projectile on remote clients except the host. Use this if you've already spawned the projectile on the host but not everyone else.
    /// </summary>
    [ClientRpc] 
    private void SpawnProjectileExceptHostClientRpc()
    {
        if (IsHost) { return; } 

        GameObject projectileInstance = Instantiate(clientProjectilePrefab, projectileSpawnPoint.position, projectileSpawnRotation.rotation);

        // Setup movement for the projectile
        if (projectileInstance.TryGetComponent(out MoveController mc))
        {
            mc.UpdateMoveDirection(Vector2.up);
            mc.Move();
        }
    }

    /// <summary>
    /// Spawns a client-side projectile on remote clients except the owner. Use this if you've already spawned the projectile on the owner but not everyone else.
    /// </summary>
    [ClientRpc] 
    private void SpawnProjectileExceptOwnerClientRpc()
    {
        if (IsOwner) { return; } // Everyone except the owner (whoever fired the projectile) spawns a  projectile

        GameObject projectileInstance = Instantiate(clientProjectilePrefab, projectileSpawnPoint.position, projectileSpawnRotation.rotation);

        // Setup movement for the projectile
        if (projectileInstance.TryGetComponent(out MoveController mc))
        {
            mc.UpdateMoveDirection(Vector2.up);
            mc.Move();
        }
    }

    /// <summary>
    /// Spawns the authoritative projectile on the server and broadcasts visual copies to appropriate clients based on shooter authority.
    /// </summary>
    /// <remarks>
    /// <para><strong>Shooter Authority determines client notification:</strong></para>
    /// <list type="bullet">
    /// <item><description><see cref="ShooterAuthority.aClientFiresIt"/>: Calls <see cref="SpawnProjectileExceptOwnerClientRpc()"/> so all clients except the firing owner receive a visual copy (owner already has one from <see cref="FireProjectile"/>).</description></item>
    /// <item><description><see cref="ShooterAuthority.serverFiresIt"/>: Calls <see cref="SpawnProjectileExceptHostClientRpc()"/> so all clients except the host receive a visual copy (host already has one from <see cref="FireProjectile"/>).</description></item>
    /// </list>
    /// <para>The server projectile instance is always authoritative and responsible for all game simulation, while client instances are purely visual.</para>
    /// </remarks>
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
        if (shooterAuthority == ShooterAuthority.aClientFiresIt)
        {
            // Like a player or client (this includes the host) firing a projectile from their spaceship
            SpawnProjectileExceptOwnerClientRpc();
        } else if (shooterAuthority == ShooterAuthority.serverFiresIt)
        {
            // A server-owned enemy fires a projectile, so everyone gets to see it (host excluded as FireProjectile implementation already spawned it for it)
            SpawnProjectileExceptHostClientRpc();
        }

        
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

/// <summary>
/// Determines which entity type initiated the projectile and controls RPC client notification logic.
/// </summary>
public enum ShooterAuthority
{
    serverFiresIt, // A server-owned entity (e.g., enemy) initiated the projectile. Non-host clients receive spawn notifications.
    aClientFiresIt // A client-owned entity (e.g., player) initiated the projectile. Non-owner clients receive spawn notifications.
}