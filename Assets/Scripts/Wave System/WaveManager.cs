using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Unity.Netcode;
using UnityEngine;

public class WaveManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private WaveUIController waveUIController;
    [SerializeField] private List<WaveData> waves; // Each wave's data is contained in a single element in this list
    

    public event Action<WaveManager> OnNextWave; // Invoked when next wave starts


    // ========================== HIDDEN FUNCTIONALITY ==========================
    private int currWave = 0;
    private int currEnemyCount = 0;
    private IEnumerator SpawnWave(WaveData waveData)
    {
        // Spawn each group
        foreach (EnemyGroup group in waveData.entireWave)
        {
            // Wait/delay between each group as specified in the data
            yield return new WaitForSeconds(group.delayFromPrevGroup);

            // Spawn every enemy in the group
            foreach (EnemyCount enemyCount in group.enemies)
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

    private IEnumerator TransitionToNextWave()
    {
        if (currWave != waves.Count) { currWave++; } // Increment while we're not on the last wave defined, to avoid errors

        // Show text
        yield return waveUIController.TransitionToNextWave(currWave);

        // Once text is finished showing, spawn wave
        StartCoroutine(SpawnWave(waves[currWave - 1]));
    }
    
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
    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        StartCoroutine(TransitionToNextWave());
    }
}
