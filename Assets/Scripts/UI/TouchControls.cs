using UnityEngine;
using UnityEngine.InputSystem;

// Mobil (Android/iOS) çoklu dokunmatik kontroller: Sol altta sanal Joystick, sağ altta aksiyon butonları.
// PC'de 'T' tuşuyla test için açılıp kapatılabilir.
public class TouchControls : MonoBehaviour
{
    public static bool Visible;
    public static float MoveX, MoveY;
    public static bool JumpPressed, JumpHeld, DashPressed, SlashPressed, GunPressed, InteractPressed;

    Texture2D circleBaseTex, circleKnobTex, btnTex, btnActiveTex;

    // Sanal Joystick
    Vector2 joyCenter, joyKnob;
    float joyRadius, knobRadius;
    bool joyActive;
    int joyPointerId = -1; // -2 = fare

    struct PadBtn
    {
        public string label;
        public Rect rect;
        public bool held;
        public bool wasHeld;
        public bool down => held && !wasHeld;
    }

    PadBtn bJump, bDash, bSlash, bGun, bUse, bPause;

    void Awake()
    {
        Visible = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
        circleBaseTex = MakeCircleTex(128, new Color(0.12f, 0.15f, 0.24f, 0.50f), new Color(1f, 1f, 1f, 0.25f));
        circleKnobTex = MakeCircleTex(96, new Color(0.35f, 0.70f, 1f, 0.80f), new Color(1f, 1f, 1f, 0.55f));
        btnTex = MakeCircleTex(96, new Color(0.12f, 0.14f, 0.22f, 0.58f), new Color(1f, 1f, 1f, 0.25f));
        btnActiveTex = MakeCircleTex(96, new Color(0.35f, 0.65f, 1f, 0.82f), new Color(1f, 1f, 1f, 0.6f));
    }

