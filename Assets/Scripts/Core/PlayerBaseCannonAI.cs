using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerBaseCannonAI : NetworkBehaviour
{
    [Header("References")] 
    [SerializeField] private SensorsController sensorsController;
    [SerializeField] private AimController aimController;
    [SerializeField] private FiringController firingController;


    [Header("Settings")]
    [SerializeField] private float scanSurroundingsRate = 3f;


    private Coroutine scanSurroundingsCoroutine;
    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        StartCoroutine(ScanSurroundings());
        StartCoroutine(FireAtTarget());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) { return; }

        StopAllCoroutines();
    }

    private GameObject target;
    private IEnumerator ScanSurroundings()
    {
        while (true)
        {
            // Sensing
            List<GameObject> enemy = sensorsController.GenerateRaycasts(1);

            if (enemy.Count > 0) { target = enemy[0]; }

            yield return new WaitForSeconds(scanSurroundingsRate);
        }
    }

    private IEnumerator FireAtTarget()
    {
        float waitPerCall = 0.1f;

        while (true)
        {
            yield return new WaitForSeconds(waitPerCall);

            // Don't fire if there is no target
            if (target == null) { continue; } 

            // Aiming
            aimController.CalculateTargetClampedRotation(target.transform.position);
            aimController.ApplyRotation(waitPerCall);

            // Firing
            firingController.FireProjectileWithCooldown();
        }
    }



}
