using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SkyRacers
{
    public sealed class GameUI : MonoBehaviour
    {
        static readonly Color Ink = new Color(.035f, .065f, .085f, .94f);
        static readonly Color Accent = new Color(.22f, 1f, .32f);
        static readonly Color Muted = new Color(.67f, .74f, .74f);
        RunManager run;
        Font font;
        RectTransform root, overlay, card, hud;
        Text heading, subtitle, speed, power, altitude, warning, countdown, mach, distance;
        Image powerBar;
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
            if (EventSystem.current == null) new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));

            hud = Panel(root, "Flight HUD", 0, 0, 1600, 900, Color.clear);
            // Flight instruments only, transparent neon-green strokes; centre stays empty.
            speed = Gauge("IAS", 80, 220, speedTicks);
            altitude = Gauge("ALT AGL", 1340, 220, altitudeTicks);
            mach = Label(hud, "Mach", "", 80, 650, 210, 30, 19, Accent);
            power = Label(hud, "Power", "", 80, 710, 240, 35, 23, Accent);
            Panel(hud, "Engine scale", 80, 752, 180, 1, Accent);
            powerBar = Panel(hud, "Engine thrust", 80, 747, 100, 5, Accent).GetComponent<Image>();
            warning = Label(hud, "Altitude caution", "", 1280, 660, 285, 60, 19, Accent);
            distance = Label(hud, "Distance remaining", "", 1280, 745, 300, 35, 22, Accent);
            countdown = Label(hud, "Preparation", "", 80, 170, 250, 35, 21, Accent);

            overlay = Panel(root, "Menus", 0, 0, 1600, 900, new Color(.02f, .035f, .05f, .2f));
            card = Panel(overlay, "Menu card", 64, 72, 560, 756, Ink);
            Panel(card, "Accent", 32, 32, 60, 4, Accent);
            Label(card, "Eyebrow", "LOW ALTITUDE / TIME ATTACK", 32, 54, 490, 26, 16, Accent);
            heading = Label(card, "Title", "SKY\nRACERS", 28, 100, 500, 165, 70, Color.white);
            heading.fontStyle = FontStyle.Bold;
            subtitle = Label(card, "Description", "", 32, 287, 492, 105, 21, Muted);
            primary = Button(card, "Primary", 32, 438, out primaryText);
            secondary = Button(card, "Secondary", 32, 518, out secondaryText);
            tertiary = Button(card, "Tertiary", 32, 598, out tertiaryText);
            Label(card, "Footer", "01 FASE   /   TIME ATTACK + VOO LIVRE", 32, 704, 490, 24, 14, Muted);
            var controls = Panel(overlay, "Controls", 1050, 512, 486, 316, Ink);
            Label(controls, "Title", "MANUAL DE VOO", 26, 22, 430, 28, 19, Accent);
            Label(controls, "Keys", "SHIFT / CTRL     Potência dos motores\nA / D                   Roll\nS / W                  Subir / descer o nariz\nQ / E                   Yaw\nX                         Airbrakes (segurar)\nESC                    Pausar", 26, 69, 440, 208, 19, Color.white);
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
            primaryText.text = pause ? "CONTINUAR VOO  →" : menu ? "CANYON 01   /   VOAR  →" : "TENTAR NOVAMENTE  →";
            secondaryText.text = pause ? "REINICIAR VOO" : menu ? "VOO LIVRE   /   TESTAR FÍSICA  →" : "MENU PRINCIPAL";
            tertiary.gameObject.SetActive(pause || menu); tertiaryText.text = menu ? "SAIR" : "MENU PRINCIPAL";
            heading.text = menu ? "SKY\nRACERS" : pause ? "VOO\nPAUSADO" : run.State == RunState.Won ? "MISSÃO\nCUMPRIDA" : "FIM DA\nRUN";
            if (menu) subtitle.text = "Corra contra o relógio dentro do cânion.\n\nOu teste o caça em voo livre, sem teto de altitude nem percurso obrigatório.";
            else if (pause) subtitle.text = run.IsFreeFlight ? "VOO LIVRE\n\nExplore o cenário sem limite de altitude. As colisões continuam ativas." : "A física e o cronômetro estão pausados.\n\nContinue ou tente uma nova trajetória.";
            else if (run.State == RunState.Won) subtitle.text = (run.NewRecord ? "NOVO RECORDE!\n" : "PERCURSO CONCLUÍDO\n") + RecordStore.Format(run.Elapsed) + "\nMelhor: " + RecordStore.Format(RecordStore.Best);
            else subtitle.text = run.Reason + (run.IsFreeFlight ? "\n\nVOO LIVRE / Reinicie para continuar testando." : "\n\nTempo: " + RecordStore.Format(run.Elapsed));
            if (overlay.gameObject.activeSelf && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(primary.gameObject);
        }
        public void UpdateTelemetry()
        {
            if (run.Aircraft == null) return;
            var plane = run.Aircraft;
            float kmh = plane.Speed * Mathf.Sqrt(FlightDynamics.Density(plane.transform.position.y) / 1.225f) * 3.6f;
            float agl = plane.transform.position.y - run.Course.FloorAt(plane.transform.position.z);
            speed.text = $"{kmh:0000}"; altitude.text = $"{agl:000}";
            mach.text = $"KM/H   M {plane.Mach:0.00}";
            power.text = $"THR  {plane.Throttle*100:000}%" + (plane.Afterburner ? "  AB" : "");
            powerBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 180 * plane.Throttle);
            UpdateTape(speedTicks, kmh, 50); UpdateTape(altitudeTicks, agl, 25);
            float margin = run.Course.CeilingAt(plane.transform.position) - plane.HighestPoint;
            warning.text = run.IsFreeFlight ? "VOO LIVRE\nSEM LIMITE" : margin < 60 ? (Mathf.Repeat(Time.unscaledTime, .6f) < .4f ? "ALTITUDE\nDESCEND" : "") : $"LIMIT {run.Course.ceiling:0} M";
            distance.text = run.IsFreeFlight ? "" : $"DIST  {run.RemainingDistance / 1000:0.0} KM";
            countdown.text = run.State == RunState.Countdown ? $"READY  {Mathf.CeilToInt(run.Countdown)}" : "";
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
            var p = Panel(parent, name, x, y, 496, 64, name == "Primary" ? Accent : new Color(.14f, .19f, .21f));
            p.GetComponent<Image>().raycastTarget = true;
            var button = p.gameObject.AddComponent<Button>(); button.targetGraphic = p.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(.85f, .88f, .84f); colors.pressedColor = new Color(.65f, .72f, .6f); button.colors = colors;
            text = Label(p, "Text", "", 20, 0, 456, 64, 20, name == "Primary" ? Ink : Color.white); text.alignment = TextAnchor.MiddleLeft;
            return button;
        }
    }
}
