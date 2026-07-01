using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControlsNew : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Mover mover;
    [SerializeField] private Aimer aimer;
    [SerializeField] private FiringController firingController;
    [SerializeField] private BoostController boostController;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }

        inputReader.MoveEvent += mover.UpdateMoveDirection;
        inputReader.AimEvent += aimer.AimWithMouse;
        inputReader.FireEvent += firingController.UpdateFireState;
        inputReader.BoostEvent += boostController.UpdateBoostState;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }

        inputReader.MoveEvent -= mover.UpdateMoveDirection;
        inputReader.AimEvent -= aimer.AimWithMouse;
        inputReader.FireEvent -= firingController.UpdateFireState;
        inputReader.BoostEvent -= boostController.UpdateBoostState;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) { return; }

        if (mover.IsMouseCloseToSelf())
        {
            mover.StopMoving();
        } 
        else
        {
            mover.Move();
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) { return; }
        if (mover.IsMouseCloseToSelf()) { return; }

        aimer.AimWithMouse(Mouse.current.position.ReadValue());
        aimer.ApplyAim(Time.deltaTime); 

        firingController.FireProjectile();       
    } 
}
