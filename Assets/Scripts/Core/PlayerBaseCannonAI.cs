using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Controls the base cannon AI behavior for a networked player unit.
/// </summary>
/// <remarks>
/// This component scans for nearby enemies, aims at the current target, and fires
/// projectiles when a valid target is available.
/// </remarks>
public class PlayerBaseCannonAI : NetworkBehaviour
{
    [Header("References")] 
    [SerializeField] private SensorsController sensorsController; // Reference to the sensor controller used to detect nearby targets.
    [SerializeField] private AimController aimController; // Reference to the aiming controller used to rotate the cannon toward a target.
    [SerializeField] private FiringController firingController; // Reference to the firing controller used to launch projectiles.


    [Header("Settings")]
    [SerializeField] private float scanSurroundingsRate = 3f; // The interval, in seconds, between target scans.


    // ==================== Private Methods ====================
    private GameObject target; // The currently selected enemy target, if one is detected.

    /// <summary>
    /// Continuously scans the surroundings for a valid enemy target.
    /// </summary>
    /// <returns>
    /// An IEnumerator used by Unity's coroutine system.
    /// </returns>
    private IEnumerator ScanSurroundings()
    {
        while (true)
        {
            // Sensing
            List<GameObject> enemy = sensorsController.GenerateRaycasts(1);

            if (enemy.Count > 0) 
            { 
                // Found a target
                target = enemy[0]; 
            } 
            else 
            {
                // Did not find a target
                target = null; 
            }

            yield return new WaitForSeconds(scanSurroundingsRate);
        }
    }

    /// <summary>
    /// Continuously aims at the current target and fires when a valid target exists.
    /// </summary>
    /// <returns>
    /// An IEnumerator used by Unity's coroutine system.
    /// </returns>
    private IEnumerator FireAtTarget()
    {
        float waitPerCall = 0.1f;

        while (true)
        {
            yield return new WaitForSeconds(waitPerCall);

            // Don't fire if there is no target
            if (target == null) { continue; } 

            // Aiming
            aimController.CalculateTargetClampedRotation(target.transform.position);
            aimController.ApplyRotation(waitPerCall);

            // Firing
            firingController.FireProjectileWithCooldown();
        }
    }


    // ==================== Runtime Methods ====================
    /// <summary>
    /// Called when the object is spawned on the network.
    /// </summary>
    /// <remarks>
    /// Starts the target-scanning and firing routines on the server.
    /// </remarks>
    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        StartCoroutine(ScanSurroundings());
        StartCoroutine(FireAtTarget());
    }

    /// <summary>
    /// Called when the object is despawned from the network.
    /// </summary>
    /// <remarks>
    /// Stops all running coroutines to prevent further AI behavior after despawn.
    /// </remarks>
    public override void OnNetworkDespawn()
    {
        if (!IsServer) { return; }

        StopAllCoroutines();
    }
}
