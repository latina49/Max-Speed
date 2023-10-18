using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace H4R
{
    [CreateAssetMenu(fileName ="InputReader", menuName ="4ndrv/Input Reader")]
    public class InputReader : ScriptableObject, PlayerInputActions.IPlayerActions
    {
        public Vector3 Move => inputActions.Player.Move.ReadValue<Vector2>();
        public bool IsBraking => inputActions.Player.Brake.ReadValue<float>() >0;
        PlayerInputActions inputActions;

        private void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new PlayerInputActions();
                inputActions.Player.SetCallbacks(this);
            }
        }

        public void Enable()
        {
            inputActions.Enable();
        }

        public void OnBrake(InputAction.CallbackContext context)
        {
            //no
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            //no
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            //no
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            //no
        }
    }
}
