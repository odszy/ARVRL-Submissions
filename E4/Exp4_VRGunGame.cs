using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

// EXP 4 - VR gun game: each trigger click destroys the cube you point at and gives +1 score.
// Score UI is shown on a big world-space board AND a small HUD attached to your head.
// Attach to an empty GameObject. In VR: drag the RIGHT controller into "gun" (or leave empty,
// it will try to find one). Without a headset it runs in desktop mode:
// hold right mouse to look, left click / Space to shoot.
public class Exp4_VRGunGame : MonoBehaviour
{
    public Transform gun;          // right controller (optional)
    public int cubeCount = 10;
    public float range = 80f;

    readonly List<GameObject> targets = new List<GameObject>();
    TextMesh board, hud;
    LineRenderer laser;
    Transform aim;
    Camera cam;
    int score, wave = 1;
    bool prevTrigger, vr;
    float yaw, pitch;

    void Start()
    {
        vr = XRSettings.isDeviceActive;
        cam = Kit.Cam();
        Kit.Sky(new Color(0.1f, 0.12f, 0.25f));
        Kit.Light(1f);

        // ground plane (Unity's built-in Plane primitive)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(5, 1, 5);
        ground.GetComponent<Renderer>().sharedMaterial = Kit.Mat(new Color(0.12f, 0.15f, 0.2f), 0.1f, 0.8f);
        // glowing ring markers on the floor for atmosphere
        for (int r = 6; r <= 18; r += 6)
            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                Kit.Prim(PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * r, 0.02f, Mathf.Sin(a) * r),
                         new Vector3(0.15f, 0.03f, 0.6f), Kit.Mat(new Color(0f, 0.8f, 1f), 1.5f), null, false)
                   .transform.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
            }

        if (vr && gun == null) gun = FindController();
        aim = gun != null ? gun : cam.transform;

        // laser pointer
        laser = new GameObject("Laser").AddComponent<LineRenderer>();
        laser.positionCount = 2;
        laser.startWidth = laser.endWidth = vr ? 0.01f : 0f;
        laser.sharedMaterial = Kit.Mat(new Color(1f, 0.1f, 0.1f), 3f);

        // visible gun body for the controller
        if (gun != null)
        {
            var body = Kit.Prim(PrimitiveType.Cube, new Vector3(0, -0.02f, 0.08f), new Vector3(0.04f, 0.06f, 0.2f),
                                Kit.Mat(new Color(0.15f, 0.15f, 0.2f), 0f, 0.9f), gun, false);
            Kit.Prim(PrimitiveType.Cube, new Vector3(0, -0.07f, 0.02f), new Vector3(0.03f, 0.08f, 0.04f),
                     Kit.Mat(new Color(0.3f, 0.3f, 0.35f)), gun, false);
        }

        // score UI
        board = Kit.Text("SCORE: 0", new Vector3(0, 3.5f, 12f), 0.25f, Color.yellow);
        hud = Kit.Text("SCORE: 0", new Vector3(-0.35f, 0.25f, 1.2f), 0.012f, Color.yellow, cam.transform);

        SpawnWave();
        UpdateUI();
    }

    Transform FindController()
    {
        string[] names = { "Right Controller", "RightHand Controller", "Right Hand Controller", "RightHand", "Right Hand" };
        foreach (var n in names)
        {
            var g = GameObject.Find(n);
            if (g != null) return g.transform;
        }
        return null;
    }

    void SpawnWave()
    {
        int n = cubeCount + (wave - 1) * 2;
        for (int i = 0; i < n; i++)
        {
            float ang = Random.Range(-80f, 80f) * Mathf.Deg2Rad;   // in front of the player (+Z)
            float dist = Random.Range(5f, 16f);
            float s = Random.Range(0.6f, 1.2f);
            Color c = Color.HSVToRGB(Random.value, 0.8f, 1f);
            var t = Kit.Prim(PrimitiveType.Cube, new Vector3(Mathf.Sin(ang) * dist, s / 2f, Mathf.Cos(ang) * dist),
                             Vector3.one * s, Kit.Mat(c, 1.2f, 0.8f));
            t.name = "Target";
            t.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 90f), 0);
            targets.Add(t);
        }
    }

    void NextWave() { wave++; SpawnWave(); UpdateUI(); }

    void UpdateUI()
    {
        string s = "SCORE: " + score + "\nWAVE " + wave;
        board.text = s; hud.text = "SCORE: " + score;
    }

    void Update()
    {
        // desktop look (no headset)
        if (!vr && Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * 3f;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 3f, -60f, 60f);
            cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        }
        if (!vr) cam.transform.position = new Vector3(0, 1.6f, 0);

        bool t = Kit.Trigger(XRNode.RightHand);
        bool fire = (t && !prevTrigger) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        prevTrigger = t;

        RaycastHit hit;
        bool ok = Physics.Raycast(aim.position, aim.forward, out hit, range);
        laser.SetPosition(0, aim.position + aim.forward * 0.1f);
        laser.SetPosition(1, ok ? hit.point : aim.position + aim.forward * range);

        foreach (var g in targets) if (g) g.transform.Rotate(0, 40f * Time.deltaTime, 0);

        if (fire)
        {
            Kit.Burst(aim.position + aim.forward * 0.3f, Color.yellow, 6);
            if (ok && hit.collider.name == "Target")
            {
                var go = hit.collider.gameObject;
                Kit.Burst(go.transform.position, go.GetComponent<Renderer>().sharedMaterial.color, 40);
                targets.Remove(go);
                Destroy(go);
                score++;
                UpdateUI();
                if (targets.Count == 0) Invoke(nameof(NextWave), 1.2f);
            }
        }
    }

    void OnGUI()
    {
        if (vr) return;
        float w = Screen.width, h = Screen.height;
        GUI.color = Color.white;
        GUI.Label(new Rect(w / 2 - 6, h / 2 - 12, 30, 30), "+");
        GUI.Label(new Rect(10, 10, 500, 30), "Desktop mode: right-mouse = look, left click / Space = shoot");
    }
}
