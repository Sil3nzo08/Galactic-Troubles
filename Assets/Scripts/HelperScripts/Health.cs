using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Responsible for monitoring and controlling the health of gameObjects. Has events to let those subscribing know when health gets depleted, and when
/// health is damaged.
/// </summary>
public class Health : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;

    public event Action<Health> OnHealthDepleted;
    public event Action<Health> OnTakeDamage;
    public NetworkVariable<int> currentHealth { get; private set; } = new NetworkVariable<int>(); 

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        currentHealth.Value = maxHealth;
    }

    /// <summary>
    /// Decreases the health by the specified amount. If the health goes to a negative value, it gets set to 0 instead. 
    /// </summary>
    /// <param name="damageAmount"> The amount of health to decrease by </param>
    public void TakeDamage(int damageAmount)
    {
        if (currentHealth.Value > damageAmount)
        {
            currentHealth.Value -= damageAmount;
            OnTakeDamage?.Invoke(this);
        }
        else
        {
            currentHealth.Value = 0;
            OnHealthDepleted?.Invoke(this);
        }
    }

    /// <summary>
    /// Increases the health by the specified amount. If health goes above max health, it gets set to maxHealth instead.
    /// </summary>
    /// <param name="healAmount"> The amount of health to increase by </param>
    public void Heal(int healAmount)
    {
        currentHealth.Value += healAmount;

        if (currentHealth.Value > maxHealth)
        {
            currentHealth.Value = maxHealth;
        }
    }

    /// <summary>
    /// An "admin" method used for setting the health manually. Note that it still obeys to the [0, maxHealth] range, so any values outside this
    /// range get automatically truncated.
    /// </summary>
    /// <param name="newValue"> The new health value </param>
    public void ModifyHealth(int newValue)
    {
        int newValueRestricted = Mathf.Clamp(newValue, 0, maxHealth);
        currentHealth.Value = newValueRestricted;
    }
}
