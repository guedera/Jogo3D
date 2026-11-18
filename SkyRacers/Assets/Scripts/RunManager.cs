using UnityEngine;
using UnityEngine.InputSystem;

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
        public RunState State { get; private set; } = RunState.Menu;
        public float Elapsed { get; private set; }
        public float Countdown { get; private set; }
        public int NextSector { get; private set; } = 1;
        public bool NewRecord { get; private set; }
        public bool IsFreeFlight { get; private set; }
        public float RemainingDistance => Aircraft == null ? Course.TotalDistance : Course.DistanceRemaining(Aircraft.Body.position, NextSector);
        public string Reason { get; private set; }
        public float Progress => Mathf.Clamp01((float)(NextSector - 1) / (Course.route.Length - 1));
        float crashUntil;
        public bool ShowingCrash => State == RunState.Failed && Time.time < crashUntil;
        bool crashMenuPending;
        RunState pausedFrom;
        Vector3 previousPosition;

        void Start()
        {
            Time.timeScale = 1; Time.fixedDeltaTime = .02f;
            Application.targetFrameRate = 60;
            Aircraft = useSceneAircraft ? aircraftPrefab : Instantiate(aircraftPrefab);
            Aircraft.name = "Player F18"; Aircraft.Run = this;
            chaseCamera.target = Aircraft; ui.Initialize(this); MainMenu();
        }
        public void StartRun() => StartFlight(false);
        public void StartFreeFlight() => StartFlight(true);
        public void RestartFlight() => StartFlight(IsFreeFlight);
        void StartFlight(bool freeFlight)
        {
            crashUntil=0; crashMenuPending=false;
            IsFreeFlight = freeFlight;
            Time.timeScale = 1; State = freeFlight ? RunState.Running : RunState.Countdown; Countdown = freeFlight ? 0 : 3;
            Elapsed = 0; NextSector = 1; NewRecord = false; Reason = "";
            Vector3 spawn = Course.Spawn + (freeFlight ? Vector3.up * (Course.ceiling + 80) : Vector3.zero);
            Aircraft.ResetFlight(spawn, Course.SpawnRotation);
            if (freeFlight) Aircraft.Launch();
            previousPosition = spawn; Physics.SyncTransforms(); chaseCamera.Snap();
            Cursor.visible = false; ui.RefreshState();
        }
        public void MainMenu()
        {
            crashUntil=0; crashMenuPending=false;
            IsFreeFlight = false;
            Time.timeScale = 1; State = RunState.Menu; Elapsed = 0;
            Aircraft.ResetFlight(Course.Spawn, Course.SpawnRotation);
            Physics.SyncTransforms(); chaseCamera.Snap(); Cursor.visible = true; ui.RefreshState();
        }
        void Update()
        {
            if (Aircraft == null) return;
            if(crashMenuPending && !ShowingCrash) {crashMenuPending=false;ui.RefreshState();}
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (State == RunState.Paused) Resume();
                else if (State == RunState.Running || State == RunState.Countdown) Pause();
            }
            Aircraft.Input.Sample(State == RunState.Running);
            if (State == RunState.Countdown)
            {
                Countdown -= Time.deltaTime;
                if (Countdown <= 0) { State = RunState.Running; Aircraft.Launch(); ui.RefreshState(); }
            }
            // Physics and collision callbacks have finished before Update. A collision
            // therefore wins over a finish crossing in the same simulation step.
            if (State == RunState.Running && !IsFreeFlight)
            {
                var current = Aircraft.Body.position;
                if (Aircraft.HighestPoint >= Course.CeilingAt(current)) { Fail("Você ultrapassou o topo do cânion."); return; }
                if (current.z < -240 || current.z > Course.length + 240) { Fail("Você saiu do percurso."); return; }
                if (Course.CrossedBridgePlane(previousPosition, current, out var crossing) && !Course.UnderBridge(crossing))
                { Fail("Passe por baixo do arco central da ponte."); return; }
                while (NextSector < Course.route.Length && Course.CrossedSector(NextSector, previousPosition, current)) NextSector++;
                previousPosition = current;
                if (NextSector >= Course.route.Length)
                {
                    State = RunState.Won; NewRecord = RecordStore.Submit(Elapsed);
                    Aircraft.Freeze(); Cursor.visible = true; ui.RefreshState();
                }
            }
            ui.UpdateTelemetry();
        }
        void FixedUpdate() { if (State == RunState.Running && !IsFreeFlight) Elapsed += Time.fixedDeltaTime; }
        public void Pause()
        {
            if (State != RunState.Running && State != RunState.Countdown) return;
            pausedFrom = State; State = RunState.Paused; Time.timeScale = 0;
            Aircraft.Input.Clear(); Cursor.visible = true; ui.RefreshState();
        }
        public void Resume()
        {
            if (State != RunState.Paused) return;
            State = pausedFrom; Time.timeScale = 1; Cursor.visible = false; ui.RefreshState();
        }
        public void Crash()
        {
            if(State!=RunState.Running)return;
            Vector3 velocity=Aircraft.Body.linearVelocity;
            Reason="Colisão com o cânion.";State=RunState.Failed;crashUntil=Time.time+2.5f;crashMenuPending=true;
            Aircraft.Freeze();Aircraft.GetComponent<FlightEffects>()?.BreakApart(velocity);
            Cursor.visible=true;ui.RefreshState();
        }
        public void Fail(string reason)
        {
            if (State != RunState.Running) return;
            Reason = reason; State = RunState.Failed; Aircraft.Freeze(); Cursor.visible = true; ui.RefreshState();
        }
        void OnApplicationFocus(bool focused) { if (!focused && Aircraft != null) Pause(); }
        void OnDestroy() { Time.timeScale = 1; Cursor.visible = true; }
        public void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
