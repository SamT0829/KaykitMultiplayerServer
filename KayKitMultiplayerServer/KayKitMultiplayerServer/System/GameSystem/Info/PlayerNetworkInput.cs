using System.Collections.Generic;
using System;
using System.Linq;
using KayKitMultiplayerServer.Utility;
using UnityEngine;

//using UnityEngine;

namespace KayKitMultiplayerServer.System.GameSystem.Info
{
    public enum NetworkInputButtons : int
    {
        JUMP,
        FIRE,
        ThrowGrenade,
        RocketLauncherFire,
    }

    public class PlayerNetworkInput
    {
        private Dictionary<NetworkInputButtons, bool> buttonsTable = new Dictionary<NetworkInputButtons, bool>();

        public Vector2 movementInput;
        public Vector2 mousePointInput;
        public float gunRotationZ;
        public Vector3 gunAimDirection;

        public PlayerNetworkInput()
        {
        }

        public PlayerNetworkInput(float movementInputX, float movementInputY,
            float mousePointInputX, float mousePointInputY,
            Dictionary<NetworkInputButtons, bool> buttons,
            float[] gunAimDirection)
        {
            movementInput = new Vector2(movementInputX, movementInputY);
            mousePointInput = new Vector2(mousePointInputX, mousePointInputY);
            buttonsTable = buttons;
            this.gunAimDirection = gunAimDirection.ToVector3();
        }

        public void SetNetworkButtonInputData(NetworkInputButtons networkInputButtons, bool buttonState)
        {
            buttonsTable[networkInputButtons] = buttonState;
        }
        public bool GetNetworkButtonInputData(NetworkInputButtons networkInputButtons)
        {
            if (buttonsTable.TryGetValue(networkInputButtons, out bool _buttonState))
            {
                return _buttonState;
            }

            return false;
        }
        public List<object> CreateSerializedObject()
        {
            List<object> retv = new List<object>();
            retv.Add(movementInput.ToFloatArray());
            retv.Add(mousePointInput.ToFloatArray());
            retv.Add(gunAimDirection.ToFloatArray());

            var buttons = buttonsTable.ToDictionary(x => (int)x.Key, x => (object)x.Value);
            retv.Add(buttons);
            return retv;
        }
        public void DeserializeObject(object[] retv)
        {
            movementInput = ((float[])retv[0]).ToVector2();
            mousePointInput = ((float[])retv[1]).ToVector2();
            gunAimDirection = ((float[])retv[2]).ToVector3();

            if (retv[3] is Dictionary<int, object>)
            {
                Dictionary<int, object> buttons = (Dictionary<int, object>)(retv[3]);
                buttonsTable = buttons.ToDictionary(x => (NetworkInputButtons)x.Key, x => (bool)x.Value);
            }
        }
    }
}