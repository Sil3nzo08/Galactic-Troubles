using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls boost behavior and visual effects for a networked object. Essentially, gives the gameObject the ability to boost!
/// </summary>
/// <remarks>
/// Subscribes to <see cref="MoveController.OnDirectionChange"/> to apply forward boost
/// and synchronizes boost particle effects across clients.
/// </remarks>
public class BoostController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem[] boostParticleSystems; // Particle systems used to display boost effects.
    [SerializeField] private MoveController mover; // MoveController instance to provide boost direction data.

    [Header("Settings")] 
    [SerializeField] private float boostFactor = 2f; // Multiplier applied when boost is active in the forward direction.


    // =============== BOOSTING FUNCTIONALITY BELOW ===============
    private bool hasBoostOn = false;

    /// <summary>
    /// Enables or disables boost state and updates effects accordingly.
    /// </summary>
    /// <param name="isBoostOn">True to turn boost on; false to turn it off.</param>
    public void UpdateBoostState(bool isBoostOn)
    {
        hasBoostOn = isBoostOn;
        ApplyBoostEffects();
    }

    /// <summary>
    /// Returns a forward boost vector when boost is active.
    /// </summary>
    private Vector2 AmplifyForwardDirection()
    {
        if (hasBoostOn)
        {
            return new Vector2(0, boostFactor);
        } else
        {
            return Vector2.zero;
        }
    }

    /// <summary>
    /// Starts listening for direction boost requests when enabled.
    /// </summary>
    private void OnEnable()
    {
        mover.OnDirectionChange += AmplifyForwardDirection;
    }

    /// <summary>
    /// Stops listening for direction boost requests when disabled.
    /// </summary>
    private void OnDisable()
    {
        mover.OnDirectionChange -= AmplifyForwardDirection;
    }

    // =============== Boost Effects ===============
    /// <summary>
    /// Applies the appropriate visual effects for the current boost state. Depends on "hasBoostOn".
    /// </summary>
    private void ApplyBoostEffects()
    {
        if (hasBoostOn)
        {
            EnableBoostEffects();
            DisplayBoostEffectsServerRpc(true);
        } 
        else 
        {
            DisableBoostEffects();
            DisplayBoostEffectsServerRpc(false);
        }
    }

     /// <summary>
    /// Activates boost particle systems and plays them.
    /// </summary>
    private void EnableBoostEffects()
    {
        foreach (ParticleSystem ps in boostParticleSystems)
        {
            ps.gameObject.SetActive(true);
            ps.Play();
        }
    }

    /// <summary>
    /// Stops boost particle systems from emitting.
    /// </summary>
    private void DisableBoostEffects()
    {
        foreach (ParticleSystem ps in boostParticleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    /// <summary>
    /// Instructs the server to broadcast boost effect visibility to clients.
    /// </summary>
    /// <param name="turnEffectsOn">True to enable effects; false to disable.</param>
    [ServerRpc]
    private void DisplayBoostEffectsServerRpc(bool turnEffectsOn)
    {
        DisplayBoostEffectsClientRpc(turnEffectsOn);
    }

    /// <summary>
    /// Receives server instructions and toggles boost effects on all clients.
    /// </summary>
    /// <param name="turnEffectsOn">True to enable effects; false to disable.</param>
    [ClientRpc]
    private void DisplayBoostEffectsClientRpc(bool turnEffectsOn)
    {
        if (IsOwner) { return; } // Because we have already applied the boost effects to the owner prior to the RPC.

        if (turnEffectsOn)
        {
            EnableBoostEffects();
        } 
        else
        {
            DisableBoostEffects();
        }
    }
}
