using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public abstract class EnemyAINEW : NetworkBehaviour
{
    protected NetworkVariable<EnemyState> enemyState = new NetworkVariable<EnemyState>();
    protected Coroutine currentBehaviour; 

    /// <summary>
    /// The coroutine used for defining scouting behaviour. 
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected abstract IEnumerator Scouting();

    /// <summary>
    /// The coroutine used for defining retreating behaviour. 
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected abstract IEnumerator Retreating();

    /// <summary>
    /// The coroutine used for defining attacking behaviour. 
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected abstract IEnumerator Attacking();

    /// <summary>
    /// The coroutine used for defining charging behaviour. 
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected abstract IEnumerator Charging();

    protected void UpdateBehaviour(EnemyState previousValue, EnemyState newValue)
    {
        // Unsubscribe from current routine behaviour, and start new one.
        if (currentBehaviour != null)
        {
            StopCoroutine(currentBehaviour);
        }

        switch (newValue)
        {
            case EnemyState.Attacking:
                currentBehaviour = StartCoroutine(Attacking());
                break;
            case EnemyState.Retreating:
                currentBehaviour = StartCoroutine(Retreating());
                break;
            case EnemyState.Charging:
                currentBehaviour = StartCoroutine(Charging());
                break;
            case EnemyState.Scouting:
                currentBehaviour = StartCoroutine(Scouting());
                break;
            default:
                currentBehaviour = StartCoroutine(Attacking());
                break;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        enemyState.OnValueChanged += UpdateBehaviour;
        enemyState.Value = EnemyState.Scouting;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) { return; }

        StopAllCoroutines();
        enemyState.OnValueChanged -= UpdateBehaviour;
    }
}

/// <summary>
/// The different states that an enemy can have whilst in-game.
/// So far, there's scouting, attacking, retreating, and chasing.
/// </summary>
public enum EnemyState
{
    Scouting,
    Attacking,
    Retreating,
    Charging
}
