using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the death lifecycle of a networked object by monitoring health depletion and coordinating despawn across the network.
/// </summary>
/// <remarks>
/// When the associated <see cref="Health"/> component reaches zero, this controller invokes the <see cref="OnDeath"/> event to notify subscribers 
/// (such as drop spawners or score trackers) before despawning the object from the network.
/// </remarks>
public class DeathController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;


    /// <summary>
    /// Invoked when this object's health is depleted, before the object is despawned from the network. Subscribers receive a reference to this controller for identity tracking.
    /// </summary>
    public event Action<DeathController> OnDeath; 


    /// <summary>
    /// Notifies subscribers of death and despawns this object from the network.
    /// </summary>
    /// <param name="health">The health component that triggered the death event.</param>
    private void HandleDeath(Health health)
    {
        // This isn't a race condition, as all subscribers do their thing, and once all are complete, we can then move onto the next line Destroy()
        OnDeath?.Invoke(this);
        NetworkObject.Despawn();
    }


    // ==================== Runtime Methods ====================     

    private void OnEnable()
    {
        health.OnHealthDepleted += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnHealthDepleted -= HandleDeath;
    }
}
