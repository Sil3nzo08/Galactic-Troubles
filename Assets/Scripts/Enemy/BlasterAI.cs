using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// AI controller for a blaster enemy that hunts targets through multi-stage combat behavior (scouting, attacking, charging, retreating).
/// </summary>
/// <remarks>
/// Manages state transitions based on proximity to target, health thresholds, and incoming damage. 
/// Uses burst firing and boost mechanics during different phases of combat.
/// </remarks>
public class BlasterAI : EnemyAI
{
    [Header("References")]
    [SerializeField] private SensorsController sensorsController; // Reference to the sensors controller for enemy detection.
    [SerializeField] private MoveController moveController; // Reference to the movement controller for directional movement and proximity queries.
    [SerializeField] private AimController aimController; // Reference to the aiming controller for target-facing rotation.
    [SerializeField] private FiringController normalFiringController; // Reference to the standard firing controller used during normal combat (Attacking/Scouting states).
    [SerializeField] private FiringController lastDitchFiringController; // Reference to the alternate firing controller used during Charging state (last stand before despawn).
    [SerializeField] private BoostController boostController; // Reference to the boost controller for speed bursts during charge and retreat.
    [SerializeField] private HitController hitController; // Reference to the hit controller for detecting incoming projectile damage.
    [SerializeField] private Health health; // Reference to this enemy's health component for state transitions based on health thresholds.
    [SerializeField] private GameObject playerBase; // Reference to the player base (Core) used as a scouting destination.
    [SerializeField] private Transform selfTransform; // Reference to this object's transform for position calculations relative to targets.

    [Header("Settings")]
    [SerializeField] protected float switchTargetCooldown = 3f; // Cooldown before this AI can switch to a new target (prevents erratic retargeting). I don't think this does anything yet...
    [SerializeField] protected float scanSurroundingsRate = 2f; // Frequency at which the AI scans surroundings for new targets.
    [SerializeField] protected float retreatingDuration = 3f; // Duration the AI remains in Retreating state before returning to Attacking.
    [SerializeField] protected float retreatingCooldown = 15f; // Cooldown before the AI can initiate another retreat (prevents constant retreating).
    [SerializeField] protected float healthThresholdToActivateRetreat = 50f; // Health threshold below which the AI will retreat during combat if retreat cooldown permits.
    [SerializeField] protected float healthThresholdToActivateCharge = 20f; // Health threshold below which the AI immediately transitions to Charging state, ignoring other combat logic.

    // ==================== Implementation ====================
    private GameObject target;

    /// <summary>
    /// Detects nearby enemies and targets them if switching cooldown has elapsed.
    /// </summary>
    /// <remarks>
    /// Transitions to Attacking state when a target is found while in Scouting state. 
    /// Respects switchTargetCooldown to prevent rapid target switching.
    /// </remarks>
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

    /// <summary>
    /// Main combat behavior: strafes around target while aiming and firing in bursts at range.
    /// </summary>
    /// <remarks>
    /// Maintains ideal distance from target through proximity checks (strafe if ideal, retreat if too close, advance if too far).
    /// Fires normal bursts when within firing range.
    /// </remarks>
    protected override IEnumerator Attacking()
    {
        // In function settings...
        float waitPerCall = 0.1f;

        // Stop boosting (if it was boosting. Does nothing otherwise)
        boostController.UpdateBoostState(false);

        while (true)
        {
            // Aim at target
            aimController.CalculateTargetRotation(GetTarget().transform.position);
            aimController.ApplyRotation(waitPerCall);

            // Move/Strafe
            ProximityStatus pStatus = moveController.ObjectProximityToSelf(GetTarget().transform, 15f, 3f);

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
            ProximityStatus fireStatus = moveController.ObjectProximityToSelf(GetTarget().transform, 10f, 10f); // Essentially simulates 20 fire range
            
            if (fireStatus == ProximityStatus.Ideal)
            {
                normalFiringController.TryBurstFire();
            }

            yield return new WaitForSeconds(waitPerCall);
        }

    }

