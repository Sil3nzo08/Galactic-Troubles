using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a kamikaze-style ship projectile that triggers an explosion effect and destroys itself when activated.
/// </summary>
/// <remarks>
/// This component is network-aware and uses server/client RPCs to ensure the explosion effect is displayed consistently across connected clients.
/// </remarks>
public class KamikazeShip : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Health health; // The health component used to destroy this ship when it explodes.
    [SerializeField] private GameObject explosionEffect; // The prefab or effect object spawned when the ship explodes.
    

    [Header("Settings")]
    [SerializeField] private ProjectileData data; // The projectile data associated with this kamikaze ship.

    /// <summary>
    /// Triggers the ship's explosion behavior by spawning an effect and setting its health to zero.
    /// </summary>
    public void Explode()
    {
        // Display explosion
        DisplayExplosionEffectServerRpc();

        // Set its own health to 0 (A.K.A dead)
        health.TakeDamage(health.currentHealth.Value);
    }

    /// <summary>
    /// Assigns the owner of this projectile to its associated projectile data.
    /// </summary>
    /// <param name="owner">The game object that should be set as the owner of the projectile.</param>
    public void SetOwner(GameObject owner)
    {
        data.owner = owner;
    }

    /// <summary>
    /// Returns the projectile data used by this kamikaze ship.
    /// </summary>
    /// <returns>The projectile configuration data for this ship.</returns>
    public ProjectileData GetProjectileData()
    {
        return data;
    }
    

    // =========================== Hidden Implementation ===========================
    /// <summary>
    /// Sends a request from the server to play the explosion effect on all clients.
    /// </summary>
    [ServerRpc]
    private void DisplayExplosionEffectServerRpc()
    {
        DisplayExplosionEffectClientRpc();
    }
    
    /// <summary>
    /// Spawns the explosion effect at the ship's current position on clients.
    /// </summary>
    [ClientRpc]
    private void DisplayExplosionEffectClientRpc()
    {
        Instantiate(explosionEffect, transform.position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
    } 
}
