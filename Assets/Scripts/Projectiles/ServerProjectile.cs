using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the authoritative server-side projectile with gameplay-relevant data.
/// </summary>
/// <remarks>
/// Stores and provides access to projectile damage and owner information.
/// This component handles the authoritative simulation logic for the projectile.
/// </remarks>
public class ServerProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ProjectileData data; // Data structure containing damage and ownership information for this projectile.

    /// <summary>
    /// Sets the GameObject that fired this projectile (used for hit detection and damage attribution).
    /// </summary>
    /// <param name="owner">The GameObject that owns/fired this projectile.</param>
    public void SetOwner(GameObject owner)
    {
        data.owner = owner;
    }

    /// <summary>
    /// Retrieves the projectile's current data including damage and owner.
    /// </summary>
    /// <returns>A <see cref="ProjectileData"/> struct containing damage and owner information.</returns>
    public ProjectileData GetProjectileData()
    {
        return data;
    }

    /// <summary>
    /// Destroys this projectile instance from the scene.
    /// </summary>
    public void Despawn()
    {
        Destroy(gameObject);
    }
}

/// <summary>
/// Serializable data container for projectile gameplay properties.
/// </summary>
[System.Serializable] 
public struct ProjectileData
{
    public int damage; // Amount of damage this projectile deals on impact.
    public GameObject owner; // The GameObject that fired this projectile, used for hit detection to avoid friendly fire.
}
