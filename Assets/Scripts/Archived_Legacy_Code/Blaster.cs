using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Blaster : EnemyAI
{
    private bool canSwitchTarget = true;     // can we switch targets?
    private bool hasRecentlyRetreated;      // has the enemy recently retreated?
    private bool gotShot;                   // did we get shot?

    [Header("Blaster-Specific")]
    [SerializeField] private float retreatCooldown;

    public override void OnNetworkSpawn()
    {
        // Server only will control the enemy ships.
        if (!IsServer) { return; }

        // Enable scanning surroundings for the rest of the enemy's life
        StartCoroutine(ScanSurroundings());

        // Start out by scouting the area.
        enemyState.Value = EnemyState.Scouting;
        currentBehaviour = StartCoroutine(Scouting());
        enemyState.OnValueChanged += UpdateBehaviour;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) { return; }

        StopAllCoroutines();

        enemyState.OnValueChanged -= UpdateBehaviour;
    }

    private void Start()
    {
        core = GameObject.FindGameObjectWithTag("Core");
    }

    // ====================== SIGHT + MOVEMENT OF SHIP =======================
    /// <summary>
    /// Generates raycasts that represent the ships line of sight, and returns any objects that come in this sight.
    /// </summary>
    /// <returns> Returns a list of gameObjects it saw in its line of sight, OR null if none were found. </returns>
    protected override List<GameObject> GenerateRaycasts()
    {
        RaycastHit2D hitInfoForward = Physics2D.Raycast(transform.position, transform.up, sightDistance, targetLayers);
        // 30 degrees to the left
        RaycastHit2D hitInfoLeft = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 30) * transform.up, sightDistance, targetLayers);
        // 30 degrees to the right  
        RaycastHit2D hitInfoRight = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -30) * transform.up, sightDistance, targetLayers);

        List<GameObject> discoveredEnemies = new List<GameObject>();
        if (hitInfoForward.collider != null)
        {
            discoveredEnemies.Add(hitInfoForward.rigidbody.gameObject);
        }
        else if (hitInfoLeft.collider != null)
        {
            discoveredEnemies.Add(hitInfoLeft.rigidbody.gameObject);
        }
        else if (hitInfoRight.collider != null)
        {
            discoveredEnemies.Add(hitInfoRight.rigidbody.gameObject);
        }

        if (discoveredEnemies.Count == 0)
        {
            return null;
        }
        else
        {
            return discoveredEnemies;
        }
    }

    // ========================= ATTACK PATTERNS ===============================
    /// <summary>
    /// Scans the surroundings/front direction using raycasts periodically
    /// </summary>
    /// <returns> Coroutine... </returns>
    private IEnumerator ScanSurroundings()
    {
        while (true)
        {
            List<GameObject> enemies = GenerateRaycasts();

            if (enemies != null && canSwitchTarget && enemyState.Value != EnemyState.Retreating)
            {
                bool coreInSight = false;
                GameObject chosenTarget = null;

                foreach (GameObject enemy in enemies)
                {
                    if (enemy.CompareTag("Core"))
                    {
                        coreInSight = true;
                    }
                    else
                    {
                        if (chosenTarget == null) { chosenTarget = enemy; }
                    }
                }

                if (coreInSight && chosenTarget == null)
                {
                    target = core;
                }
                else
                {
                    target = chosenTarget;
                }

                if (enemyState.Value == EnemyState.Scouting)
                {
                    enemyState.Value = EnemyState.Attacking;
                }

                StartCoroutine(CountdownSwitchTargetCooldown(switchTargetCooldown));
            }

            yield return new WaitForSeconds(2);
        }
    }

    /// <summary>
    /// The coroutine responsible for the attacking state of the enemy ship.
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected override IEnumerator Attacking()
    {
        float idealDistanceFromTarget = 10;
        float tolerance = 3;
        float waitPerCall = 0.05f;

        float durationOfStrafe = 0;
        Vector2 directionOfStrafe = ChooseRandomStrafeDirection();

        float firingCooldown = 4;
        float currentFiringCooldown = 0;

        int lowHealthThreshold = 40;
        int criticalHealthThreshold = 15;

        Vector2 betterStrafe = Vector2.zero;

        Boost(false);

        while (true)
        {
            yield return new WaitForSeconds(waitPerCall);

            // Aiming
            Aim(target, waitPerCall);

            if (helpingTheTarget != null)
            {
                betterStrafe = BetterHorizontalMovementAwayFromHelper();
            }
            else
            {
                betterStrafe = Vector2.zero;
            }

            // Movement
            float distanceFromTarget = Vector2.Distance(transform.position, target.transform.position);
            if (distanceFromTarget > idealDistanceFromTarget - tolerance           // Strafe if in ideal distance
                        && distanceFromTarget < idealDistanceFromTarget + tolerance)
            {
                if (durationOfStrafe > 2)
                {
                    directionOfStrafe = ChooseRandomStrafeDirection();
                    durationOfStrafe = 0;
                }

                if (helpingTheTarget != null)
                {
                    Move(betterStrafe);
                }
                else
                {
                    Move(directionOfStrafe);
                }

                durationOfStrafe += waitPerCall;
            }
            else if (distanceFromTarget < idealDistanceFromTarget)   // Move away if too close
            {
                Move((Vector2.down + betterStrafe).normalized);
            }
            else        // Otherwise, move forward
            {
                Move((Vector2.up + betterStrafe).normalized);
            }

            // Firing
            if (currentFiringCooldown > 0 )
            {
                currentFiringCooldown -= waitPerCall;
            }

            if (currentFiringCooldown <= 0 && CloseToTarget())
            {
                StartCoroutine(BurstFire(3, 0.2f));
                currentFiringCooldown = firingCooldown + currentFiringCooldown;
            }

            // Changing states 
            if (health.currentHealth.Value <= lowHealthThreshold && !hasRecentlyRetreated && gotShot)
            {
                gotShot = false;
                enemyState.Value = EnemyState.Retreating;
            }
            else if (health.currentHealth.Value <= criticalHealthThreshold)
            {
                enemyState.Value = EnemyState.Charging;
            }
        }
    }

    /// <summary>
    /// The coroutine responsble for the retreating state of the enemy ship
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected override IEnumerator Retreating()
    {
        float waitPerCall = 0.05f;
        float safeDistanceFromTarget = 30;

        float criticalHealthThreshold = 15;

        Boost(true);

        while (true)
        {
            yield return new WaitForSeconds(waitPerCall);

            if (helpingTheTarget != null)
            {
                Vector2 optimalDirectionAwayFromHelper = (transform.position - helpingTheTarget.transform.position).normalized;
                Vector2 optimalDirectionAwayFromTarget = (transform.position - target.transform.position).normalized;
                Vector2 optimalDirectionOverall = (optimalDirectionAwayFromHelper + optimalDirectionAwayFromTarget).normalized;

                Aim(optimalDirectionOverall, waitPerCall);
            }
            else
            {
                Aim(target, waitPerCall, 180);
            }

            Move(Vector2.up);

            if (Vector2.Distance(transform.position, target.transform.position) > safeDistanceFromTarget)
            {
                if (helpingTheTarget != null && Vector2.Distance(transform.position, helpingTheTarget.transform.position) <= safeDistanceFromTarget) { continue; }

                StartCoroutine(CountdownRetreatCooldown(retreatCooldown));
                enemyState.Value = EnemyState.Attacking;
            }
            else if (health.currentHealth.Value <= criticalHealthThreshold)
            {
                enemyState.Value = EnemyState.Charging;
            }
        }
    }

    /// <summary>
    /// The coroutine responsible for the charging state of the enemy ship. It's like a suicide attempt, unleashing everything nad going
    /// all out type of behaviour.
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected override IEnumerator Charging()
    {
        float waitPerCall = 0.05f;
        float fireCooldown = 2f;
        float currentCooldown = 0f;

        Boost(true);

        while (true)
        {
            yield return new WaitForSeconds(waitPerCall);

            Aim(target, waitPerCall);
            Move(Vector2.up);

            if (currentCooldown > 0)
            {
                currentCooldown -= waitPerCall;
            }

            if (currentCooldown <= 0 && CloseToTarget())
            {
                StartCoroutine(BurstFire(5, 0.15f));
                currentCooldown = fireCooldown + currentCooldown;
            }

        }
    }

    /// <summary>
    /// The coroutine responsible for the scouting state of the enemy ship. It slowly glides towards the core, and checks its surroundings periodically.
    /// </summary>
    /// <returns> Coroutine... </returns>
    protected override IEnumerator Scouting()
    {
        float waitPerCall = 0.05f;

        // cooldown for transitioning between marching towards core, and having a look around
        float lookAroundCooldown = 5;
        float currentLookAroundCooldown = lookAroundCooldown;
        bool isLookingAround = false;

        // storing the duration that is spent looking around during one check (by check, I mean one rotation)
        float checkDuration = 5;
        float currentCheckDuration = checkDuration;
        int currentCheckNum = 0;
        int maxChecksInOneGo = 2;

        // where the ship should look
        float lookingOffset = 30;

        Boost(false);

        while (true)
        {
            yield return new WaitForSeconds(waitPerCall);

            // Looking around functionality
            if (isLookingAround)
            {
                Aim(core, waitPerCall, lookingOffset);

                currentCheckDuration -= waitPerCall;
                if (currentCheckDuration <= 0)
                {
                    if (currentCheckNum != maxChecksInOneGo)
                    {
                        currentCheckNum++;
                        lookingOffset = lookingOffset + 180 + UnityEngine.Random.Range(-90, 90);

                        currentCheckDuration = checkDuration + currentCheckDuration;
                    }
                    else
                    {
                        isLookingAround = false;
                        currentCheckNum = 0;

                        currentCheckDuration = 0;
                    }
                }
            }
            // Marching towards core functionality
            else
            {
                Aim(core, waitPerCall);
                Move(Vector2.up);

                currentLookAroundCooldown -= waitPerCall;
                if (currentLookAroundCooldown <= 0)
                {
                    Move(Vector2.zero);

                    isLookingAround = true;
                    lookingOffset = UnityEngine.Random.Range(-180, 180);

                    currentLookAroundCooldown = lookAroundCooldown;
                }
            }
        }
    }

    /// <summary>
    /// A public method for other scripts to use, especially projectiles, to let the enemy AI that they've just been hit. Need to pass in the
    /// gameObject who attacked this enemy ship.
    /// </summary>
    /// <param name="attacker"> The player/gameObject that attacked this ship </param>
    public override void GotHit(GameObject attacker)
    {
        gotShot = true;

        if (attacker == target) { return; }

        if (attacker == null) { return; }

        if (enemyState.Value == EnemyState.Scouting)
        {
            target = attacker;
            StartCoroutine(CountdownSwitchTargetCooldown(switchTargetCooldown));

            enemyState.Value = EnemyState.Attacking;
        }
        else if (enemyState.Value == EnemyState.Attacking)
        {
            if (canSwitchTarget)
            {
                target = attacker;
                StartCoroutine(CountdownSwitchTargetCooldown(switchTargetCooldown));
            }
            else
            {
                helpingTheTarget = attacker;
                StartCoroutine(TrackHelpingTarget());
            }
        }
        else if (enemyState.Value == EnemyState.Charging)
        {
            if (canSwitchTarget)
            {
                target = attacker;
                StartCoroutine(CountdownSwitchTargetCooldown(switchTargetCooldown));
            }
        }
        else if (enemyState.Value == EnemyState.Retreating)
        {
            helpingTheTarget = attacker;
            StartCoroutine(TrackHelpingTarget());
        }
    }

    /// <summary>
    /// A public method for other scripts, specifically enemy ships, to gain insight about whether other enemy ships are willing to take in orders.
    /// </summary>
    /// <returns> True if they can listen to commands, and false otherwise. </returns>
    public bool IsListeningForCommands()
    {
        // return listeningForCommands;
        return false;
    }

    // ========================= HELPER METHODS ===============================

    /// <summary>
    /// Chooses a random direction to strafe, typically either directly left or right.
    /// </summary>
    /// <returns> The direction to strafe </returns>
    private Vector2 ChooseRandomStrafeDirection()
    {
        int random = UnityEngine.Random.Range(0, 2);

        if (random == 0)
        {
            return Vector2.left;
        }
        else if (random == 1)
        {
            return Vector2.right;
        }

        return Vector2.left;
    }

    /// <summary>
    /// Responsible for providing a cooldown on the enemy ship's ability to switch targets. Start this coroutine as soon as you switch targets, as this
    /// coroutine counts down the time at once! 
    /// </summary>
    /// <param name="cooldown"> How long, in seconds, before the enemy ship can switch targets again. </param>
    /// <returns> Coroutine... </returns>
    private IEnumerator CountdownSwitchTargetCooldown(float cooldown)
    {
        canSwitchTarget = false;

        yield return new WaitForSeconds(cooldown);

        canSwitchTarget = true;
    }

    private int callNum;
    /// <summary>
    /// Responsible for clearing the gameObject stored in "helpingTheTarget" after 5 seconds, to ensure the one helping the target is still "fresh". To
    /// use this method, simply call it when you've assigned "helpingTheTarget", so this coroutine can clear it 5 seconds later. It's a cooldown 
    /// that counts down essentially.
    /// </summary>
    /// <returns> Coroutine... </returns>
    private IEnumerator TrackHelpingTarget()
    {
        callNum++;

        yield return new WaitForSeconds(5);

        callNum--;

        if (callNum == 0)
        {
            helpingTheTarget = null;
        }
    }

    /// <summary>
    /// Responsible for counting down the cooldown for recently retreating, specifically modifying the variable "hasRecentlyRetreated". To use this 
    /// coroutine, simply start it as soon as the ship has retreated to begin the countdown/start the cooldown. 
    /// </summary>
    /// <param name="retreatCooldown"> How long until the ship can retreat again via "hasRecentlyRetreated" </param>
    /// <returns> Coroutine... </returns>
    private IEnumerator CountdownRetreatCooldown(float retreatCooldown)
    {
        hasRecentlyRetreated = true;

        yield return new WaitForSeconds(retreatCooldown);

        hasRecentlyRetreated = false;
    }

    /// <summary>
    /// Returns the Vector2 direction, such that moving in that direction allows you to move away from the gameObject stored in "helpingTheTarget".
    /// This method will only return either the right or left directions.  
    /// </summary>
    /// <returns> The Vector2 direction that helps move away from the "helpingTheTarget" object</returns>
    private Vector2 BetterHorizontalMovementAwayFromHelper()
    {
        Vector2 shipStrafedLeft = (Vector2)(transform.position - transform.right);
        Vector2 shipStrafedRight = (Vector2)(transform.position + transform.right);

        float distanceLeft = Vector2.Distance(helpingTheTarget.transform.position, shipStrafedLeft);
        float distanceRight = Vector2.Distance(helpingTheTarget.transform.position, shipStrafedRight);

        if (distanceLeft < distanceRight)
        {
            return Vector2.right;
        }
        else
        {
            return Vector2.left;
        }
    }

    /// <summary>
    /// Returns whether the enemy ship is close to the target. This gauge is determined by the firing distance, so if the enemy ship is too close to the 
    /// "target", past the firing distance, it is classified as "too close".
    /// </summary>
    /// <returns> true if the distance from the target is less than the firing distance, and false otherwise. </returns>
    private bool CloseToTarget()
    {
        float distance = Vector2.Distance(transform.position, target.transform.position);

        if (distance <= firingDistance)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
