using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gives the gameObject the ability to aim, by rotating it towards a target position or mouse input.
/// </summary>
/// <remarks>
/// Calculates a target rotation and applies smooth rotation toward that target using a max rotation speed.
/// </remarks>
public class AimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform selfTransform; // The transform that will be rotated to aim.    


    [Header("Settings")]
    [SerializeField] private float maxRotationSpeed = 100f; // Maximum angular speed for rotation, in degrees per second.
    [SerializeField] private float lookingAtTargetTolerance = 0.5f; // Angle threshold in degrees within which the object is considered to be facing the target.


    // =============== AIMING FUNCTIONALITY BELOW ===============
    private Quaternion targetRotation = Quaternion.identity; 

    /// <summary>
    /// Converts screen-space mouse input to world position and updates the target rotation.
    /// </summary>
    /// <param name="mouseInput">Mouse position in screen coordinates.</param>
    public void AimAtMouse(Vector2 mouseInput)
    {
        Vector2 mousePos = (Vector2) Camera.main.ScreenToWorldPoint(mouseInput);
        CalculateTargetRotation(mousePos);
    }

    /// <summary>
    /// Calculates the rotation needed to face a target position.
    /// </summary>
    /// <param name="targetPos">Target position in world space.</param>
    /// <param name="offsetRotation">Optional rotation offset in degrees. A value of 30 means 30 degrees to the left of targetPos, and a value of -30 means 30 degrees to the right of targetPos. </param>
    public void CalculateTargetRotation(Vector3 targetPos, float offsetRotation = 0)
    {
        // Calculate rotation needed to look at target
        Vector2 directionToLookAt = targetPos - selfTransform.position;
        float angle = (Mathf.Atan2(directionToLookAt.y, directionToLookAt.x) * Mathf.Rad2Deg) - 90f + offsetRotation;
        targetRotation = Quaternion.Euler(0, 0, angle);   
    }

    /// <summary>
    /// Rotates the object toward the target rotation using the configured max speed. This method is the one that actually rotates the gameObject in "selfTransform". 
    /// </summary>
    /// <param name="waitPerCall">Delta time or elapsed time since the last call (of this method).</param>
    public void ApplyRotation(float waitPerCall)
    {
        selfTransform.rotation = Quaternion.RotateTowards(selfTransform.rotation, targetRotation, maxRotationSpeed * waitPerCall);
    }

    /// <summary>
    /// Rotates the object toward the current target rotation until the angle difference is within tolerance, or the timeout is reached.
    /// </summary>
    /// <param name="waitPerCall">Time step used for each rotation step, typically delta time or a fixed wait interval.</param>
    /// <param name="timeoutThreshold">Maximum time in seconds allowed for the rotation to complete.</param>
    /// <returns>An IEnumerator that can be used with StartCoroutine.</returns>
    public IEnumerator CompleteRotationTowardsTarget(float waitPerCall, float timeoutThreshold)
    {
        float totalRuntime = 0;

        while (Quaternion.Angle(transform.rotation, targetRotation) > lookingAtTargetTolerance || totalRuntime > timeoutThreshold)
        {
            ApplyRotation(waitPerCall);
            yield return new WaitForSeconds(waitPerCall);

            totalRuntime += waitPerCall;
        }
    }
}
