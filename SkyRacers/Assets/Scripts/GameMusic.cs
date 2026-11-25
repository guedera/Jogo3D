using UnityEngine;

namespace SkyRacers
{
    public sealed class GameMusic : MonoBehaviour
    {
        AudioSource source;
        AudioClip menu,canyon;
        bool paused;
        void Awake()
        {
            source=gameObject.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=true;source.spatialBlend=0;
            source.volume=0;source.priority=160;
            menu=Resources.Load<AudioClip>("Music/Menu");
            canyon=Resources.Load<AudioClip>("Music/Canyon");
        }
        public void PlayMenu() => Play(menu);
        public void PlayFlight(bool freeFlight) => Play(freeFlight?null:canyon);
        void Play(AudioClip clip)
        {
            source.Stop();source.clip=clip;source.volume=0;paused=false;
            if(clip!=null)source.Play();
        }
        public void PauseMusic() {paused=true;source.Pause();}
        public void ResumeMusic() {paused=false;source.UnPause();}
        void Update()
        {
            if(!paused&&source.clip!=null)
                source.volume=Mathf.MoveTowards(source.volume,.28f,Time.unscaledDeltaTime*.2f);
        }
    }
}
