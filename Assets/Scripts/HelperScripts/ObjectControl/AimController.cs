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
    [SerializeField] private RotationBoundaries rotationBoundaries;


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
    /// Calculates the rotation needed to face a target position, clamped within the configured rotation boundaries.
    /// </summary>
    /// <remarks>
    /// The resulting target rotation is constrained by the left and right angle boundaries defined in <see cref="rotationBoundaries"/>.
    /// Boundary ranges that wrap around (e.g., 270° to 90°) are handled correctly.
    /// </remarks>
    /// <param name="targetPos">Target position in world space.</param>
    /// <param name="offsetRotation">Optional rotation offset in degrees. A value of 30 means 30 degrees to the left of targetPos, and a value of -30 means 30 degrees to the right of targetPos.</param>
    public void CalculateTargetClampedRotation(Vector3 targetPos, float offsetRotation = 0)
    {
        // Calculate rotation needed to look at target
        Vector2 directionToLookAt = targetPos - selfTransform.position;
        float angle = (Mathf.Atan2(directionToLookAt.y, directionToLookAt.x) * Mathf.Rad2Deg) - 90f + offsetRotation;

        targetRotation = Quaternion.Euler(0, 0, ClampAngle(angle));
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

        while (Quaternion.Angle(transform.rotation, targetRotation) > lookingAtTargetTolerance && totalRuntime <= timeoutThreshold)
        {
            ApplyRotation(waitPerCall);
            yield return new WaitForSeconds(waitPerCall);

            totalRuntime += waitPerCall;
        }
    }

    // =================== HIDDEN FUNCTIONALITY ===================
    /// <summary>
    /// Converts an angle to the 0-360 degree range, handling negative angles correctly. 
    /// </summary>
    /// <param name="angle">The angle in degrees to normalize.</param>
    /// <returns>The normalized angle in the range [0, 360). 0° is UP, 90° is LEFT, 180° is DOWN, 270° is RIGHT </returns>
    private float NormalizeAngle(float angle)
    {
        float newAngle = angle % 360f;

        if (newAngle < 0f) { newAngle += 360f; }

        return newAngle;
    }

    /// <summary>
    /// Clamps an angle within the configured rotation boundaries, supporting both standard ranges and wrap-around ranges. Note that both the paramter and the return value has the degrees orientation as follows: 0° is UP, 90° is LEFT, 180° is DOWN, 270° is RIGHT.
    /// </summary>
    /// <remarks>
    /// Handles two cases: standard ranges where left ≤ right use direct clamping, and wrap-around ranges where left > right cross the 360°/0° boundary (e.g., 270° to 90°).
    /// </remarks>
    /// <param name="angle">The angle in degrees to clamp. </param>
    /// <returns>The clamped angle constrained within rotation boundaries. </returns>
    private float ClampAngle(float angle)
    {
        float normalizedAngle = NormalizeAngle(angle); // 0-360

        if (rotationBoundaries.leftAngleBoundary <= rotationBoundaries.rightAngleBoundary)
        {
            // e.g: [240°, 270°]
            return Mathf.Clamp(normalizedAngle, rotationBoundaries.leftAngleBoundary, rotationBoundaries.rightAngleBoundary);
        } 
        else
        {
            // e.g: [270°, 90°]
            
            // Push the left boundary and the angle if its below it up by 360° (do a full loop)
            if (normalizedAngle < rotationBoundaries.leftAngleBoundary) { normalizedAngle += 360f; } // so something like 80° -> 440°
            float rightBoundary = rotationBoundaries.rightAngleBoundary + 360f; // [270°, 450°]

            // Clamp angle to this new range
            float clampedAngle = Mathf.Clamp(normalizedAngle, rotationBoundaries.leftAngleBoundary, rightBoundary); // 440° -> 440°

            // Bring the angle back down to [0°, 360°] range
            clampedAngle %= 360f; // 440° -> 80°

            return clampedAngle; 
        }
    }
}

/// <summary>
/// Defines the minimum (left) and maximum (right) rotation angle boundaries for clamped aiming. The range is specified by starting at the left angle boundary, and working your way counter-clockwise until you reach the rightAngle boundary.
/// </summary>
/// <remarks>
/// Supports ranges that don't wrap (e.g., 45° to 135°) and ranges that wrap around 360° (e.g., 315° to 45°). 0° is UP, 90° is LEFT, 180° is DOWN, 270° is RIGHT
/// </remarks>
[System.Serializable]
public struct RotationBoundaries
{
    public float leftAngleBoundary; // The minimum allowed rotation angle in degrees.
    public float rightAngleBoundary; // The maximum allowed rotation angle in degrees.
}
