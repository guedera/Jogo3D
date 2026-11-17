using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyRacers
{
    public sealed class FlightInput : MonoBehaviour
    {
        public Vector3 Axes { get; private set; }
        public float ThrottleChange { get; private set; }
        public bool Braking { get; private set; }
        public void Sample(bool active)
        {
            var k = Keyboard.current;
            if (!active || k == null) { Clear(); return; }
            Axes = new Vector3((k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0),
                (k.eKey.isPressed ? 1 : 0) - (k.qKey.isPressed ? 1 : 0),
                (k.aKey.isPressed ? 1 : 0) - (k.dKey.isPressed ? 1 : 0));
            ThrottleChange = ((k.leftShiftKey.isPressed || k.rightShiftKey.isPressed) ? 1 : 0)
                - ((k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed) ? 1 : 0);
            Braking = k.xKey.isPressed;
        }
        public void Clear() { Axes = Vector3.zero; ThrottleChange = 0; Braking = false; }
    }
}
