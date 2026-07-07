using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Abstract base class for networked enemy AI behavior, managing state transitions and behavior coroutines.
/// </summary>
/// <remarks>
/// Derived classes implement state-specific behavior (Scouting, Attacking, Retreating, Charging) and surrounding detection.
/// The server controls state changes while all clients replicate the current enemy state.
/// </remarks>
public abstract class EnemyAI : NetworkBehaviour
{
    protected NetworkVariable<EnemyState> enemyState = new NetworkVariable<EnemyState>(); // The current AI behavior state, synchronized across the network. Controls which behavior coroutine is active.
    protected Coroutine currentBehaviour;  // Reference to the currently active behavior coroutine, used to stop it when transitioning to a new state.

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

    /// <summary>
    /// The coroutine used for defining how the enemy scans its surroundings. 
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected abstract IEnumerator ScanSurroundings();

    /// <summary>
    /// Handles state transitions by stopping the current behavior coroutine and starting the new one.
    /// </summary>
    /// <param name="previousValue">The previous enemy state (unused, required by NetworkVariable callback signature).</param>
    /// <param name="newValue">The new enemy state to transition into.</param>
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

    /// <summary>
    /// Initializes the enemy AI on the server: starts surroundings scanning and sets initial state to Scouting.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        // You need some form of way to scan your surroundings
        StartCoroutine(ScanSurroundings());

        enemyState.Value = EnemyState.Scouting;
        currentBehaviour = StartCoroutine(Scouting());

        enemyState.OnValueChanged += UpdateBehaviour;
    }

    /// <summary>
    /// Cleans up the enemy AI when despawning: stops all coroutines and unsubscribes from state change events.
    /// </summary>
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
