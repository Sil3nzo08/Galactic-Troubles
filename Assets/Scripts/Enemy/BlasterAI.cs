using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class BlasterAI : EnemyAINEW
{
    [Header("References")]
    [SerializeField] private SensorsController sensorsController;
    [SerializeField] private MoveController moveController;
    [SerializeField] private AimController aimController;
    [SerializeField] private FiringController firingController;
    [SerializeField] private BoostController boostController;
    

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
        throw new System.NotImplementedException();
    }
}
