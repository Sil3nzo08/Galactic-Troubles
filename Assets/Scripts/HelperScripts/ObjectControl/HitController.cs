using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects projectile collisions and handles damage, effects, and event notifications.
/// </summary>
/// <remarks>
/// Responds to trigger collisions from client-side visual projectiles, server-side authoritative projectiles,
/// and kamikaze-style ships. It applies visual effects, reduces health, and raises hit events for subscribers.
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
    /// Fired when a kamikaze ship collides with this object.
    /// Subscribers receive the kamikaze ship that caused the impact.
    /// </summary>
    public event Action<KamikazeShip> OnKamikazeHit;

    /// <summary>
    /// Handles triggers entered by projectile objects and applies the appropriate response.
    /// </summary>
    /// <param name="other">The collider that entered the trigger zone.</param>
    /// <remarks>
    /// If the entering object is a client projectile, the controller applies its hit effect and despawns it.
    /// If it is a server projectile, the controller applies damage, despawns it, and raises the server hit event.
    /// If it is a kamikaze ship, the controller applies damage and triggers its explosion logic.
    /// </remarks>
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

        if (other.attachedRigidbody.TryGetComponent(out KamikazeShip kp))
        {
            // This gameObject takes damage from kamikaze attack
            ProjectileData data = kp.GetProjectileData();
            health.TakeDamage(data.damage);
            
            // Make that kamikaze projectile explode
            kp.Explode();

            OnKamikazeHit?.Invoke(kp);
        }
    }
}
