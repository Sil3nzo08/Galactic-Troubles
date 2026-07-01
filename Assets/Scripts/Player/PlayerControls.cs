using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Routes local player input to movement, aiming, firing, and boost systems for the owning player. Defines how a player interacts with the game world.
/// </summary>
/// <remarks>
/// Subscribes to input events on spawn, unsubscribes on despawn,
/// and performs owner-only update dispatch for movement, aiming, and firing.
/// </remarks>
public class PlayerControls : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader; // Reads input events from the player's input system.
    [SerializeField] private MoveController moveController; // Movement controller that receives direction updates and performs Rigidbody2D movement.
    [SerializeField] private AimController aimController; // Aiming controller that computes and applies target rotation.
    [SerializeField] private FiringController firingController; // Firing controller that manages projectile firing and cooldowns.
    [SerializeField] private BoostController boostController; // Boost controller that applies forward boost and visual effects.

    /// <summary>
    /// Subscribes input events when this player object is spawned for the local owner.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }

        inputReader.MoveEvent += moveController.UpdateMoveDirection;
        inputReader.AimEvent += aimController.AimAtMouse;
        inputReader.FireEvent += firingController.UpdateFireState;
        inputReader.BoostEvent += boostController.UpdateBoostState;
    }

    /// <summary>
    /// Unsubscribes input events when this player object is despawned for the local owner.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }

        inputReader.MoveEvent -= moveController.UpdateMoveDirection;
        inputReader.AimEvent -= aimController.AimAtMouse;
        inputReader.FireEvent -= firingController.UpdateFireState;
        inputReader.BoostEvent -= boostController.UpdateBoostState;
    }

    /// <summary>
    /// Handles owner-only physics movement updates.
    /// </summary>
    private void FixedUpdate()
    {
        if (!IsOwner) { return; }

        if (moveController.IsMouseCloseToSelf())
        {
            moveController.StopMoving();
        } 
        else
        {
            moveController.Move();
        }
    }

    /// <summary>
    /// Handles owner-only aiming and firing updates after all other updates.
    /// </summary>
    private void LateUpdate()
    {
        if (!IsOwner) { return; }

        // Doesn't rotate if mouse is too close to self (the player)
        if (moveController.IsMouseCloseToSelf()) { return; }

        // Aim
        aimController.AimAtMouse(Mouse.current.position.ReadValue());
        aimController.ApplyRotation(Time.deltaTime); 

        // Fire
        firingController.FireProjectile();       
    } 
}
