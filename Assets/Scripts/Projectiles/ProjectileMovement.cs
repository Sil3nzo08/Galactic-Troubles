using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Settings")]
    [SerializeField] private float projectileSpeed;

    void Start()
    {
        rb.velocity = transform.up * projectileSpeed;
    }
}
