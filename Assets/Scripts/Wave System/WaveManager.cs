using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WaveManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private List<WaveData> waves; // Each wave's data is contained in a single element in this list

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
                    float xNoise = Random.Range(-group.noiseAroundSpawnPoint.x, group.noiseAroundSpawnPoint.x);
                    float yNoise = Random.Range(-group.noiseAroundSpawnPoint.y, group.noiseAroundSpawnPoint.y);
                    Vector2 spawnPos = group.spawnPoint.position + new Vector3(xNoise, yNoise);

                    // Spawn enemy and wait
                    GameObject enemy = Instantiate(enemyCount.enemy, spawnPos, Quaternion.identity);
                    if (enemy.TryGetComponent(out NetworkObject networkObject)) {
                        networkObject.Spawn(); // Spawn on the network
                    }

                    yield return new WaitForSeconds(group.timeBetweenEnemySpawnsInGroup);
                }
            }
        }
    }

    // ======================== Runtime Methods ========================
    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        StartCoroutine(SpawnWave(waves[0]));
    }
}
