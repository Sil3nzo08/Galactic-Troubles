using System;
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

    // Return: new boosts
    public event Func<Vector2> OnDirectionChange;


    private Vector2 normalisedDirection = Vector2.up;
    
    // =============== Movement Functionality ===============

    public void UpdateMoveDirection(Vector2 newNormalisedDirection)
    {   
        normalisedDirection = newNormalisedDirection;
    }

    public void Move()
    {
        Vector2 boostMovement = (Vector2) OnDirectionChange?.Invoke();
        if (boostMovement == null) { boostMovement = Vector2.one; }

        rb.velocity = (transform.right * normalisedDirection.x * xMovementSpeed * boostMovement.x) + (transform.up * normalisedDirection.y * yMovementSpeed * boostMovement.y);
    }

}
