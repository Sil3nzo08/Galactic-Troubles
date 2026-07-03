using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects projectile collisions and handles damage, effects, and event notifications.
/// </summary>
/// <remarks>
/// Responds to both client-side visual projectiles and server-side authoritative projectiles.
/// Applies hit effects, reduces health, and fires events to notify subscribers of impacts.
/// </remarks>
public class HitController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health; // Health component that receives damage when hit by a server projectile.

    /// <summary>
    /// Fired when a server-side (authoritative) projectile collides with this object.
    /// Subscribers receive the server projectile that caused the impact.
    /// </summary>
    public event Action<ServerProjectile> OnServerHit; 

    /// <summary>
    /// Fired when a client-side (visual) projectile collides with this object.
    /// Subscribers receive the client projectile that caused the impact.
    /// </summary>
    public event Action<ClientProjectile> OnClientHit;

    /// <summary>
    /// Handles collision detection for projectiles entering the trigger zone.
    /// </summary>
    /// <remarks>
    /// For client projectiles: applies visual effects and despawns the projectile.
    /// For server projectiles: applies damage, despawns, and fires the server hit event.
    /// </remarks>
    /// <param name="other">Collider of the object entering the trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody.TryGetComponent(out ClientProjectile cp)) 
        {
            cp.ApplyHitEffect();
            cp.Despawn();

            OnClientHit?.Invoke(cp);
        } 
        
        if (other.attachedRigidbody.TryGetComponent(out ServerProjectile sp))
        {
            ProjectileData data = sp.GetProjectileData();

            health.TakeDamage(data.damage);
            sp.Despawn();    

            OnServerHit?.Invoke(sp);
        }
    }
}
