using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerControlsNew : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Mover mover;
    [SerializeField] private Aimer aimer;
    [SerializeField] private FiringController firingController;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }

        inputReader.MoveEvent += mover.UpdateMoveDirection;
        inputReader.AimEvent += aimer.AimWithMouse;
        inputReader.FireEvent += firingController.UpdateFireState;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }

        inputReader.MoveEvent -= mover.UpdateMoveDirection;
        inputReader.AimEvent -= aimer.AimWithMouse;
        inputReader.FireEvent -= firingController.UpdateFireState;
    }

    private void Update()
    {
        if (!IsOwner) { return; }

        mover.Move();
    }

    private void LateUpdate()
    {
        if (!IsOwner) { return; }

        aimer.Aim(Time.deltaTime); 
        firingController.FireProjectile();       
    } 
}
