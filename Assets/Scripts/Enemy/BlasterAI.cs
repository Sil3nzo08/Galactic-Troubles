using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class BlasterAI : EnemyAINEW
{
    [Header("References")]
    [SerializeField] private SensorsController sensorsController;
    [SerializeField] private MoveController moveController;
    [SerializeField] private AimController aimController;
    [SerializeField] private FiringController firingController;
    [SerializeField] private BoostController boostController;
    [SerializeField] private Health health;
    [SerializeField] private GameObject playerBase;
    [SerializeField] private Transform selfTransform;

    [Header("Settings")]
    [SerializeField] protected float switchTargetCooldown = 3f;
    [SerializeField] protected float firingDistance = 12f;
    [SerializeField] protected float scanSurroundingsRate = 2f;


    // ==================== Implementation ====================
    private GameObject target;

    protected override IEnumerator ScanSurroundings()
    {
        while (true)
        {
            List<GameObject> enemies = sensorsController.GenerateRaycasts(1);

            if (enemies.Count != 0 && switchTargetCooldown <= 0 && enemyState.Value != EnemyState.Retreating)
            {
                target = enemies[0];

                if (enemyState.Value == EnemyState.Scouting)
                {
                    enemyState.Value = EnemyState.Attacking;
                }
            }

            yield return new WaitForSeconds(scanSurroundingsRate);
        }
    }

    protected override IEnumerator Attacking()
    {
        throw new System.NotImplementedException();
    }

    protected override IEnumerator Charging()
    {
        throw new System.NotImplementedException();
    }

    protected override IEnumerator Retreating()
    {
        throw new System.NotImplementedException();
    }

    protected override IEnumerator Scouting()
    {
        float waitPerCall = 0.1f;
        float randomAngle = Random.Range(0f, 2*Mathf.PI);
        boostController.UpdateBoostState(false);

        while (true)
        {
            // Looking around functionality
            for (int i = 0; i < 3; i++)
            {
                // Generate a random direction (via picking a random spot on the unit circle around the blaster)
                randomAngle = ((randomAngle * Mathf.Rad2Deg) + 180 + Random.Range(-90, 90)) * Mathf.Deg2Rad;
                Vector3 randomDirection = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));

                // Calculate this direction's rotation
                aimController.CalculateTargetRotation(selfTransform.position + randomDirection);

                // Coroutine to spend time aiming at the spot
                yield return StartCoroutine(aimController.CompleteRotationTowardsTarget(waitPerCall, 10f));

                // Wait for 3 seconds before repeating
                yield return new WaitForSeconds(3f);
            }

            // Moving forward
            yield return StartCoroutine(moveTowardsTarget(waitPerCall, 10f));
        }
    }

    // ==================== Class Specific ====================
    private IEnumerator moveTowardsTarget(float waitPerCall, float duration)
    {
        float currDuration = 0;
        while (currDuration < duration)
        {
            aimController.CalculateTargetRotation(playerBase.transform.position);
            aimController.ApplyRotation(waitPerCall);

            moveController.UpdateMoveDirection(Vector2.up);
            moveController.Move();

            yield return new WaitForSeconds(waitPerCall);
            currDuration += waitPerCall;
        }
    }
    

    private void Update()
    {
        if (switchTargetCooldown > 0)
        {
            switchTargetCooldown -= Time.deltaTime;
        }
    }
}
