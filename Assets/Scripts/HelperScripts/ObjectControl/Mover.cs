using System;
using System.Collections;
using System.Collections.Generic;
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
        // Apply boosts based on the subscribers
        Vector2 boostMovement = Vector2.one;
        foreach (Func<Vector2> subscriber in OnDirectionChange.GetInvocationList())
        {
            Vector2 returnVal = subscriber.Invoke();
            boostMovement += returnVal;

            Debug.Log(boostMovement);
        }

        // Apply movement now
        rb.velocity = (transform.right * normalisedDirection.x * xMovementSpeed * boostMovement.x) + (transform.up * normalisedDirection.y * yMovementSpeed * boostMovement.y);
    }

}
