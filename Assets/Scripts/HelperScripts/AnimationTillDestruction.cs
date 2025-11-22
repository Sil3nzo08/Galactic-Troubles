using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTillDestruction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Settings")]
    [SerializeField] private float animationSpeed;

    private void Start()
    {
        animator.speed = animationSpeed;
    }

    private void Update()
    {
    }
}
