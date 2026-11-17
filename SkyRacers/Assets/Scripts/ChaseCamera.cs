using UnityEngine;

namespace SkyRacers
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        public FlightController target;
        public LayerMask terrainMask = 1;
        Camera lens;
        Vector3 smoothVelocity;
        void Awake() { lens = GetComponent<Camera>(); }
        public void Snap()
        {
            if (target == null) return;
            smoothVelocity = Vector3.zero;
            transform.position = DesiredPosition();
            Aim(1);
        }
        Vector3 DesiredPosition()
        {
            var anchor = target.transform.position + Vector3.up * 4;
            var desired = anchor - target.transform.forward * 34 + Vector3.up * 7;
            var delta = desired - anchor;
            if (Physics.SphereCast(anchor, 1.5f, delta.normalized, out var hit, delta.magnitude, terrainMask, QueryTriggerInteraction.Ignore))
                desired = anchor + delta.normalized * Mathf.Max(1, hit.distance - 1);
            return desired;
        }
        void LateUpdate()
        {
            if (target == null || Time.timeScale == 0) return;
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref smoothVelocity, .12f);
            Aim(1 - Mathf.Exp(-9 * Time.deltaTime));
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, Mathf.Lerp(57, 69, Mathf.InverseLerp(45, 125, target.Speed)), Time.deltaTime * 2);
        }
        void Aim(float blend)
        {
            var aim = target.transform.position + target.transform.forward * 45;
            var up = Vector3.Slerp(Vector3.up, target.transform.up, .18f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aim - transform.position, up), blend);
        }
    }
}
