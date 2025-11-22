using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this script to gameObjects that are involved with animations. Provides functionality to change animation speed, and destroy
/// gameObject.
/// </summary>
public class AnimationHelper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject rootGameObject;

    [Header("Settings")]
    [SerializeField] private float animationSpeed;

    private void Start()
    {
        animator.speed = animationSpeed;
    }

    public void DestroyGameObject()
    {
        Destroy(rootGameObject);
    }
}
