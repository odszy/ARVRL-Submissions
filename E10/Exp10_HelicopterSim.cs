using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

// EXP 10 - VR helicopter flying simulation with a generated city, trees, mountains and clouds.
// Attach to an empty GameObject.
// VR: drag your XR Origin into "xrRig" - you will sit inside the helicopter.
//     Left stick = forward/back + turn, right stick up/down = climb/descend.
// Desktop (no XR): W/S forward-back, A/D turn, Space climb, Left Shift / Ctrl descend, V = change view.
public class Exp10_HelicopterSim : MonoBehaviour
{
    public Transform xrRig;
    public float maxSpeed = 30f, climbSpeed = 8f, turnSpeed = 55f;

    Transform root, model, mainRotor, tailRotor, camT;
    readonly List<Transform> clouds = new List<Transform>();
    TextMesh dash;
    float speed, vy, yawRate, rpm, flash;
    bool cockpit = true;
    string hud = "";

    void Start()
    {
        Random.InitState(7);
        Color sky = new Color(0.55f, 0.75f, 0.95f);
        Kit.Sky(sky); Kit.Light(1.2f);
        RenderSettings.fog = true; RenderSettings.fogColor = sky;
        RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.004f;
        camT = Kit.Cam().transform;
        camT.GetComponent<Camera>().farClipPlane = 1500f;

        BuildWorld();
        BuildHeli();

        if (xrRig != null)
        {
            xrRig.SetParent(root, false);
            xrRig.localPosition = new Vector3(0, 0.25f, 0.35f);
            xrRig.localRotation = Quaternion.identity;
            dash = Kit.Text("", new Vector3(0, 0.2f, 1.1f), 0.01f, new Color(0.3f, 1f, 0.4f), root);
        }
    }

    // ---------------- world ----------------
    bool InRoad(Vector3 p) { return p.x > 22f && p.x < 38f; }

