using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aimer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform selfTransform;    

    [Header("Settings")]
    [SerializeField] private float maxRotationSpeed = 100f; // There's a max rotation speed at which the object can rotate (doesn't snap). 

    private Quaternion targetRotation = Quaternion.identity; 

    public void AimAtMouse(Vector2 mouseInput)
    {
        Vector2 mousePos = (Vector2) Camera.main.ScreenToWorldPoint(mouseInput);
        CalculateTargetRotation(mousePos);
    }

    // Use to update the rotation to "rotate to"
    public void CalculateTargetRotation(Vector3 targetPos, float offsetRotation = 0)
    {
        // Calculate rotation needed to look at target
        Vector2 directionToLookAt = targetPos - selfTransform.position;
        float angle = (Mathf.Atan2(directionToLookAt.y, directionToLookAt.x) * Mathf.Rad2Deg) - 90f + offsetRotation;
        targetRotation = Quaternion.Euler(0, 0, angle);   
    }

    // Aim
    public void ApplyRotation(float waitPerCall)
    {
        // Apply rotation
        selfTransform.rotation = Quaternion.RotateTowards(selfTransform.rotation, targetRotation, maxRotationSpeed * waitPerCall);
    }
}
