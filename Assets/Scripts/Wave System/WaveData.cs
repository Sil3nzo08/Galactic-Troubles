using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores the configuration for a single wave in the wave system.
/// </summary>
[CreateAssetMenu(fileName = "Wave", menuName = "Wave System/Wave Data", order = 1)]
public class WaveData : ScriptableObject
{
    public List<EnemyGroup> entireWave; // The full list of enemy groups that compose this wave.
}

/// <summary>
/// Defines a single group of enemies that spawn together during a wave.
/// </summary>
[System.Serializable]
public class EnemyGroup
{
    public float delayFromPrevGroup; // The delay, in seconds, before this enemy group spawns after the previous group.
    public Transform spawnPoint; // The transform where this enemy group will spawn.
    public Vector2 noiseAroundSpawnPoint; // The random spread applied around the spawn point for each enemy.
    public List<EnemyEntry> enemies; // The enemies that should be spawned as part of this group.
    public float timeBetweenEnemySpawnsInGroup; // The time, in seconds, between each enemy spawn within this group.
}

/// <summary>
/// Represents a single enemy type and the number of times it should spawn.
/// </summary>
[System.Serializable]
public class EnemyEntry
{
    public GameObject enemy; // The enemy prefab to spawn.
    public int spawnCount; // The number of enemies to spawn for this entry.
}
