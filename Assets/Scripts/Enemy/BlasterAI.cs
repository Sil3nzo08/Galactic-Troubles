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
    [SerializeField] protected float scanSurroundingsRate = 2f;
    [SerializeField] protected float firingDistance = 12f;


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

    private float currentFiringCooldown = 0f;
    protected override IEnumerator Attacking()
    {
        boostController.UpdateBoostState(false);

        while (true)
        {
            // Aim at target
            aimController.CalculateTargetRotation(target.transform.position);
            aimController.ApplyRotation(0.1f);

            // Move/Strafe
            ProximityStatus pStatus = moveController.ObjectProximityToSelf(target.transform, 10f, 3f);

            switch (pStatus)
            {
                case ProximityStatus.Ideal:
                    // Strafing left/right (randomly)
                    Vector2 strafeDir = (Random.Range(0, 2) == 0) ? Vector2.left : Vector2.right;
                    moveController.UpdateMoveDirection(strafeDir.normalized);
                    moveController.Move();
                    break;
                
                case ProximityStatus.TooClose:
                    // Move backwards
                    moveController.UpdateMoveDirection(Vector2.down);
                    moveController.Move();
                    break;
                
                case ProximityStatus.TooFar:
                    // Move forwards
                    moveController.UpdateMoveDirection(Vector2.up);
                    moveController.Move();
                    break;
            }

            // Firing
            ProximityStatus fireStatus = moveController.ObjectProximityToSelf(target.transform, 6f, 6f); // Essentially simulates 12 fire range
            
            if (fireStatus == ProximityStatus.Ideal)
            {
                firingController.TryBurstFire();
            }

            yield return new WaitForSeconds(0.1f);
        }

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
            // === Looking around functionality ===
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

            // === Moving forward functionality ===
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

        moveController.StopMoving();
    }
    

    private void Update()
    {
        if (switchTargetCooldown > 0)
        {
            switchTargetCooldown -= Time.deltaTime;
        }
    }
}
