using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SkyRacers
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        public FlightController target;
        public LayerMask terrainMask = 1;
        Camera lens;
        VolumeProfile lensProfile;
        Vector3 smoothVelocity;
        float orbitYaw, orbitPitch, lastLook;
        void Awake() { lens = GetComponent<Camera>(); }
        void Start()
        {
            var data=GetComponent<UniversalAdditionalCameraData>();
            if(data==null)data=gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing=true;
            // Fully blend distant geometry before the camera clips it away.
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogStartDistance=700;RenderSettings.fogEndDistance=2700;
            RenderSettings.fogColor=new Color(.66f,.74f,.80f);
            lens.farClipPlane=Mathf.Max(lens.farClipPlane,3500);
            lens.backgroundColor=RenderSettings.fogColor;
            var effects=new GameObject("Subtle flight lens");effects.transform.SetParent(transform,false);
            effects.layer=0;
            data.volumeLayerMask |= 1;
            var volume=effects.AddComponent<Volume>();volume.isGlobal=true;volume.priority=1000;
            lensProfile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=lensProfile;
            var vignette=lensProfile.Add<Vignette>();vignette.intensity.Override(.30f);vignette.smoothness.Override(.55f);
            var chromatic=lensProfile.Add<ChromaticAberration>();chromatic.intensity.Override(.24f);
        }
        void OnDestroy()
        {
            if(lensProfile==null)return;
            foreach(var component in lensProfile.components)Destroy(component);
            Destroy(lensProfile);
        }
        public void Snap()
        {
            if (target == null) return;
            smoothVelocity = Vector3.zero; orbitYaw=orbitPitch=0; lastLook=Time.time;
            transform.position = DesiredPosition();
            Aim(1);
        }
        Vector3 DesiredPosition()
        {
            var anchor = target.transform.position + Vector3.up * 4;
            var orbit = Quaternion.AngleAxis(orbitYaw, Vector3.up) * target.transform.rotation * Quaternion.Euler(orbitPitch,0,0);
            var desired = anchor - orbit * Vector3.forward * 34 + Vector3.up * 7;
            var delta = desired - anchor;
            if (Physics.SphereCast(anchor, 1.5f, delta.normalized, out var hit, delta.magnitude, terrainMask, QueryTriggerInteraction.Ignore))
                desired = anchor + delta.normalized * Mathf.Max(1, hit.distance - 1);
            return desired;
        }
        void LateUpdate()
        {
            if (target == null || Time.timeScale == 0 || (target.Run != null && target.Run.State == RunState.Failed)) return;
            Vector2 look=target.Input.Look;
            if(look.sqrMagnitude>.0025f)
            {
                orbitYaw=Mathf.Repeat(orbitYaw+look.x*100*Time.deltaTime+180,360)-180;
                orbitPitch=Mathf.Clamp(orbitPitch+look.y*70*Time.deltaTime,-65,65);lastLook=Time.time;
            }
            else if(Time.time-lastLook>=10)
            {
                orbitYaw=Mathf.MoveTowards(orbitYaw,0,100*Time.deltaTime);
                orbitPitch=Mathf.MoveTowards(orbitPitch,0,70*Time.deltaTime);
            }
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref smoothVelocity, .12f);
            Aim(1 - Mathf.Exp(-9 * Time.deltaTime));
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, Mathf.Lerp(57, 69, Mathf.InverseLerp(45, 125, target.Speed)), Time.deltaTime * 2);
        }
        void Aim(float blend)
        {
            float orbitBlend=Mathf.Clamp01((Mathf.Abs(orbitYaw)+Mathf.Abs(orbitPitch))/20);
            var aim = target.transform.position + target.transform.forward * Mathf.Lerp(45,0,orbitBlend);
            var up = Vector3.Slerp(Vector3.up, target.transform.up, .18f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aim - transform.position, up), blend);
        }
    }
}