    /// <summary>
    /// Aggressive end-game behavior: boosts toward target while firing continuous bursts, triggered at low health.
    /// </summary>
    /// <remarks>
    /// Activated when health drops below a certain threshold. Ignores proximity management and commits to a direct charge with boost enabled.
    /// Uses lastDitchFiringController for final barrage.
    /// </remarks>
    protected override IEnumerator Charging()
    {
        // In-function settings...
        float waitPerCall = 0.1f;

        // Turn boosts on
        boostController.UpdateBoostState(true);

        while (true)
        {
            // Aiming
            aimController.CalculateTargetRotation(GetTarget().transform.position);
            aimController.ApplyRotation(waitPerCall);
            
            // Moving
            moveController.UpdateMoveDirection(Vector2.up);
            moveController.Move();

            // Firing
            ProximityStatus fireStatus = moveController.ObjectProximityToSelf(GetTarget().transform, 10f, 10f); // Essentially simulates 20 fire range
            
            if (fireStatus == ProximityStatus.Ideal)
            {
                lastDitchFiringController.TryBurstFire();
            }

            yield return new WaitForSeconds(waitPerCall);
        }
    }

    private float currRetreatingDuration = 0f;
    private float currRetreatingCooldown = 0f;

    /// <summary>
    /// Defensive behavior: boosts away from target for a duration before returning to attack.
    /// </summary>
    /// <remarks>
    /// Activated when health drops below a certain threshold during combat and retreating cooldown is ready.
    /// Remains in this state for retreatingDuration, then transitions back to Attacking.
    /// Respects retreatingCooldown to prevent constant retreating.
    /// </remarks>
    protected override IEnumerator Retreating()
    {
        float waitPerCall = 0.1f;

        boostController.UpdateBoostState(true);
        currRetreatingDuration = retreatingDuration;
        currRetreatingCooldown = retreatingCooldown;

        while (true)
        {
            Vector2 awayFromTarget = selfTransform.position - GetTarget().transform.position.normalized;
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
            yield return MoveTowardsBase(waitPerCall, 10f);
        }
    }

    // ==================== Class Specific ====================
    /// <summary>
    /// Finds and caches the player base (Core) at startup for navigation during scouting.
    /// </summary>
    private void FindPlayerBase()
    {
        playerBase = GameObject.FindGameObjectWithTag("Core");
    }

    /// <summary>
    /// Moves toward the player base position while maintaining aim rotation for a specified duration.
    /// </summary>
    /// <param name="waitPerCall">Frame wait time in seconds for movement updates.</param>
    /// <param name="duration">How long to move before stopping.</param>
    private IEnumerator MoveTowardsBase(float waitPerCall, float duration)
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

    /// <summary>
    /// Handles incoming damage and triggers appropriate state responses (counter-attack, retreat, or charge).
    /// </summary>
    /// <param name="sp">The server projectile that hit this enemy.</param>
    private void OnServerHit(ServerProjectile sp)
    {
        // If we are too low, just start charging. Don't do anything else.
        if (health.currentHealth.Value <= healthThresholdToActivateCharge)
        {
            target = sp.GetProjectileData().owner;
            enemyState.Value = EnemyState.Charging;

            return;
        }
        
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
                if (health.currentHealth.Value <= healthThresholdToActivateRetreat &&currRetreatingCooldown <= 0)
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

    private GameObject GetTarget()
    {
        if (target == null)
        {
            target = playerBase;
        }

        return target;
    }
    

    // ======================= Runtime Methods =======================
    /// <summary>
    /// Subscribes to hit events when enabled to track incoming damage.
    /// </summary>
    private void OnEnable()
    {
        hitController.OnServerHit += OnServerHit;
    }

    /// <summary>
    /// Unsubscribes from hit events when disabled to prevent memory leaks.
    /// </summary>
    private void OnDisable()
    {
        hitController.OnServerHit -= OnServerHit;
    }
    
    /// <summary>
    /// Initializes the AI by finding and caching the player base reference.
    /// </summary>
    private void Start()
    {
        FindPlayerBase();
    }

    /// <summary>
    /// Decrements all active cooldown timers each frame.
    /// </summary>
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
