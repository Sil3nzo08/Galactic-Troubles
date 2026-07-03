using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gives the gameObject the ability to move, using a Rigidbody2D.
/// </summary>
/// <remarks>
/// Provides methods to update movement direction, apply movement velocity,
/// check mouse proximity, and stop motion. External boosters can subscribe to
/// <see cref="OnDirectionChange"/> to modify movement input.
/// </remarks>
public class MoveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform selfTransform; // The one that is moving

    [Header("Settings")]
    [SerializeField] private float xMovementSpeed = 5f; // Horizontal
    [SerializeField] private float yMovementSpeed = 5f; // Vertical
    [SerializeField] private float atMouseTolerance = 10f; // Decides when the ship stops when it reaches the mouse

    /// <summary>
    /// Fired when the direction of the movement changes/updates. Subscribers should return a Vector2, which represents an amplification of forward normalised direction. 
    /// </summary>
    public event Func<Vector2> OnDirectionChange;

    
    // =============== MOVEMENT FUNCTIONALITY BELOW ===============
    private Vector2 normalisedDirection = Vector2.zero;

    /// <summary>
    /// Updates the normalized movement direction used by <see cref="Move"/>.
    /// </summary>
    /// <param name="newNormalisedDirection">A normalized direction vector in local space.</param>
    public void UpdateMoveDirection(Vector2 newNormalisedDirection)
    {   
        normalisedDirection = newNormalisedDirection;
    }

    /// <summary>
    /// Applies velocity to the Rigidbody2D based on current direction. This velocity gets boosted by any subscribers listening.
    /// </summary>
    public void Move()
    {
        // Calculate boosts based on the subscribers
        Vector2 boostMovement = Vector2.zero;
        foreach (Func<Vector2> subscriber in OnDirectionChange.GetInvocationList())
        {
            Vector2 returnVal = subscriber.Invoke();
            boostMovement += returnVal;
        }

        // Apply movement now
        rb.velocity = (transform.right * (normalisedDirection.x + boostMovement.x) * xMovementSpeed) + (transform.up * (normalisedDirection.y + boostMovement.y) * yMovementSpeed);
    }

    /// <summary>
    /// Returns true when the cursor is within the configured tolerance of the object's screen position.
    /// </summary>
    /// <returns>True if the mouse is close enough, otherwise false.</returns>
    public bool IsMouseCloseToSelf()
    {
        // Get the self and mouse positions relative to the screen.
        Vector2 selfPos = Camera.main.WorldToScreenPoint(selfTransform.position);
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Calculate distance and check if distance is within mouse tolerance range specified
        float dist = Vector2.Distance(selfPos, mousePos);
        if (dist < atMouseTolerance)
        {
            return true;
        } 
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Stops all movement by zeroing the Rigidbody2D velocity.
    /// </summary>
    public void StopMoving()
    {
        rb.velocity = Vector2.zero;
    }

    /// <summary>
    /// Evaluates the proximity of another object relative to this object (the one stored in "selfTransform").
    /// </summary>
    /// <param name="otherTrans">Transform of the object to check distance to.</param>
    /// <param name="idealDistance">Desired distance between objects.</param>
    /// <param name="distanceTolerance">Acceptable deviation from the ideal distance.</param>
    /// <returns>A <see cref="ProximityStatus"/> indicating if the object is too close, at ideal distance, or too far.</returns>
    public ProximityStatus ObjectProximityToSelf(Transform otherTrans, float idealDistance, float distanceTolerance) {
        float distance = Vector2.Distance(selfTransform.position, otherTrans.position);

        if (distance < (idealDistance - distanceTolerance)) {
            // Too close
            return ProximityStatus.TooClose;
        } else if (distance > (idealDistance + distanceTolerance)) {
            // Too far
            return ProximityStatus.TooFar;
        } else {
            // Goldilocks zone
            return ProximityStatus.Ideal;
        }
    }
}

/// <summary>
/// Describes the proximity relationship between two objects relative to an ideal distance.
/// </summary>
public enum ProximityStatus 
{
    TooClose, // Closer than ideal distance minus tolerance.
    Ideal, // Within the acceptable range around ideal distance.
    TooFar // Farther than ideal distance plus tolerance.   
}
