using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Use for determining what happens when projectiles hit a target gameObject
/// </summary>
public class ProjectileHits : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject hitImpact;
    [SerializeField] private GameObject sourceShooter;

    [Header("Settings")]
    [SerializeField] private HitType hitType;
    [SerializeField] private int damage;

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (hitType == HitType.ClientSide)
        {
            Instantiate(hitImpact, transform.position, Quaternion.identity);

            if (collision.attachedRigidbody.TryGetComponent(out DamageOnContactClient clientDamage))
            {
                clientDamage.DamageEffect();
            }
        }

        if (hitType == HitType.ServerSide)
        {
            if (collision.attachedRigidbody.TryGetComponent(out Health healthScript))
            {
                healthScript.TakeDamage(damage);
            }

            if (collision.attachedRigidbody.TryGetComponent(out EnemyAI enemyAI))
            {
                enemyAI.GotHit(sourceShooter);
            }
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Set the shooter/source from which this projectile was shot from. Essentially, the gameObject who shot this projectile
    /// </summary>
    /// <param name="sourceShooter"> The gameObject who shot this projectile </param>
    public void SetSourceShooter(GameObject sourceShooter)
    {
        this.sourceShooter = sourceShooter;
    }
}

public enum HitType
{
    None,
    ServerSide,
    ClientSide
}
