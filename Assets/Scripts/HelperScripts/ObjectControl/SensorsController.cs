using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Performs raycasts in multiple directions to detect objects on specified layers.
/// </summary>
/// <remarks>
/// Uses pre-configured ray directions and distances to scan the environment.
/// Results are returned in discovery order up to a specified maximum.
/// </remarks>
public class SensorsController : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private LayerMask targetLayers; // Layers to detect when raycasting (the layers the raycasts use to detect gameObjects on).
    [SerializeField] private RayCastInfo[] rayInfos; // Array of raycast configurations defining directions and distances to scan. Essentially defines all the rays to cast.
    
    /// <summary>
    /// Performs raycasts in all configured directions and returns discovered objects.
    /// </summary>
    /// <param name="maxObjectsToDiscover">Maximum number of objects to return. Defaults to 50.</param>
    /// <returns>A list of discovered GameObjects, up to the specified maximum.</returns>
    public List<GameObject> GenerateRaycasts(int maxObjectsToDiscover = 50)
    {
        List<GameObject> discoveredObjects = new List<GameObject>();
        RaycastHit2D[] hitBuffer = new RaycastHit2D[maxObjectsToDiscover];

        foreach (RayCastInfo rayInfo in rayInfos)
        {
            // Cast the ray
            int hitCount = Physics2D.RaycastNonAlloc(
                rayInfo.positionTransform.position,
                Quaternion.Euler(0, 0, rayInfo.rotationOffset) * rayInfo.directionTransform.up,
                hitBuffer,
                rayInfo.sightDistance,
                targetLayers);

            // Add the discovered objects onto the list to return, provided it doesn't go over the max number to discover.
            for (int i = 0; i < hitCount; i++)
            {
                discoveredObjects.Add(hitBuffer[i].rigidbody.gameObject);
                if (discoveredObjects.Count == maxObjectsToDiscover)
                {
                    return discoveredObjects;
                }
            }
        }

        return discoveredObjects;
    }
}


/// <summary>
/// Configuration data for a single raycast direction and distance. Defines the ray (use this information to cast such a ray)
/// </summary>
[System.Serializable]
public struct RayCastInfo
{
    public Transform positionTransform; // World position from which the raycast originates.
    public Transform directionTransform; // Transform whose up direction defines the raycast direction, before applying rotation offset.
    public float rotationOffset; // Rotation offset in degrees applied to the direction transform's up vector.
    public float sightDistance; // Maximum distance the raycast can travel.
}
