using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Responsible for moving the gameObject
// Provides a set of functions that the "brain" of the object can use
public class Mover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform selfTransform; // The one that is moving

    [Header("Settings")]
    [SerializeField] private float xMovementSpeed = 5f; // Horizontal
    [SerializeField] private float yMovementSpeed = 5f; // Vertical
    [SerializeField] private float atMouseTolerance = 10f; // Decides when the ship stops when it reaches the mouse

    // Return: new boosts
    public event Func<Vector2> OnDirectionChange;


    
    // =============== Movement Functionality ===============
    private Vector2 normalisedDirection = Vector2.up;

    public void UpdateMoveDirection(Vector2 newNormalisedDirection)
    {   
        normalisedDirection = newNormalisedDirection;
    }

    public void Move()
    {
        // Apply boosts based on the subscribers
        Vector2 boostMovement = Vector2.zero;
        foreach (Func<Vector2> subscriber in OnDirectionChange.GetInvocationList())
        {
            Vector2 returnVal = subscriber.Invoke();
            boostMovement += returnVal;
        }

        // Apply movement now
        rb.velocity = (transform.right * (normalisedDirection.x + boostMovement.x) * xMovementSpeed) + (transform.up * (normalisedDirection.y + boostMovement.y) * yMovementSpeed);
    }

    public bool IsMouseCloseToSelf()
    {
        Vector2 selfPos = Camera.main.WorldToScreenPoint(selfTransform.position);
        Vector2 mousePos = Mouse.current.position.ReadValue();

        float dist = Vector2.Distance(selfPos, mousePos); 
        Debug.Log(dist);
        if (dist < atMouseTolerance)
        {
            return true;
        } 
        else
        {
            return false;
        }
    }

    public void StopMoving()
    {
        rb.velocity = Vector2.zero;
    }
}
