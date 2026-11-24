using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;

namespace SkyRacers
{
    public sealed class GameUI : MonoBehaviour
    {
        static readonly Color Accent = new Color(.22f, 1f, .32f);
        static readonly Color Muted = new Color(.67f, .74f, .74f);
        RunManager run;
        Font font;
        RectTransform root, overlay, card, hud;
        Text heading, subtitle, speed, power, altitude, warning, countdown, mach, distance;
        Image powerBar;
        static readonly Color MenuAccent=new Color(.97f,.72f,.35f);
        Text mapTitle,mapDetail,controlsText,flares,clock,objective,navigationHint,bearing,threats;
        MissionMapGraphic map;
        RectTransform briefing;
        bool lastPad;

        Text[] speedTicks = new Text[HudTape.Rows], altitudeTicks = new Text[HudTape.Rows];
        Button primary, secondary, tertiary;
        Text primaryText, secondaryText, tertiaryText;

        public void Initialize(RunManager manager)
        {
            run = manager; font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>(); root = GetComponent<RectTransform>();
            if (EventSystem.current == null)
            {
                var events=new GameObject("Event System",typeof(EventSystem));
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            hud = Panel(root, "Flight HUD", 0, 0, 1600, 900, Color.clear);
            // Flight instruments only, transparent neon-green strokes; centre stays empty.
            speed = Gauge("IAS", 80, 220, speedTicks);
            altitude = Gauge("ALT AGL", 1340, 220, altitudeTicks);
            mach = Label(hud, "Mach", "", 80, 650, 210, 30, 19, Accent);
            power = Label(hud, "Power", "", 80, 710, 240, 35, 23, Accent);
            Panel(hud, "Engine scale", 80, 752, 180, 1, Accent);
            powerBar = Panel(hud, "Engine thrust", 80, 747, 100, 5, Accent).GetComponent<Image>();
            warning = Label(hud, "Altitude caution", "", 1280, 660, 285, 60, 19, Accent);
            threats=Label(hud,"Missile ranges","",1280,55,310,140,18,new Color(1,.45f,.2f));
            distance = Label(hud, "Distance remaining", "", 1280, 745, 300, 35, 22, Accent);
            countdown = Label(hud, "Preparation", "", 80, 75, 440, 30, 21, Accent);
            objective=Label(hud,"Mission leg","",80,115,420,30,19,Accent);
            bearing=Label(hud,"Next sector bearing","",80,151,420,30,18,Accent);
            flares=Label(hud,"Countermeasures","",80,790,300,30,21,Accent);
            clock=Label(hud,"Mission clock","",80,830,300,30,18,Accent);

            overlay=Panel(root,"Operations screen",0,0,1600,900,new Color(.025f,.045f,.052f,.96f));
            Panel(overlay,"Top rule",64,68,1472,1,new Color(.26f,.38f,.39f));
            Panel(overlay,"Bottom rule",64,834,1472,1,new Color(.26f,.38f,.39f));
            Label(overlay,"Unit","SR / 01     NAVAL FLIGHT OPERATIONS",64,30,800,26,17,MenuAccent);
            Label(overlay,"Status","OFFLINE     /     SINGLE PILOT",1180,30,360,26,15,Muted);
            Label(overlay,"Version","SKYRACERS  /  FLIGHT DECK",64,853,650,24,14,Muted);
            navigationHint=Label(overlay,"Navigation","",930,853,630,24,14,Muted);
            card=Rect(overlay,"Flight selection",64,100,640,710);
            Label(card,"Eyebrow","CHOOSE YOUR SORTIE",0,0,560,26,17,MenuAccent);
            heading=Label(card,"Title","SKY\nRACERS",-5,44,620,230,94,Color.white);heading.fontStyle=FontStyle.BoldAndItalic;
            subtitle=Label(card,"Brief","",0,282,580,115,20,Muted);
            primary=Button(card,"Primary",0,425,out primaryText);
            secondary=Button(card,"Secondary",0,512,out secondaryText);
            tertiary=Button(card,"Tertiary",0,599,out tertiaryText);
            var panel=Panel(overlay,"Tactical board",760,100,776,475,new Color(.04f,.075f,.082f));
            briefing=panel;
            Panel(panel,"Map accent",0,0,3,475,MenuAccent);
            mapTitle=Label(panel,"Map title","",26,19,725,32,22,MenuAccent);
            var graphic=Rect(panel,"Terrain chart",26,66,724,310);map=graphic.gameObject.AddComponent<MissionMapGraphic>();map.raycastTarget=false;
            mapDetail=Label(panel,"Map detail","",26,392,724,68,17,Muted);
            var controls=Panel(overlay,"Controls",760,596,776,215,Color.clear);
            Label(controls,"Title","PILOT CHECKLIST",0,0,725,28,16,MenuAccent);
            controlsText=Label(controls,"Bindings","",0,42,760,170,18,Muted);
            primary.gameObject.AddComponent<MenuOptionFocus>().focused=()=>Preview(false);
            secondary.gameObject.AddComponent<MenuOptionFocus>().focused=()=>Preview(true);
            UpdateBindings();
            primary.onClick.AddListener(() => {
                if (run.State == RunState.Paused) run.Resume();
                else if (run.State == RunState.Menu) run.StartRun();
                else run.RestartFlight();
            });
            secondary.onClick.AddListener(() => {
                if (run.State == RunState.Paused) run.RestartFlight();
                else if (run.State == RunState.Menu) run.StartFreeFlight();
                else run.MainMenu();
            });
            tertiary.onClick.AddListener(() => { if (run.State == RunState.Menu) run.Quit(); else run.MainMenu(); });
        }
        public void RefreshState()
        {
            if (overlay == null) return;
            bool menu = run.State == RunState.Menu, pause = run.State == RunState.Paused;
            bool ended = run.State == RunState.Failed || run.State == RunState.Won;
            overlay.gameObject.SetActive((menu || pause || ended) && !run.ShowingCrash);
            hud.gameObject.SetActive(!menu && !ended);
            primaryText.text = pause ? "CONTINUAR VOO" : menu ? "01   /   OPERAÇÃO CÂNION" : "TENTAR NOVAMENTE";
            secondaryText.text = pause ? "REINICIAR VOO" : menu ? "02   /   VOO LIVRE" : "MENU PRINCIPAL";
            tertiary.gameObject.SetActive(pause || menu); tertiaryText.text = menu ? "03   /   ENCERRAR SESSÃO" : "MENU PRINCIPAL";
            heading.text = menu ? "SKY\nRACERS" : pause ? "VOO\nPAUSADO" : run.State == RunState.Won ? "MISSÃO\nCUMPRIDA" : "FIM DA\nRUN";
            if (menu) subtitle.text = "Voe baixo. Cruze a ponte. Volte inteiro.\n\nUma missão de ida e volta, ou um campo de voo aberto para dominar o caça.";
            else if (pause) subtitle.text = run.IsFreeFlight ? "VOO LIVRE\n\nCampo de treino independente. Sem teto nem ameaças; colisões ativas." : "A física e o cronômetro estão pausados.\n\nContinue ou tente uma nova trajetória.";
            else if (run.State == RunState.Won) subtitle.text = (run.NewRecord ? "NOVO RECORDE!\n" : "PERCURSO CONCLUÍDO\n") + RecordStore.Format(run.Elapsed) + "\nMelhor: " + RecordStore.Format(RecordStore.Best);
            else subtitle.text = run.Reason + (run.IsFreeFlight ? "\n\nVOO LIVRE / Reinicie para continuar testando." : "\n\nTempo: " + RecordStore.Format(run.Elapsed));
            briefing.gameObject.SetActive(menu || pause);
            if(menu)Preview(false);
            var buttons=tertiary.gameObject.activeSelf?new[]{primary,secondary,tertiary}:new[]{primary,secondary};
            for(int i=0;i<buttons.Length;i++)buttons[i].navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=buttons[(i+buttons.Length-1)%buttons.Length],selectOnDown=buttons[(i+1)%buttons.Length]};
            if (overlay.gameObject.activeSelf && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(primary.gameObject);
        }
        public void UpdateTelemetry()
        {
            if (run.Aircraft == null) return;
            var plane = run.Aircraft;
            float kmh = plane.Speed * Mathf.Sqrt(FlightDynamics.Density(plane.transform.position.y) / 1.225f) * 3.6f;
            float agl = plane.transform.position.y - run.FloorAt(plane.transform.position);
            speed.text = $"{kmh:0000}"; altitude.text = $"{agl:000}";
            mach.text = $"KM/H   M {plane.Mach:0.00}";
            power.text = $"THR  {plane.Throttle*100:000}%" + (plane.Afterburner ? "  AB" : "");
            powerBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 180 * plane.Throttle);
            UpdateTape(speedTicks, kmh, 50); UpdateTape(altitudeTicks, agl, 25);
            float margin = run.Course.CeilingAt(plane.transform.position) - plane.HighestPoint;
            bool incoming=run.Defense.Incoming>0;
            warning.color=incoming?new Color(1,.38f,.15f):Accent;
            warning.text=incoming?$"MISSILE  {run.Defense.NearestMissileDistance:0} M\nEVADIR / FLARES":run.IsFreeFlight?"GRANITE RANGE\nVOO LIVRE":run.Defense.Acquiring?"RADAR LOCK\nDESCEND":margin<60?"ALTITUDE\nDESCEND":$"RADAR  {run.Course.ceiling:0} M";
            threats.text="";
            for(int i=0;i<run.Defense.Incoming;i++)
            {
                if(!run.Defense.TryGetMissilePosition(i,out var missilePosition))continue;
                Vector3 relative=plane.transform.InverseTransformDirection(missilePosition-plane.Body.position);
                float range=relative.magnitude;
                string side=Mathf.Abs(relative.z)>=Mathf.Abs(relative.x)?(relative.z>=0?"FRENTE":"ATRÁS"):(relative.x>=0?"DIREITA":"ESQUERDA");
                threats.text+=$"SAM {i+1}  {range:0} M  {side}\n";
            }
            distance.text=run.IsFreeFlight?"":$"RESTAM  {run.RemainingDistance/1000:0.0} KM";
            countdown.text=run.State==RunState.Countdown?$"READY  {Mathf.CeilToInt(run.Countdown)}":run.TurnNotice?"ARCO CRUZADO / RETORNE À BASE":"";
            objective.text=run.IsFreeFlight?"TREINO / SEM RESTRIÇÕES":run.Returning?"02 / RETORNO À BASE":"01 / IDA AO MARCO";
            Vector3 destination=run.Course.route[Mathf.Clamp(run.NextSector,0,run.Course.route.Length-1)]-plane.transform.position;
            float headingDegrees=Mathf.Repeat(Mathf.Atan2(destination.x,destination.z)*Mathf.Rad2Deg,360);
            bearing.text=run.IsFreeFlight?"":$"RUMO AO PERCURSO  {headingDegrees:000}°";
            flares.text=$"FLARES  {run.Flares:00} / 10";
            clock.text=run.IsFreeFlight?"":$"TIME  {RecordStore.Format(run.Elapsed)}";

        }
        void Update()
        {
            if(controlsText!=null && lastPad!=(Gamepad.current!=null))UpdateBindings();
        }
        void UpdateBindings()
        {
            lastPad=Gamepad.current!=null;
            navigationHint.text=lastPad?"D-PAD / ANALÓGICO: SELECIONAR     X / A: CONFIRMAR":"SETAS: SELECIONAR     ENTER: CONFIRMAR     MOUSE: CLICAR";
            controlsText.text=lastPad
                ? "L STICK     Pitch / roll         R STICK     Câmera\nR2 / L2      Potência              R1 / L1        Yaw\nX / A          Airbrakes             CÍRCULO / B    Flare\nOPTIONS / START     Pausa\nCâmera retorna após 10 s sem mover o analógico."
                : "W / S     Pitch       A / D     Roll       Q / E     Yaw\nSHIFT / CTRL     Potência         X     Airbrakes\nF     Flare        SETAS     Câmera        ESC     Pausa\nControle compatível: conecte e jogue.\nR2/L2 potência · X/A freio · círculo/B flare";
        }
        void Preview(bool free)
        {
            if(run.State!=RunState.Menu)return;
            map.Configure(run.Course,free);
            mapTitle.text=free?"02 / GRANITE RANGE":"01 / OPERAÇÃO CÂNION";
            mapDetail.text=free?"20 × 20 KM   /   MONTANHAS + ARCOS + OBSTÁCULOS\nVoo aberto. Mesma física. Sem radar nem cronômetro."
                : $"{run.Course.TotalDistance*2/1000:0.0} KM   /   IDA + RETORNO   /   10 FLARES\nSAMs em todo o percurso acima de {run.Course.ceiling:0} M. Use o relevo como proteção.";
        }
        Text Gauge(string title, float x, float y, Text[] ticks)
        {
            Label(hud, title, title, x, y-45, 220, 30, 21, Accent);
            Panel(hud, title+" spine", x+105, y, 2, 360, Accent);
            var viewport = Rect(hud, title + " tape viewport", x+105, y, 130, 360);
            viewport.gameObject.AddComponent<RectMask2D>();
            for (int i=0;i<ticks.Length;i++)
            {
                var row = Rect(viewport, "Moving graduation", 0, 0, 130, 26);
                Panel(row, "Scale tick", 0, 12, 24, 2, Accent);
                Panel(row, "Half tick", 0, 32, 12, 1, Accent);
                ticks[i]=Label(row, "Scale value", "", 35, 0, 95, 26, 18, Accent);
                ticks[i].alignment = TextAnchor.MiddleLeft;
            }
            Panel(hud,"Readout top",x,y+157,100,2,Accent);
            Panel(hud,"Readout bottom",x,y+203,100,2,Accent);
            Panel(hud,"Readout end",x,y+157,2,46,Accent);
            Panel(hud,"Fixed index",x+98,y+179,13,2,Accent);
            return Label(hud,"Readout","",x+5,y+162,98,36,26,Accent);
        }
        void UpdateTape(Text[] ticks, float value, int step)
        {
            for(int i=0;i<ticks.Length;i++)
            {
                float tick = HudTape.TickValue(value, step, i);
                var row = (RectTransform)ticks[i].transform.parent;
                row.gameObject.SetActive(tick >= 0);
                row.anchoredPosition = new Vector2(0, -(HudTape.TickY(value, tick, step) - 13));
                ticks[i].text = tick.ToString("0");
            }
        }

        RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height); return r;
        }
        RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var r = Rect(parent, name, x, y, w, h); var image = r.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return r;
        }
        Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size, Color color)
        {
            var r = Rect(parent, name, x, y, w, h); var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.text = value; t.fontSize = size; t.color = color; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; return t;
        }
        Button Button(Transform parent, string name, float x, float y, out Text text)
        {
            var p = Panel(parent, name, x, y, 580, 72, name == "Primary" ? new Color(.19f,.24f,.23f) : new Color(.06f,.10f,.11f));
            Panel(p,"Selection rule",0,0,3,72,MenuAccent);
            p.GetComponent<Image>().raycastTarget = true;
            var button = p.gameObject.AddComponent<Button>(); button.targetGraphic = p.GetComponent<Image>();
            p.GetComponent<Image>().color=Color.white;
            var colors = button.colors; colors.normalColor=new Color(.055f,.09f,.10f);colors.highlightedColor=new Color(.17f,.26f,.25f);
            colors.selectedColor=new Color(.31f,.25f,.15f);colors.pressedColor=new Color(.44f,.35f,.19f);colors.fadeDuration=.12f;button.colors = colors;
            text = Label(p, "Text", "", 20, 0, 490, 72, 19, Color.white); text.alignment = TextAnchor.MiddleLeft;
            var arrow=Label(p,"Action arrow","→",530,0,30,72,22,MenuAccent);arrow.alignment=TextAnchor.MiddleCenter;
            return button;
        }
    }
}
