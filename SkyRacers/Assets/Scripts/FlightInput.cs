using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyRacers
{
    public sealed class FlightInput : MonoBehaviour
    {
        public Vector3 Axes { get; private set; }
        public float ThrottleChange { get; private set; }
        public bool Braking { get; private set; }
        public Vector2 Look { get; private set; }
        public bool FlarePressed { get; private set; }
        public bool PausePressed => (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) || (Gamepad.current?.startButton.wasPressedThisFrame ?? false);
        public void Sample(bool active)
        {
            var k = Keyboard.current;
            var pad = Gamepad.current;
            Clear();
            if (!active) return;
            if (k != null)
            {
                Axes = new Vector3((k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0),
                    (k.eKey.isPressed ? 1 : 0) - (k.qKey.isPressed ? 1 : 0),
                    (k.aKey.isPressed ? 1 : 0) - (k.dKey.isPressed ? 1 : 0));
                ThrottleChange = ((k.leftShiftKey.isPressed || k.rightShiftKey.isPressed) ? 1 : 0)
                    - ((k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed) ? 1 : 0);
                Braking = k.xKey.isPressed; FlarePressed = k.fKey.wasPressedThisFrame;
                Look = new Vector2((k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0),
                    (k.upArrowKey.isPressed?1:0)-(k.downArrowKey.isPressed?1:0));
            }
            if (pad != null)
            {
                Vector2 stick=pad.leftStick.ReadValue();
                Axes += new Vector3(stick.y, (pad.rightShoulder.isPressed?1:0)-(pad.leftShoulder.isPressed?1:0), -stick.x);
                ThrottleChange += pad.rightTrigger.ReadValue()-pad.leftTrigger.ReadValue();
                Braking |= pad.buttonSouth.isPressed; FlarePressed |= pad.buttonEast.wasPressedThisFrame;
                Look += pad.rightStick.ReadValue();
            }
            Axes = new Vector3(Mathf.Clamp(Axes.x,-1,1),Mathf.Clamp(Axes.y,-1,1),Mathf.Clamp(Axes.z,-1,1));
            ThrottleChange=Mathf.Clamp(ThrottleChange,-1,1); Look=Vector2.ClampMagnitude(Look,1);
        }
        public void Clear() { Axes = Vector3.zero; ThrottleChange = 0; Braking = false; Look=Vector2.zero; FlarePressed=false; }
    }
}
