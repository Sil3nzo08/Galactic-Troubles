using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles client-side visual representation and effects for a projectile.
/// </summary>
/// <remarks>
/// Responsible for playing hit effects and despawning the visual projectile.
/// This is a dummy/visual-only component that does not affect gameplay logic.
/// </remarks>
public class ClientProjectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject hitEffect; // Particle effect or visual prefab to instantiate when the projectile hits.

    /// <summary>
    /// Instantiates the hit effect at the projectile's current position with a random rotation.
    /// </summary>
    public void ApplyHitEffect()
    {
        Instantiate(hitEffect, transform.position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
    }

    /// <summary>
    /// Destroys this visual projectile instance from the scene.
    /// </summary>
    public void Despawn()
    {
        Destroy(gameObject);
    }
}
