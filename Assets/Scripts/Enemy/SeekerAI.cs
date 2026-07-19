using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SeekerAI : EnemyAI
{
    [Header("References")]
    [SerializeField] private MoveController moveController;
    [SerializeField] private AimController aimController;
    [SerializeField] private SensorsController sensorsController;
    [SerializeField] private SpriteController spriteController;
    [SerializeField] private HitController hitController;
    [SerializeField] private BoostController boostController;
    [SerializeField] private Health health;
    [SerializeField] private GameObject playerBase;


    [Header("Settings")]
    [SerializeField] private float scanSurroundingsRate = 1f;
    [SerializeField] private Vector2 directionSwitchCooldownRange = new Vector2(1f, 1.5f); // X-coord is lower bound, Y-coord is upper bound
    [SerializeField] private float healthThresholdToChargeAtPlayer = 40f;


    // ======================== Implementation ========================
    private GameObject target;


    protected override IEnumerator ScanSurroundings()
    {
        while (true)
        {
            List<GameObject> enemies = sensorsController.GenerateRaycasts(1);

            if (enemies.Count != 0 && enemyState.Value == EnemyState.Scouting)
            {
                // Target core
                enemyState.Value = EnemyState.Attacking;
            }

            yield return new WaitForSeconds(scanSurroundingsRate);
        }
    }

    private float currDirectionSwitchCooldown = 0f;
    protected override IEnumerator Scouting()
    {
        float waitPerCall = 0.1f;
        float currOffset = 0f;

        while (true)
        {
            if (currDirectionSwitchCooldown <= 0)
            {
                currDirectionSwitchCooldown = Random.Range(directionSwitchCooldownRange.x, directionSwitchCooldownRange.y); 

                currOffset = AlternateOffsets(currOffset);
                aimController.CalculateTargetRotation(playerBase.transform.position, currOffset);
            }

            aimController.ApplyRotation(waitPerCall);
            moveController.UpdateMoveDirection(Vector2.up);
            moveController.Move();

            yield return new WaitForSeconds(waitPerCall);
        }
    }



    protected override IEnumerator Attacking()
    {
        float waitPerCall = 0.1f;

        spriteController.SwitchSprite("Locked on");
        boostController.UpdateBoostState(true);

        while (true)
        {
            aimController.CalculateTargetRotation(playerBase.transform.position);
            aimController.ApplyRotation(waitPerCall);

            moveController.UpdateMoveDirection(Vector2.up);
            moveController.Move();

            yield return new WaitForSeconds(waitPerCall);
        }
    }

    protected override IEnumerator Charging()
    {
        float waitPerCall = 0.1f;

        spriteController.SwitchSprite("Locked on");
        boostController.UpdateBoostState(true);

        while (true)
        {
            aimController.CalculateTargetRotation(GetTarget().transform.position);
            aimController.ApplyRotation(waitPerCall);

            moveController.UpdateMoveDirection(Vector2.up);
            moveController.Move();

            yield return new WaitForSeconds(waitPerCall);
        }
    }

    protected override IEnumerator Retreating()
    {
        // Here, "target" refers to the player ship we want to run away from
        float waitPerCall = 0.1f;

        while (true)
        {
            // Calculate necessary vectors
            Vector2 towardsBase = (playerBase.transform.position - transform.position).normalized;
            Vector2 perpendicularToBase = new Vector2(-towardsBase.y, towardsBase.x).normalized;
            Vector2 toPlayer = (GetTarget().transform.position - transform.position).normalized;

            // Pick the better perpendicular direction (one that allows this ship to move away from the player/target)
            if (Vector2.Dot(perpendicularToBase, toPlayer) > 0)
            {
                perpendicularToBase = -perpendicularToBase;
            }

            // Aim
            perpendicularToBase += (Vector2) transform.position; // Offset normalized vector from current ships position, so CalculateTargetRotation() can work as intended
            aimController.CalculateTargetRotation(perpendicularToBase);
            aimController.ApplyRotation(waitPerCall);
            
            // Move
            moveController.UpdateMoveDirection(Vector2.up);
            moveController.Move();

            yield return new WaitForSeconds(waitPerCall);
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

    private GameObject GetTarget()
    {
        if (target == null)
        {
            target = playerBase;
        }

        return target;
    }

    private float AlternateOffsets(float currOffset)
    {
        if (currOffset == 45f)
        {
            return -45f;
        } else
        {
            return 45f;
        }
    }

    private void OnServerHit(ServerProjectile sp)
    {
        // Health low enough, so charge at player
        if (health.currentHealth.Value <= healthThresholdToChargeAtPlayer)
        {
            target = sp.GetProjectileData().owner;
            enemyState.Value = EnemyState.Charging;
        }
        // else try and move away from the player whilst repositioning for a better angle at the core 
        else
        {
            target = sp.GetProjectileData().owner;
            enemyState.Value = EnemyState.Retreating;
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



    /// <summary>
    /// Initializes the AI by finding and caching the player base reference.
    /// </summary>
    private void Start()
    {
        FindPlayerBase();
    }

    private void Update()
    {
        if (currDirectionSwitchCooldown > 0)
        {
            currDirectionSwitchCooldown -= Time.deltaTime;
        }
    }
}
