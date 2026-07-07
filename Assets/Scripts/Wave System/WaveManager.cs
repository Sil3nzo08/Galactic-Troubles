using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages wave progression, enemy spawning, and wave-to-wave transitions for the game.
/// </summary>
/// <remarks>
/// This component reads wave data, spawns enemy groups over time, and advances to the
/// next wave when the current wave is cleared.
/// </remarks>
public class WaveManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private WaveUIController waveUIController; // Reference to the UI controller that displays wave transition information.
    [SerializeField] private List<WaveData> waves; // The list of wave definitions that determine the sequence of gameplay waves.
    
    /// <summary>
    /// Invoked when the manager begins the next wave.
    /// </summary>
    public event Action<WaveManager> OnNextWave; // Invoked when next wave starts


    // ========================== HIDDEN FUNCTIONALITY ==========================
    private int currWave = 0; // The index of the currently active wave.
    private int currEnemyCount = 0; // The number of enemies currently alive in the active wave.

    /// <summary>
    /// Spawns all enemy groups defined by the provided wave data.
    /// </summary>
    /// <param name="waveData">
    /// The wave configuration containing the enemy groups to spawn.
    /// </param>
    /// <returns>
    /// An IEnumerator used by Unity's coroutine system.
    /// </returns>
    private IEnumerator SpawnWave(WaveData waveData)
    {
        // Spawn each group
        foreach (EnemyGroup group in waveData.entireWave)
        {
            // Wait/delay between each group as specified in the data
            yield return new WaitForSeconds(group.delayFromPrevGroup);

            // Spawn every enemy in the group
            foreach (EnemyEntry enemyCount in group.enemies)
            {
                for (int i = 0; i < enemyCount.spawnCount; i++)
                {
                    // Create the noise around the spawn point
                    float xNoise = UnityEngine.Random.Range(-group.noiseAroundSpawnPoint.x, group.noiseAroundSpawnPoint.x);
                    float yNoise = UnityEngine.Random.Range(-group.noiseAroundSpawnPoint.y, group.noiseAroundSpawnPoint.y);
                    Vector2 spawnPos = group.spawnPoint.position + new Vector3(xNoise, yNoise);

                    // Spawn enemy and update enemy count
                    GameObject enemy = Instantiate(enemyCount.enemy, spawnPos, Quaternion.identity);
                    currEnemyCount++;

                    if (enemy.TryGetComponent(out NetworkObject networkObject)) { 
                        // Spawn on the network (if possible)
                        networkObject.Spawn(); 
                    }

                    if (enemy.TryGetComponent(out DeathController dc))
                    {
                        // Subscribe to its OnDeath event (if possible)
                        dc.OnDeath += UpdateCountUponEnemyDeath;
                    }


                    yield return new WaitForSeconds(group.timeBetweenEnemySpawnsInGroup);
                }
            }
        }
    }

    /// <summary>
    /// Advances the wave system to the next wave after the current wave has been cleared.
    /// </summary>
    /// <returns>
    /// An IEnumerator used by Unity's coroutine system.
    /// </returns>
    private IEnumerator TransitionToNextWave()
    {
        if (currWave != waves.Count) { currWave++; } // Increment while we're not on the last wave defined, to avoid errors

        // Show text
        yield return waveUIController.TransitionToNextWave(currWave);

        // Once text is finished showing, spawn wave
        StartCoroutine(SpawnWave(waves[currWave - 1]));
    }
    
    /// <summary>
    /// Updates the active enemy count when an enemy dies and advances the wave if needed.
    /// </summary>
    /// <param name="deathController">
    /// The death controller attached to the enemy that just died.
    /// </param>
    private void UpdateCountUponEnemyDeath(DeathController deathController)
    {   
        // Reduce enemy count by 1
        currEnemyCount--;

        // Unsubscribe from their deathController as they are going to die
        deathController.OnDeath -= UpdateCountUponEnemyDeath;

        // Check if that was the last enemy. If so start the next wave
        if (currEnemyCount <= 0)
        {
            OnNextWave?.Invoke(this);
            StartCoroutine(TransitionToNextWave());
        }
    }

    // ======================== Runtime Methods ========================
    /// <summary>
    /// Called when the object is spawned on the network.
    /// </summary>
    /// <remarks>
    /// Starts the first wave transition on the server.
    /// </remarks>
    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        OnNextWave?.Invoke(this);
        StartCoroutine(TransitionToNextWave());
    }
}
