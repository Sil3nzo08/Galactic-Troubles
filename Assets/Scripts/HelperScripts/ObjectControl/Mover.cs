using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.Callbacks;
using UnityEngine;

// Responsible for moving the gameObject
// Provides a set of functions that the "brain" of the object can use
public class Mover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Settings")]
    [SerializeField] private float xMovementSpeed = 5f; // Horizontal
    [SerializeField] private float yMovementSpeed = 5f; // Vertical

    private Vector2 normalisedDirection = Vector2.up;
    // =============== Movement Functionality ===============

    public void UpdateMoveDirection(Vector2 newNormalisedDirection)
    {   
        normalisedDirection = newNormalisedDirection;
    }

    public void Move()
    {
        rb.velocity = (transform.right * normalisedDirection.x * xMovementSpeed) + (transform.up * normalisedDirection.y * yMovementSpeed);
    }

}
