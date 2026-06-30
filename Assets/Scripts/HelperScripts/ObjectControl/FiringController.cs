using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FiringController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private Transform projectileSpawnRotation;

    [Header("Settings")]
    [SerializeField] private float fireCooldown = 1f;


    private bool isFiring = false;
    private float currentCooldownLeft;
    public void UpdateFireState(bool isFiring)
    {
        this.isFiring = isFiring;
    }

    public void FireProjectile()
    {
        // Can't fire projectile yet
        if (!isFiring || currentCooldownLeft > 0) { return; }

        Instantiate(clientProjectilePrefab, projectileSpawnPoint.position, projectileSpawnRotation.rotation);

        currentCooldownLeft = fireCooldown;

        SpawnProjectileServerRpc();
    }

    [ClientRpc] 
    private void SpawnProjectileClientRpc()
    {
        if (IsOwner) { return; }

        Instantiate(clientProjectilePrefab, projectileSpawnPoint.position, projectileSpawnRotation.rotation);
    }

    [ServerRpc]
    private void SpawnProjectileServerRpc()
    {
        GameObject projectileInstance = Instantiate(serverProjectilePrefab, projectileSpawnPoint.position, projectileSpawnRotation.rotation);
        if (projectileInstance.TryGetComponent(out ProjectileHits projectileHits))
        {
            projectileHits.SetSourceShooter(gameObject);
        }

        SpawnProjectileClientRpc();
    }

    // =================== Update loop ===================
    public void Update()
    {
        if (currentCooldownLeft > 0)
        {
            currentCooldownLeft -= Time.deltaTime;
        }
    }
}
