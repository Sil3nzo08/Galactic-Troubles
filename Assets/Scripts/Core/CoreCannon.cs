using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CoreCannon : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float originalRotation;
    [SerializeField] private float maxRotationSpeed;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private float sightDistance;

    [Header("Implementation-Wise")]
    public GameObject target;
    public List<GameObject> potentialTargets;

    public override void OnNetworkSpawn()
    {
        // Only the server will control the core.
        if (!IsServer) { return; }

        StartCoroutine(ScanSurroundings());
        StartCoroutine(Firing());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) { return; }

        StopAllCoroutines();
    }

    private IEnumerator ScanSurroundings()
    {
        while (true)
        {
            potentialTargets = GenerateRaycasts();

            if (potentialTargets != null)
            {
                target = potentialTargets[0];
            }

            yield return new WaitForSeconds(1f);
        }
        
    }

    private IEnumerator Firing()
    {
        while (true)
        {
            if (target != null)
            {
                Aim(target, 0.1f);
            }

            yield return new WaitForSeconds(0.1f);
        }
        
    }

    // ==== HELPER METHODS ====
    /// <summary>
    /// A method used to aim at the target. For "waitPerCall", just pass in the time you specified in WaitForSeconds().
    /// You can offset this aim if needed. Passing a value of 30 means aiming 30 degrees to the left of the target's position (so the ship won't obviously point towards the target)
    /// Note that it rotates "maxRotationDegrees" per second.
    /// </summary>
    /// <param name="target"> The target the enemy ship will aim at </param>
    /// <param name="waitPerCall"> The time between each call of Aim() A.K.A the time you specified in WaitForSeconds(). This is needed as an "alternative" to Time.deltaTime </param>
    /// <param name="offsetRotation"> How much you want to offset the rotation by. A value of 30 means 30 degrees to the left of targetPos, and a value of -30 means 30 degrees to the right of targetPos </param>
    private void Aim(GameObject target, float waitPerCall, float offsetRotation = 0)
    {
        if (target == null) { return; }
        
        transform.rotation = Quaternion.RotateTowards(transform.rotation, calculateTargetRotation(target.transform.position, offsetRotation),
                                maxRotationSpeed * waitPerCall);
    }

    /// <summary>
    /// Calculates and returns the quaternion needed to face the target's position. You can offset this
    /// rotation if needed. Passing a value of 30 means 30 degrees to the left of the targetPos's rotation (so the ship won't obviously point towards targetPos)
    /// </summary>
    /// <param name="targetPos">The position that the returned Quaternion will face </param>
    /// <param name="offsetRotation"> How much you want to offset the rotation by. A value of 30 means 30 degrees to the left of targetPos, and a value of -30 means 30 degrees to the right of targetPos </param>
    /// <returns>The quaternion that faces the target's position </returns>
    private Quaternion calculateTargetRotation(Vector3 targetPos, float offsetRotation = 0)
    {
        Vector2 directionToLookAt = targetPos - transform.position;
        float zDegrees = Mathf.Atan2(directionToLookAt.y, directionToLookAt.x) * (180 / Mathf.PI);

        if (zDegrees > originalRotation + 75 || zDegrees < originalRotation - 75)
        {
            return transform.rotation; // Don't change current rotation
        }

        Quaternion targetQuaternionRotation = Quaternion.Euler(0, 0, zDegrees - 90 + offsetRotation);
        return targetQuaternionRotation;
    }

    /// <summary>
    /// Generates raycasts that represent the core cannon's line of sight, and returns any objects that come in this sight.
    /// </summary>
    /// <returns> Returns a list of gameObjects it saw in its line of sight, OR null if none were found. </returns>
    private List<GameObject> GenerateRaycasts()
    {
        RaycastHit2D hitInfoForward = Physics2D.Raycast(transform.position, transform.up, sightDistance, targetLayers);
        // 45 degrees to the left
        RaycastHit2D hitInfoLeft = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 45) * transform.up, sightDistance, targetLayers);
        // 45 degrees to the right  
        RaycastHit2D hitInfoRight = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -45) * transform.up, sightDistance, targetLayers);

        List<GameObject> discoveredEnemies = new List<GameObject>();
        if (hitInfoForward.collider != null)
        {
            discoveredEnemies.Add(hitInfoForward.rigidbody.gameObject);
        }
        else if (hitInfoLeft.collider != null)
        {
            discoveredEnemies.Add(hitInfoLeft.rigidbody.gameObject);
        }
        else if (hitInfoRight.collider != null)
        {
            discoveredEnemies.Add(hitInfoRight.rigidbody.gameObject);
        }

        if (discoveredEnemies.Count == 0)
        {
            return null;
        }
        else
        {
            return discoveredEnemies;
        }
    }
}
