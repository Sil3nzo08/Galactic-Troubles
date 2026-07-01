using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SensorsController : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private RayCastInfo[] rayInfos;
    
    
    public List<GameObject> GenerateRaycasts(int maxObjectsToDiscover = 50)
    {
        List<GameObject> discoveredObjects = new List<GameObject>();
        RaycastHit2D[] hitBuffer = new RaycastHit2D[maxObjectsToDiscover];

        foreach (RayCastInfo rayInfo in rayInfos)
        {
            int hitCount = Physics2D.RaycastNonAlloc(
                rayInfo.positionTransform.position,
                Quaternion.Euler(0, 0, rayInfo.rotationOffset) * rayInfo.directionTransform.up,
                hitBuffer,
                rayInfo.sightDistance,
                targetLayers);

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



[System.Serializable]
public struct RayCastInfo
{
    public Transform positionTransform;
    public Transform directionTransform; // Uses its up direction
    public float rotationOffset; // Offset from rotationTransform's direction
    public float sightDistance;
}
