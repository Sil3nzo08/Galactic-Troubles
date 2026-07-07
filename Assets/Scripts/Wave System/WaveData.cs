using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave", menuName = "Wave System/Wave Data", order = 1)]
public class WaveData : ScriptableObject
{
    public List<EnemyGroup> entireWave;
}

[System.Serializable]
public class EnemyGroup
{
    public float delayFromPrevGroup; // Once the previous wave group has spawned, this is the time in seconds until this group spawns
    public Transform spawnPoint;
    public Vector2 noiseAroundSpawnPoint; // Noise to randomise spawn location around spawn point
    public List<EnemyCount> enemies;
    public float timeBetweenEnemySpawnsInGroup; // The time between each enemy spawned in the group (all don't get instantiated instantly)
}

[System.Serializable]
public class EnemyCount
{
    public GameObject enemy;
    public int spawnCount;
}
