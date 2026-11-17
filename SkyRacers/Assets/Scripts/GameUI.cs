using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SkyRacers
{
    public sealed class GameUI : MonoBehaviour
    {
        static readonly Color Ink = new Color(.035f, .065f, .085f, .94f);
        static readonly Color Accent = new Color(.85f, .96f, .35f);
        static readonly Color Muted = new Color(.67f, .74f, .74f);
        RunManager run;
        Font font;
        RectTransform root, overlay, card, hud;
        Text heading, subtitle, record, timer, speed, power, altitude, warning, countdown, progress;
        Image powerBar, routeBar;
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
            var tl = Panel(hud, "Timer panel", 40, 32, 300, 104, Ink);
            Label(tl, "Label", "CANYON 01  /  TEMPO", 20, 14, 260, 20, 14, Accent);
            timer = Label(tl, "Timer", "00:00.000", 20, 37, 270, 52, 38, Color.white);
            var tr = Panel(hud, "Record panel", 1270, 32, 290, 92, Ink);
            Label(tr, "Label", "MELHOR TEMPO", 20, 14, 250, 20, 14, Muted);
            record = Label(tr, "Best", "--:--.---", 20, 39, 250, 38, 28, Color.white);
            var bottom = Panel(hud, "Aircraft telemetry", 40, 724, 460, 136, Ink);
            speed = Label(bottom, "Speed", "342", 20, 12, 180, 58, 45, Color.white);
            Label(bottom, "Units", "KM/H", 23, 74, 130, 22, 14, Muted);
            power = Label(bottom, "Power", "POTÊNCIA  62%", 200, 23, 235, 26, 18, Accent);
            Panel(bottom, "Power rail", 200, 62, 230, 5, new Color(.2f, .25f, .27f));
            powerBar = Panel(bottom, "Power bar", 200, 62, 142, 5, Accent).GetComponent<Image>();
            altitude = Label(bottom, "Ceiling", "MARGEM ATÉ O TETO  80 m", 200, 85, 245, 26, 15, Color.white);
            var br = Panel(hud, "Progress", 1150, 770, 410, 90, Ink);
            progress = Label(br, "Route", "PERCURSO  0%", 20, 12, 370, 25, 16, Color.white);
            Panel(br, "Progress rail", 20, 53, 370, 5, new Color(.2f, .25f, .27f));
            routeBar = Panel(br, "Progress bar", 20, 53, 1, 5, Accent).GetComponent<Image>();
            Label(hud, "Pause hint", "ESC  /  PAUSAR", 1330, 142, 230, 25, 15, Color.white).alignment = TextAnchor.MiddleRight;
            warning = Label(hud, "Warning", "", 500, 130, 600, 50, 24, Accent); warning.alignment = TextAnchor.MiddleCenter;
            countdown = Label(hud, "Countdown", "", 650, 300, 300, 200, 106, Color.white); countdown.alignment = TextAnchor.MiddleCenter;
            Label(hud, "Reticle", "+", 780, 431, 40, 38, 27, new Color(1, 1, 1, .4f)).alignment = TextAnchor.MiddleCenter;

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
            Label(card, "Footer", "01 FASE   /   SEM SEGUNDA CHANCE", 32, 704, 490, 24, 14, Muted);
            var controls = Panel(overlay, "Controls", 1050, 512, 486, 316, Ink);
            Label(controls, "Title", "MANUAL DE VOO", 26, 22, 430, 28, 19, Accent);
            Label(controls, "Keys", "SHIFT / CTRL     Potência dos motores\nA / D                   Roll\nS / W                  Subir / descer o nariz\nQ / E                   Yaw\nX                         Airbrakes (segurar)\nESC                    Pausar", 26, 69, 440, 208, 19, Color.white);
            primary.onClick.AddListener(() => { if (run.State == RunState.Paused) run.Resume(); else run.StartRun(); });
            secondary.onClick.AddListener(() => { if (run.State == RunState.Paused) run.StartRun(); else if (run.State == RunState.Menu) run.Quit(); else run.MainMenu(); });
            tertiary.onClick.AddListener(run.MainMenu);
        }
        public void RefreshState()
        {
            if (overlay == null) return;
            bool menu = run.State == RunState.Menu, pause = run.State == RunState.Paused;
            bool ended = run.State == RunState.Failed || run.State == RunState.Won;
            overlay.gameObject.SetActive(menu || pause || ended);
            hud.gameObject.SetActive(!menu && !ended);
            primaryText.text = pause ? "CONTINUAR VOO  →" : menu ? "CANYON 01   /   VOAR  →" : "TENTAR NOVAMENTE  →";
            secondaryText.text = pause ? "REINICIAR RUN" : menu ? "SAIR" : "MENU PRINCIPAL";
            tertiary.gameObject.SetActive(pause); tertiaryText.text = "MENU PRINCIPAL";
            heading.text = menu ? "SKY\nRACERS" : pause ? "VOO\nPAUSADO" : run.State == RunState.Won ? "MISSÃO\nCUMPRIDA" : "FIM DA\nRUN";
            if (menu) subtitle.text = "Um caça. Um cânion. O seu melhor tempo.\n\nFique abaixo das bordas e alcance a chegada.";
            else if (pause) subtitle.text = "A física e o cronômetro estão pausados.\n\nContinue ou tente uma nova trajetória.";
            else if (run.State == RunState.Won) subtitle.text = (run.NewRecord ? "NOVO RECORDE!\n" : "PERCURSO CONCLUÍDO\n") + RecordStore.Format(run.Elapsed) + "\nMelhor: " + RecordStore.Format(RecordStore.Best);
            else subtitle.text = run.Reason + "\n\nTempo: " + RecordStore.Format(run.Elapsed);
            if (overlay.gameObject.activeSelf && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(primary.gameObject);
        }
        public void UpdateTelemetry()
        {
            if (run.Aircraft == null) return;
            var plane = run.Aircraft;
            timer.text = RecordStore.Format(run.Elapsed);
            record.text = RecordStore.Best > 0 ? RecordStore.Format(RecordStore.Best) : "--:--.---";
            speed.text = Mathf.RoundToInt(plane.Speed * 3.6f).ToString();
            power.text = $"POTÊNCIA  {plane.Throttle * 100:0}%";
            powerBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 230 * plane.Throttle);
            float margin = run.Course.ceiling - Mathf.Max(plane.transform.position.y + 3, plane.HighestPoint);
            altitude.text = $"MARGEM ATÉ O TETO  {Mathf.Max(0, margin):0} m";
            progress.text = $"PERCURSO  {run.Progress * 100:0}%";
            routeBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 370 * run.Progress);
            warning.text = margin < 25 ? "↓  ALTITUDE ALTA — DESÇA" : plane.Speed < 52 && run.State == RunState.Running ? "BAIXA VELOCIDADE — ACELERE" : plane.Airbrake > .1f ? "AIRBRAKES ABERTOS" : "";
            countdown.text = run.State == RunState.Countdown ? Mathf.CeilToInt(run.Countdown).ToString() : "";
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
