using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DamageOnContactServer : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;

    public override void OnNetworkSpawn()
    {
        health.OnHealthDepleted += Die;
    }

    public override void OnNetworkDespawn()
    {
        health.OnHealthDepleted -= Die;
    }

    private void Die(Health health)
    {
        NetworkObject.Despawn(true);
    }
}
