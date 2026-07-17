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
    [SerializeField] private GameObject playerBase;


    [Header("Settings")]
    [SerializeField] private float scanSurroundingsRate = 2f;
    [SerializeField] private float switchScoutingDirectionCooldown = 1f;
    


    protected override IEnumerator ScanSurroundings()
    {
        while (true)
        {
            List<GameObject> enemies = sensorsController.GenerateRaycasts(1);

            if (enemies.Count != 0)
            {
                // Target core
                enemyState.Value = EnemyState.Attacking;
            }

            yield return new WaitForSeconds(scanSurroundingsRate);
        }
    }

    private float currSwitchScoutingDirectionCooldown = 0f;
    protected override IEnumerator Scouting()
    {
        float waitPerCall = 0.1f;
        float currOffset = 0f;

        while (true)
        {
            if (currSwitchScoutingDirectionCooldown <= 0)
            {
                currSwitchScoutingDirectionCooldown = switchScoutingDirectionCooldown; 

                currOffset = alternateOffsets(currOffset);
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
        throw new System.NotImplementedException();
    }

    protected override IEnumerator Retreating()
    {
        throw new System.NotImplementedException();
    }

    

    

    // ==================== Class Specific ====================
    /// <summary>
    /// Finds and caches the player base (Core) at startup for navigation during scouting.
    /// </summary>
    private void FindPlayerBase()
    {
        playerBase = GameObject.FindGameObjectWithTag("Core");
    }

    private float alternateOffsets(float currOffset)
    {
        if (currOffset == 45f)
        {
            return -45f;
        } else
        {
            return 45f;
        }
    }

    // ======================= Runtime Methods =======================
    /// <summary>
    /// Initializes the AI by finding and caching the player base reference.
    /// </summary>
    private void Start()
    {
        FindPlayerBase();
    }

    private void Update()
    {
        if (currSwitchScoutingDirectionCooldown > 0)
        {
            currSwitchScoutingDirectionCooldown -= Time.deltaTime;
        }
    }
}
