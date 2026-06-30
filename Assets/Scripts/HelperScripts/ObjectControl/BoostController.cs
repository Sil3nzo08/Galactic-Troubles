using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Callbacks;
using UnityEngine;

public class BoostController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem[] boostParticleSystems;
    [SerializeField] private Mover mover;

    [Header("Settings")] 
    [SerializeField] private float boostFactor = 2f; // In the forward A.K.A Vector2.up direction

    // ======================= Implementation =======================
    private bool hasBoostOn;
    public void UpdateBoostState(bool isBoostOn)
    {
        hasBoostOn = isBoostOn;
    }

    private Vector2 AmplifyForwardDirection()
    {
        if (hasBoostOn)
        {
            return new Vector2(1, boostFactor);
        } else
        {
            return Vector2.one;
        }
    }

    private void OnEnable()
    {
        mover.OnDirectionChange += AmplifyForwardDirection;
    }

    private void OnDisable()
    {
        mover.OnDirectionChange -= AmplifyForwardDirection;
    }


}
