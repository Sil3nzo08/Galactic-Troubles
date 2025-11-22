using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Controls;

[CreateAssetMenu(fileName = "New Input Reader", menuName = "Input/Input Reader")]
public class InputReader : ScriptableObject, IPlayerActions
{
    public event Action<Vector2> MoveEvent;
    public event Action<Vector2> AimEvent;
    public event Action<bool> FireEvent;
    public event Action<bool> BoostEvent;

    private Controls controls;

    public void OnEnable()
    {
        controls = new Controls();
        controls.Player.SetCallbacks(this);

        controls.Enable();
    }

    public void OnDisable()
    {
        controls.Disable();
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        AimEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnBoost(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            BoostEvent?.Invoke(true);
        }
        else if (context.canceled)
        {
            BoostEvent?.Invoke(false);
        }
        
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            FireEvent?.Invoke(true);
        }
        else if (context.canceled)
        {
            FireEvent?.Invoke(false);
        }
    }
}
