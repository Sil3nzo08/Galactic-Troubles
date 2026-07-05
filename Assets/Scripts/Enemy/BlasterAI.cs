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
    [SerializeField] private HitController hitController;
    [SerializeField] private Health health;
    [SerializeField] private GameObject playerBase;
    [SerializeField] private Transform selfTransform;

    [Header("Settings")]
    [SerializeField] protected float switchTargetCooldown = 3f;
    [SerializeField] protected float scanSurroundingsRate = 2f;
    [SerializeField] protected float retreatingDuration = 3f;
    [SerializeField] protected float retreatingCooldown = 15f;


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
        // In function settings...
        float waitPerCall = 0.1f;

        // Stop boosting (if it was boosting. Does nothing otherwise)
        boostController.UpdateBoostState(false);

        while (true)
        {
            // Aim at target
            aimController.CalculateTargetRotation(target.transform.position);
            aimController.ApplyRotation(waitPerCall);

            // Move/Strafe
            ProximityStatus pStatus = moveController.ObjectProximityToSelf(target.transform, 15f, 3f);

            switch (pStatus)
            {
                case ProximityStatus.Ideal:
                    // Strafing left/right (randomly)
                    Vector2 strafeDir = moveController.GetAStrafeDirection();
                    if (strafeDir != Vector2.zero)
                    {
                        moveController.UpdateMoveDirection(strafeDir);
                    }
                    moveController.Move();
                    break;
                
                case ProximityStatus.TooClose:
                    // Move backwards
                    moveController.ResetStrafeCooldown(); // So that if you switch back to "Ideal" immediately, you can strafe immediately!

                    moveController.UpdateMoveDirection(Vector2.down);
                    moveController.Move();
                    break;
                
                case ProximityStatus.TooFar:
                    // Move forwards
                    moveController.ResetStrafeCooldown(); 

                    moveController.UpdateMoveDirection(Vector2.up);
                    moveController.Move();
                    break;
            }

            // Firing
            ProximityStatus fireStatus = moveController.ObjectProximityToSelf(target.transform, 10f, 10f); // Essentially simulates 20 fire range
            
            if (fireStatus == ProximityStatus.Ideal)
            {
                firingController.TryBurstFire();
            }

            yield return new WaitForSeconds(waitPerCall);
        }

    }

    protected override IEnumerator Charging()
    {
        throw new System.NotImplementedException();
    }

    private float currRetreatingDuration = 0f;
    private float currRetreatingCooldown = 0f;
    protected override IEnumerator Retreating()
    {
        float waitPerCall = 0.1f;

        boostController.UpdateBoostState(true);
        currRetreatingDuration = retreatingDuration;
        currRetreatingCooldown = retreatingCooldown;

        while (true)
        {
            Vector2 awayFromTarget = selfTransform.position - target.transform.position.normalized;
            aimController.CalculateTargetRotation(awayFromTarget);
            aimController.ApplyRotation(waitPerCall);

            moveController.UpdateMoveDirection(Vector2.up);
            moveController.Move();

            if (currRetreatingDuration < 0)
            {
                enemyState.Value = EnemyState.Attacking;
            }

            yield return new WaitForSeconds(waitPerCall);
        }
    }

    protected override IEnumerator Scouting()
    {
        // In function settings...
        float waitPerCall = 0.1f;
        float randomAngle = Random.Range(0f, 2*Mathf.PI);

        // Disable boost (if it was on)
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
                yield return aimController.CompleteRotationTowardsTarget(waitPerCall, 10f);

                // Wait for 3 seconds before repeating
                yield return new WaitForSeconds(2f);
            }

            // === Moving forward functionality ===
            yield return MoveTowardsTarget(waitPerCall, 10f);
        }
    }

    // ==================== Class Specific ====================
    private void FindPlayerBase()
    {
        playerBase = GameObject.FindGameObjectWithTag("Core");
    }

    private IEnumerator MoveTowardsTarget(float waitPerCall, float duration)
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

    private void OnServerHit(ServerProjectile sp)
    {
        // We are reacting to the server's hit, changing state if need
        // be
        switch (enemyState.Value)
        {
            case EnemyState.Scouting:
                // Start attacking the shooter
                target = sp.GetProjectileData().owner;
                enemyState.Value = EnemyState.Attacking;
                break;
            
            case EnemyState.Attacking:
                // Retreat if low health upon getting shot
                if (health.currentHealth.Value <= 50 &&currRetreatingCooldown <= 0)
                {
                    enemyState.Value = EnemyState.Retreating;
                }

                break;

            case EnemyState.Retreating:
                // I guess update from who it's retreating from
                target = sp.GetProjectileData().owner;
                break;

            case EnemyState.Charging:
                // Do nothing, already on its last final breath
                break;
        }
    }
    

    // ======================= Runtime Methods =======================
    private void OnEnable()
    {
        hitController.OnServerHit += OnServerHit;
    }

    private void OnDisable()
    {
        hitController.OnServerHit -= OnServerHit;
    }
    
    private void Start()
    {
        FindPlayerBase();
    }

    private void Update()
    {
        // Cooldowns lowering as time passes
        if (switchTargetCooldown > 0)
        {
            switchTargetCooldown -= Time.deltaTime;
        }

        if (currRetreatingDuration > 0)
        {
            currRetreatingDuration -= Time.deltaTime;
        }

        if (currRetreatingCooldown > 0)
        {
            currRetreatingCooldown -= Time.deltaTime;
        }
    }
}