    void BuildWorld()
    {
        Kit.Prim(PrimitiveType.Cube, new Vector3(0, -0.5f, 0), new Vector3(1200, 1, 1200),
                 Kit.Mat(new Color(0.25f, 0.55f, 0.25f), 0f, 0.1f), null, false);
        // road + landing pad with H
        Kit.Prim(PrimitiveType.Cube, new Vector3(30, 0.02f, 0), new Vector3(10, 0.04f, 1000),
                 Kit.Mat(new Color(0.15f, 0.15f, 0.17f), 0f, 0.2f), null, false);
        Kit.Prim(PrimitiveType.Cube, new Vector3(30, 0.05f, 0), new Vector3(0.3f, 0.02f, 1000),
                 Kit.Mat(Color.yellow, 0.5f), null, false);
        Kit.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.02f, 0), new Vector3(12, 0.02f, 12),
                 Kit.Mat(new Color(0.35f, 0.35f, 0.4f), 0f, 0.3f), null, false);
        var white = Kit.Mat(Color.white, 0.8f);
        Kit.Prim(PrimitiveType.Cube, new Vector3(-1.5f, 0.06f, 0), new Vector3(0.6f, 0.02f, 4f), white, null, false);
        Kit.Prim(PrimitiveType.Cube, new Vector3(1.5f, 0.06f, 0), new Vector3(0.6f, 0.02f, 4f), white, null, false);
        Kit.Prim(PrimitiveType.Cube, new Vector3(0, 0.06f, 0), new Vector3(3f, 0.02f, 0.6f), white, null, false);

        // buildings
        var palette = new Material[5];
        for (int i = 0; i < palette.Length; i++)
            palette[i] = Kit.Mat(Color.HSVToRGB(0.55f + i * 0.04f, 0.25f, 0.55f + i * 0.08f), 0f, 0.6f);
        var windows = Kit.Mat(new Color(1f, 0.85f, 0.5f), 1.2f);
        for (int i = 0; i < 80; i++)
        {
            Vector3 p = new Vector3(Random.Range(-250f, 250f), 0, Random.Range(-250f, 250f));
            if (p.magnitude < 25f || InRoad(p)) continue;
            float w = Random.Range(8f, 18f), d = Random.Range(8f, 18f), h = Random.Range(8f, 50f);
            Kit.Prim(PrimitiveType.Cube, new Vector3(p.x, h / 2f, p.z), new Vector3(w, h, d), palette[Random.Range(0, palette.Length)]);
            for (float y = 4f; y < h - 2f; y += 4f)
                Kit.Prim(PrimitiveType.Cube, new Vector3(p.x, y, p.z), new Vector3(w * 1.01f, 0.5f, d * 1.01f), windows, null, false);
        }

        // trees
        var trunk = Kit.Mat(new Color(0.4f, 0.25f, 0.12f), 0f, 0.1f);
        var leaf = Kit.Mat(new Color(0.12f, 0.45f, 0.15f), 0f, 0.2f);
        for (int i = 0; i < 140; i++)
        {
            Vector3 p = new Vector3(Random.Range(-280f, 280f), 0, Random.Range(-280f, 280f));
            if (p.magnitude < 20f || InRoad(p)) continue;
            float s = Random.Range(0.8f, 1.8f);
            Kit.Prim(PrimitiveType.Cylinder, new Vector3(p.x, 1.5f * s, p.z), new Vector3(0.5f * s, 1.5f * s, 0.5f * s), trunk);
            Kit.Prim(PrimitiveType.Sphere, new Vector3(p.x, 4.2f * s, p.z), Vector3.one * 3.5f * s, leaf, null, false);
        }

        // mountains
        for (int i = 0; i < 14; i++)
        {
            float a = i / 14f * Mathf.PI * 2f + Random.Range(-0.1f, 0.1f);
            float r = Random.Range(420f, 520f);
            Kit.Prim(PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * r, -20f, Mathf.Sin(a) * r),
                     new Vector3(Random.Range(150f, 220f), Random.Range(160f, 320f), Random.Range(150f, 220f)),
                     Kit.Mat(Color.Lerp(new Color(0.4f, 0.45f, 0.5f), Color.white, Random.value * 0.5f), 0f, 0.1f), null, false);
        }

        // clouds
        var cm = Kit.Mat(Color.white, 0.35f, 0.1f);
        for (int i = 0; i < 25; i++)
        {
            var c = new GameObject("Cloud").transform;
            c.position = new Vector3(Random.Range(-400f, 400f), Random.Range(80f, 130f), Random.Range(-400f, 400f));
            for (int k = 0; k < 4; k++)
                Kit.Prim(PrimitiveType.Sphere, new Vector3(k * 12f - 18f, Random.Range(-2f, 2f), Random.Range(-4f, 4f)),
                         new Vector3(Random.Range(18f, 28f), Random.Range(8f, 12f), Random.Range(14f, 20f)), cm, c, false);
            clouds.Add(c);
        }
    }

    // ---------------- helicopter ----------------
    void BuildHeli()
    {
        root = new GameObject("Helicopter").transform;
        root.position = new Vector3(0, 1f, 0);
        model = new GameObject("Model").transform;
        model.SetParent(root, false);

        var red = Kit.Mat(new Color(0.85f, 0.1f, 0.1f), 0f, 0.9f);
        var dark = Kit.Mat(new Color(0.1f, 0.1f, 0.12f), 0f, 0.5f);
        var glass = Kit.Mat(new Color(0.5f, 0.85f, 1f), 0.3f, 1f);

        Kit.Prim(PrimitiveType.Sphere, Vector3.zero, new Vector3(1.8f, 1.6f, 2.8f), red, model, false);
        Kit.Prim(PrimitiveType.Sphere, new Vector3(0, 0.25f, 0.7f), new Vector3(1.5f, 1.1f, 1.4f), glass, model, false);
        Kit.Prim(PrimitiveType.Cube, new Vector3(0, 0.1f, -2.4f), new Vector3(0.3f, 0.3f, 3.2f), red, model, false);
        Kit.Prim(PrimitiveType.Cube, new Vector3(0, 0.6f, -3.9f), new Vector3(0.12f, 1.0f, 0.5f), red, model, false);
        Kit.Prim(PrimitiveType.Cylinder, new Vector3(0, 1.0f, 0), new Vector3(0.12f, 0.3f, 0.12f), dark, model, false);
        foreach (float sx in new[] { -0.8f, 0.8f })
        {
            Kit.Prim(PrimitiveType.Cube, new Vector3(sx, -0.95f, 0), new Vector3(0.08f, 0.08f, 2.6f), dark, model, false);
            foreach (float sz in new[] { -0.7f, 0.7f })
                Kit.Prim(PrimitiveType.Cube, new Vector3(sx * 0.9f, -0.65f, sz), new Vector3(0.06f, 0.6f, 0.06f), dark, model, false);
        }
        mainRotor = new GameObject("MainRotor").transform;
        mainRotor.SetParent(model, false); mainRotor.localPosition = new Vector3(0, 1.35f, 0);
        Kit.Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(9f, 0.04f, 0.35f), dark, mainRotor, false);
        Kit.Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(0.35f, 0.04f, 9f), dark, mainRotor, false);
        tailRotor = new GameObject("TailRotor").transform;
        tailRotor.SetParent(model, false); tailRotor.localPosition = new Vector3(0.2f, 0.6f, -3.9f);
        Kit.Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(0.04f, 1.6f, 0.12f), dark, tailRotor, false);
        Kit.Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(0.04f, 0.12f, 1.6f), dark, tailRotor, false);
        Kit.Prim(PrimitiveType.Sphere, new Vector3(0, 1.1f, -4.1f), Vector3.one * 0.2f,
                 Kit.Mat(Color.red, 3f), model, false);

        // searchlight
        var lg = new GameObject("Searchlight");
        lg.transform.SetParent(model, false);
        lg.transform.localPosition = new Vector3(0, -0.3f, 1.2f);
        lg.transform.localRotation = Quaternion.Euler(25, 0, 0);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Spot; l.range = 60f; l.spotAngle = 45f; l.intensity = 2f;
    }

    // ---------------- flight ----------------
    void Update()
    {
        float dt = Time.deltaTime;
        Vector2 L = Kit.Stick(XRNode.LeftHand), R = Kit.Stick(XRNode.RightHand);

        float fwd = Mathf.Clamp(Input.GetAxis("Vertical") + L.y, -1f, 1f);
        float turn = Mathf.Clamp(Input.GetAxis("Horizontal") + L.x, -1f, 1f);
        float climb = Mathf.Clamp((Input.GetKey(KeyCode.Space) ? 1f : 0f)
                      - (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl) ? 1f : 0f) + R.y, -1f, 1f);
        if (Input.GetKeyDown(KeyCode.V)) cockpit = !cockpit;

        speed = Mathf.Lerp(speed, fwd * maxSpeed, dt * 1.2f);
        vy = Mathf.Lerp(vy, climb * climbSpeed, dt * 2f);
        yawRate = Mathf.Lerp(yawRate, turn * turnSpeed, dt * 2f);

        root.Rotate(0, yawRate * dt, 0);
        Vector3 np = root.position + root.forward * speed * dt + Vector3.up * vy * dt;
        np.y = Mathf.Clamp(np.y, 1f, 250f);
        if (np.y <= 1.01f) speed *= 0.97f;   // ground friction when landed

        if (Physics.CheckSphere(np + Vector3.up * 0.2f, 1.6f, ~0, QueryTriggerInteraction.Ignore))
        {
            speed = -speed * 0.3f; flash = 1f;   // bumped into a building / tree
        }
        else root.position = np;

        // visual tilt + rotors
        model.localRotation = Quaternion.Slerp(model.localRotation,
            Quaternion.Euler(fwd * 12f, 0, -yawRate * 0.25f), dt * 3f);
        rpm = Mathf.Lerp(rpm, 900f + Mathf.Abs(climb) * 500f + Mathf.Abs(fwd) * 200f, dt);
        mainRotor.Rotate(0, rpm * dt, 0);
        tailRotor.Rotate(rpm * 1.5f * dt, 0, 0);

        foreach (var c in clouds)
        {
            c.position += Vector3.right * 3f * dt;
            if (c.position.x > 450f) c.position += Vector3.left * 900f;
        }

        flash = Mathf.Max(0, flash - dt);
        hud = "ALT " + (root.position.y - 1f).ToString("0") + " m\n"
            + "SPD " + (Mathf.Abs(speed) * 3.6f).ToString("0") + " km/h\n"
            + "HDG " + root.eulerAngles.y.ToString("0") + " deg"
            + (flash > 0 ? "\n! COLLISION !" : "");
        if (dash != null) dash.text = hud;
    }

    void LateUpdate()
    {
        if (xrRig != null) return;   // VR rig is parented to the helicopter
        if (cockpit)
        {
            camT.position = root.TransformPoint(new Vector3(0, 0.3f, 0.5f));
            camT.rotation = model.rotation;
        }
        else
        {
            Vector3 target = root.TransformPoint(new Vector3(0, 4f, -13f));
            camT.position = Vector3.Lerp(camT.position, target, Time.deltaTime * 4f);
            camT.LookAt(root.position + Vector3.up);
        }
    }

    void OnGUI()
    {
        if (xrRig != null) return;
        int fs = Mathf.RoundToInt(Screen.height * 0.035f);
        var s = new GUIStyle(GUI.skin.label) { fontSize = fs, fontStyle = FontStyle.Bold };
        s.normal.textColor = new Color(0.1f, 1f, 0.4f);
        GUI.color = new Color(0, 0, 0, 0.45f);
        GUI.DrawTexture(new Rect(10, 10, fs * 9, fs * 5.2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(20, 14, fs * 9, fs * 5f), hud, s);
        s.fontSize = Mathf.RoundToInt(fs * 0.6f); s.normal.textColor = Color.white;
        GUI.Label(new Rect(10, Screen.height - fs * 1.5f, Screen.width, fs * 1.5f),
                  "W/S fly  A/D turn  SPACE up  SHIFT down  V change view", s);
    }
}
