using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifetimeBeforeDestruction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifeTime;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
