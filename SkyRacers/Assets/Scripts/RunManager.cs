using UnityEngine;

namespace SkyRacers
{
    public enum RunState { Menu, Countdown, Running, Paused, Failed, Won }
    [DefaultExecutionOrder(-50)]
    public sealed class RunManager : MonoBehaviour
    {
        public FlightController aircraftPrefab;
        public bool useSceneAircraft;
        public CanyonCourse Course;
        public ChaseCamera chaseCamera;
        public GameUI ui;
        public FlightController Aircraft { get; private set; }
        public AirDefense Defense { get; private set; }
        public RunState State { get; private set; } = RunState.Menu;
        public float Elapsed { get; private set; }
        public float Countdown { get; private set; }
        public bool NewRecord { get; private set; }
        public bool IsFreeFlight { get; private set; }
        public bool Returning => mission != null && mission.Returning;
        public int NextSector => mission == null ? 1 : mission.NextSector;
        public int Flares => Defense == null ? MissionRules.FlareCapacity : Defense.Flares;
        public string Reason { get; private set; }
        public float RemainingDistance
        {
            get
            {
                if(IsFreeFlight || (mission != null && mission.Complete))return 0;
                if(Aircraft==null)return Course.TotalDistance*2;
                return Returning ? Course.TotalDistance-Course.DistanceRemaining(Aircraft.Body.position,NextSector+1)
                    : Course.TotalDistance+Course.DistanceRemaining(Aircraft.Body.position,NextSector);
            }
        }
        public float Progress => Mathf.Clamp01(1-RemainingDistance/(Course.TotalDistance*2));
        float crashUntil,turnNoticeUntil;
        public bool ShowingCrash => State == RunState.Failed && Time.time < crashUntil;
        public bool TurnNotice => Returning && Time.time<turnNoticeUntil;
        bool crashMenuPending;
        RunState pausedFrom;
        Vector3 previousPosition;
        MissionProgress mission;
        FreeFlightWorld freeWorld;
        GameMusic music;
        public float FloorAt(Vector3 position) => IsFreeFlight ? FreeFlightWorld.HeightAt(position.x,position.z) : Course.FloorAt(position.z);
        void Start()
        {
            Time.timeScale=1;Time.fixedDeltaTime=.02f;Application.targetFrameRate=60;
            Aircraft=useSceneAircraft?aircraftPrefab:Instantiate(aircraftPrefab);
            Aircraft.name="Player F18";Aircraft.Run=this;
            Defense=gameObject.AddComponent<AirDefense>();Defense.Initialize(this);
            SimpleClouds.Create(Course.transform,false);
            music=gameObject.AddComponent<GameMusic>();
            chaseCamera.target=Aircraft;ui.Initialize(this);MainMenu();
        }
        public void StartRun() => StartFlight(false);
        public void StartFreeFlight() => StartFlight(true);
        public void RestartFlight() => StartFlight(IsFreeFlight);
        void SelectWorld(bool free)
        {
            if(free && freeWorld==null)
            {
                var go=new GameObject("Free flight - Granite Range 20km");go.transform.SetParent(transform,false);
                freeWorld=go.AddComponent<FreeFlightWorld>();freeWorld.Generate(Course.rockMaterial);
                SimpleClouds.Create(freeWorld.transform,true);
            }
            Course.gameObject.SetActive(!free);
            if(freeWorld!=null)freeWorld.gameObject.SetActive(free);
        }
        void StartFlight(bool freeFlight)
        {
            crashUntil=0;crashMenuPending=false;turnNoticeUntil=0;IsFreeFlight=freeFlight;
            Time.timeScale=1;SelectWorld(freeFlight);Defense.ResetFlight();
            music.PlayFlight(freeFlight);
            State=freeFlight?RunState.Running:RunState.Countdown;Countdown=freeFlight?0:3;
            Elapsed=0;NewRecord=false;Reason="";mission=new MissionProgress(Course.route.Length);
            Vector3 spawn=freeFlight?freeWorld.Spawn:Course.Spawn;
            Aircraft.ResetFlight(spawn,freeFlight?Quaternion.identity:Course.SpawnRotation);
            if(freeFlight)Aircraft.Launch();
            previousPosition=spawn;Physics.SyncTransforms();chaseCamera.Snap();
            Cursor.visible=false;ui.RefreshState();
        }
        public void MainMenu()
        {
            crashUntil=0;crashMenuPending=false;turnNoticeUntil=0;IsFreeFlight=false;
            Time.timeScale=1;State=RunState.Menu;Elapsed=0;mission=new MissionProgress(Course.route.Length);
            music.PlayMenu();
            SelectWorld(false);Defense.ResetFlight();Aircraft.ResetFlight(Course.Spawn,Course.SpawnRotation);
            Physics.SyncTransforms();chaseCamera.Snap();Cursor.visible=true;ui.RefreshState();
        }
        void Update()
        {
            if(Aircraft==null)return;
            if(crashMenuPending&&!ShowingCrash) {crashMenuPending=false;ui.RefreshState();}
            if(Aircraft.Input.PausePressed)
            {
                if(State==RunState.Paused)Resume();
                else if(State==RunState.Running||State==RunState.Countdown)Pause();
            }
            Aircraft.Input.Sample(State==RunState.Running);
            if(Aircraft.Input.FlarePressed)Defense.DeployFlare();
            if(State==RunState.Countdown)
            {
                Countdown-=Time.deltaTime;
                if(Countdown<=0) {State=RunState.Running;Aircraft.Launch();ui.RefreshState();}
            }
            if(State==RunState.Running&&!IsFreeFlight)
            {
                Vector3 current=Aircraft.Body.position;
                if(Course.CrossedBridgePlane(previousPosition,current,out var crossing,Returning)&&!Course.UnderBridge(crossing))
                {Fail("Passe pelo arco central da ponte, na ida e na volta.");return;}
                bool wasReturning=Returning;
                while(!mission.Complete && Course.CrossedSector(NextSector,previousPosition,current,Returning))
                {
                    mission.Accept(NextSector);
                    if(Returning!=wasReturning) {turnNoticeUntil=Time.time+12;break;}
                }
                previousPosition=current;
                if(mission.Complete)
                {
                    State=RunState.Won;NewRecord=RecordStore.Submit(Elapsed);
                    Aircraft.Freeze();Cursor.visible=true;ui.RefreshState();
                }
            }
            ui.UpdateTelemetry();
        }
        void FixedUpdate() {if(State==RunState.Running&&!IsFreeFlight)Elapsed+=Time.fixedDeltaTime;}
        public void Pause()
        {
            if(State!=RunState.Running&&State!=RunState.Countdown)return;
            pausedFrom=State;State=RunState.Paused;Time.timeScale=0;music.PauseMusic();
            Aircraft.Input.Clear();Cursor.visible=true;ui.RefreshState();
        }
        public void Resume()
        {
            if(State!=RunState.Paused)return;
            State=pausedFrom;Time.timeScale=1;music.ResumeMusic();Cursor.visible=false;ui.RefreshState();
        }
        public void Crash(string reason="Colisão com o cenário.")
        {
            if(State!=RunState.Running)return;
            Vector3 velocity=Aircraft.Body.linearVelocity;
            Reason=reason;State=RunState.Failed;crashUntil=Time.time+2.5f;crashMenuPending=true;
            Aircraft.Freeze();Aircraft.GetComponent<FlightEffects>()?.BreakApart(velocity);
            Cursor.visible=true;ui.RefreshState();
        }
        public void Fail(string reason)
        {
            if(State!=RunState.Running)return;
            Reason=reason;State=RunState.Failed;Aircraft.Freeze();Cursor.visible=true;ui.RefreshState();
        }
        void OnApplicationFocus(bool focused) {if(!focused&&Aircraft!=null)Pause();}
        void OnDestroy() {Time.timeScale=1;Cursor.visible=true;}
        public void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
    }
}
