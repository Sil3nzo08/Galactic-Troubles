using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class BoostController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem[] boostParticleSystems;
    [SerializeField] private Mover mover;

    [Header("Settings")] 
    [SerializeField] private float boostFactor = 2f; // In the forward A.K.A Vector2.up direction

    // ======================= Implementation =======================
    private bool hasBoostOn = false;
    public void UpdateBoostState(bool isBoostOn)
    {
        hasBoostOn = isBoostOn;
        ApplyBoostEffects();
    }

    private Vector2 AmplifyForwardDirection()
    {
        if (hasBoostOn)
        {
            return new Vector2(0, boostFactor);
        } else
        {
            return Vector2.zero;
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

    // == Boost Effects ==
    private void ApplyBoostEffects()
    {
        if (hasBoostOn)
        {
            EnableBoostEffects();
            DisplayBoostEffectsServerRpc(true);
        } 
        else 
        {
            DisableBoostEffects();
            DisplayBoostEffectsServerRpc(false);
        }
    }

    private void EnableBoostEffects()
    {
        foreach (ParticleSystem ps in boostParticleSystems)
        {
            ps.gameObject.SetActive(true);
            ps.Play();
        }
    }

    private void DisableBoostEffects()
    {
        foreach (ParticleSystem ps in boostParticleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    [ServerRpc]
    private void DisplayBoostEffectsServerRpc(bool turnEffectsOn)
    {
        DisplayBoostEffectsClientRpc(turnEffectsOn);
    }

    [ClientRpc]
    private void DisplayBoostEffectsClientRpc(bool turnEffectsOn)
    {
        if (turnEffectsOn)
        {
            EnableBoostEffects();
        } 
        else
        {
            DisableBoostEffects();
        }
    }
}