    static Texture2D MakeCircleTex(int size, Color fill, Color border)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        float r2 = (r - 1f) * (r - 1f);
        float inner2 = (r - 4f) * (r - 4f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                float d2 = dx * dx + dy * dy;
                if (d2 > r2) t.SetPixel(x, y, Color.clear);
                else if (d2 > inner2) t.SetPixel(x, y, border);
                else t.SetPixel(x, y, fill);
            }
        }
        t.Apply();
        return t;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            Visible = !Visible;

        if (!Visible && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            Visible = true;

        JumpPressed = DashPressed = SlashPressed = GunPressed = InteractPressed = false;
        JumpHeld = false;
        MoveX = MoveY = 0f;

        if (!Visible || GameBootstrap.Instance == null || GameBootstrap.Instance.state != GameBootstrap.GameState.Playing)
        {
            joyActive = false;
            joyPointerId = -1;
            return;
        }

        ComputeLayout();
        ReadPointers();

        if (joyActive)
        {
            Vector2 delta = joyKnob - joyCenter;
            float normX = Mathf.Clamp(delta.x / joyRadius, -1f, 1f);
            float normY = Mathf.Clamp(-delta.y / joyRadius, -1f, 1f); // GUI y aşağı artar, oyun y yukarı
            MoveX = Mathf.Abs(normX) > 0.15f ? normX : 0f;
            MoveY = Mathf.Abs(normY) > 0.20f ? normY : 0f;
        }

        JumpPressed = bJump.down;
        JumpHeld = bJump.held;
        DashPressed = bDash.down;
        SlashPressed = bSlash.down;
        GunPressed = bGun.down;
        InteractPressed = bUse.down;

        if (bPause.down) GameBootstrap.Instance.TogglePause();
    }

    void ComputeLayout()
    {
        float u = Mathf.Clamp(Screen.height * 0.16f, 76f, 200f);
        float gap = u * 0.22f;
        float pad = u * 0.35f;

        // Sol alt: Büyük, rahat sanal Joystick
        joyRadius = u * 0.95f;
        knobRadius = u * 0.42f;
        joyCenter = new Vector2(pad + joyRadius + u * 0.15f, Screen.height - pad - joyRadius - u * 0.1f);
        if (!joyActive) joyKnob = joyCenter;

        // Sağ alt: Yuvarlak aksiyon butonları
        float bottom = Screen.height - pad - u;
        float rx = Screen.width - pad - u;
        bJump.label = "ZIPLA";    bJump.rect  = new Rect(rx, bottom, u, u);
        bSlash.label = "KILIÇ";   bSlash.rect = new Rect(rx - u - gap, bottom, u, u);
        bDash.label = "DASH";     bDash.rect  = new Rect(rx, bottom - u - gap, u, u);
        bGun.label = "TABANCA";   bGun.rect   = new Rect(rx - u - gap, bottom - u - gap, u, u);
        bUse.label = "AÇ";        bUse.rect   = new Rect(rx - (u + gap) * 2f, bottom + u * 0.1f, u * 0.86f, u * 0.86f);

        // Üst orta: Duraklat butonu (minimap'ten uzak)
        float pw = Mathf.Clamp(Screen.height * 0.095f, 52f, 88f);
        bPause.label = "||";      bPause.rect = new Rect(Screen.width * 0.5f - pw - 6f, 10f, pw, pw * 0.72f);
    }

    void ReadPointers()
    {
        bJump.wasHeld = bJump.held; bDash.wasHeld = bDash.held; bSlash.wasHeld = bSlash.held;
        bGun.wasHeld = bGun.held; bUse.wasHeld = bUse.held; bPause.wasHeld = bPause.held;
        bJump.held = bDash.held = bSlash.held = bGun.held = bUse.held = bPause.held = false;

        bool joyStillDown = false;
        float joyZoneRadius = joyRadius * 1.65f;

        var ts = Touchscreen.current;
        if (ts != null)
        {
            foreach (var touch in ts.touches)
            {
                if (!touch.press.isPressed) continue;
                int id = touch.touchId.ReadValue();
                Vector2 p = touch.position.ReadValue();
                Vector2 gp = new Vector2(p.x, Screen.height - p.y);

                if (joyActive && id == joyPointerId)
                {
                    joyStillDown = true;
                    UpdateKnob(gp);
                    continue;
                }
                if (!joyActive && gp.x < Screen.width * 0.45f && Vector2.Distance(gp, joyCenter) <= joyZoneRadius)
                {
                    joyActive = true;
                    joyPointerId = id;
                    joyStillDown = true;
                    UpdateKnob(gp);
                    continue;
                }
                HitButtons(gp);
            }
        }

        // PC'de fareyle test desteği
        var m = Mouse.current;
        if (m != null && m.leftButton.isPressed)
        {
            Vector2 p = m.position.ReadValue();
            Vector2 gp = new Vector2(p.x, Screen.height - p.y);
            if (joyActive && joyPointerId == -2)
            {
                joyStillDown = true;
                UpdateKnob(gp);
            }
            else if (!joyActive && gp.x < Screen.width * 0.45f && Vector2.Distance(gp, joyCenter) <= joyZoneRadius)
            {
                joyActive = true;
                joyPointerId = -2;
                joyStillDown = true;
                UpdateKnob(gp);
            }
            else
            {
                HitButtons(gp);
            }
        }

        if (!joyStillDown)
        {
            joyActive = false;
            joyPointerId = -1;
            joyKnob = joyCenter;
        }
    }

    void UpdateKnob(Vector2 guiPos)
    {
        Vector2 offset = guiPos - joyCenter;
        if (offset.magnitude > joyRadius) offset = offset.normalized * joyRadius;
        joyKnob = joyCenter + offset;
    }

    void HitButtons(Vector2 p)
    {
        if (bJump.rect.Contains(p)) bJump.held = true;
        if (bDash.rect.Contains(p)) bDash.held = true;
        if (bSlash.rect.Contains(p)) bSlash.held = true;
        if (bGun.rect.Contains(p)) bGun.held = true;
        if (bUse.rect.Contains(p)) bUse.held = true;
        if (bPause.rect.Contains(p)) bPause.held = true;
    }

    void OnGUI()
    {
        if (!Visible || GameBootstrap.Instance == null || GameBootstrap.Instance.state != GameBootstrap.GameState.Playing)
            return;

        ComputeLayout();

        // Joystick tabanı ve topuzu
        GUI.DrawTexture(new Rect(joyCenter.x - joyRadius, joyCenter.y - joyRadius, joyRadius * 2f, joyRadius * 2f), circleBaseTex);
        GUI.DrawTexture(new Rect(joyKnob.x - knobRadius, joyKnob.y - knobRadius, knobRadius * 2f, knobRadius * 2f), circleKnobTex);

        var st = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp((int)(Screen.height * 0.030f), 15, 34),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        st.normal.textColor = new Color(1f, 1f, 1f, 0.94f);

        DrawBtn(bJump, st); DrawBtn(bDash, st); DrawBtn(bSlash, st); DrawBtn(bGun, st); DrawBtn(bUse, st);
        DrawBtn(bPause, st);
    }

    void DrawBtn(PadBtn b, GUIStyle st)
    {
        GUI.DrawTexture(b.rect, b.held ? btnActiveTex : btnTex);
        GUI.Label(b.rect, b.label, st);
    }
}
