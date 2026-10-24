using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

// EXP 6 - AR Foundation sample app for Android.
// Put this on the XR Origin (Mobile AR) object, next to AR Raycast Manager / AR Plane Manager.
// - A glowing reticle follows the centre of the screen over detected planes
// - Tap anywhere = place a glowing floating object at the reticle
// - Two-finger pinch = scale last object, two-finger twist = rotate it
// - On-screen buttons: Shape (cube/sphere/cylinder) and Clear
// In the Unity Editor, left-click acts like a tap (for quick testing).
[RequireComponent(typeof(ARRaycastManager))]
public class Exp6_ARTapToPlace : MonoBehaviour
{
    public GameObject prefab;      // optional: your own prefab. Empty = built-in glowing shapes.
    public Camera arCamera;

    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    readonly List<Transform> placed = new List<Transform>();
    readonly PrimitiveType[] shapes = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Cylinder };
    ARRaycastManager rays;
    ARPlaneManager planes;
    GameObject reticle;
    bool hasPose;
    Pose pose;
    int shape;

    void Start()
    {
        rays = GetComponent<ARRaycastManager>();
        planes = GetComponent<ARPlaneManager>();
        if (arCamera == null) arCamera = Camera.main;

        reticle = new GameObject("Reticle");
        var m = Kit.Mat(Color.white, 2f);
        Kit.Prim(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.12f, 0.002f, 0.12f), m, reticle.transform, false);
        Kit.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.002f, 0), new Vector3(0.09f, 0.002f, 0.09f),
                 Kit.Mat(Color.black), reticle.transform, false);
        Kit.Prim(PrimitiveType.Sphere, new Vector3(0, 0.01f, 0), Vector3.one * 0.015f, m, reticle.transform, false);
        reticle.SetActive(false);
    }

    Rect[] Buttons()
    {
        float h = Screen.height * 0.09f, w = Screen.width * 0.4f, pad = Screen.width * 0.05f;
        float y = Screen.height - h - Screen.height * 0.04f;
        return new[] { new Rect(pad, y, w, h), new Rect(Screen.width - pad - w, y, w, h) };
    }

    GameObject MakeObject()
    {
        if (prefab != null) return Instantiate(prefab);
        var root = new GameObject("Placed");
        var c = Color.HSVToRGB(Random.value, 0.7f, 1f);
        var body = Kit.Prim(shapes[shape], new Vector3(0, 0.15f, 0), Vector3.one * 0.15f, Kit.Mat(c, 0.8f, 0.9f), root.transform, false);
        body.name = "Body";
        Kit.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.002f, 0), new Vector3(0.3f, 0.002f, 0.3f), Kit.Mat(c, 1.5f), root.transform, false);
        return root;
    }

    void Place()
    {
        var g = MakeObject();
        g.transform.position = pose.position;
        g.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        placed.Add(g.transform);
        Kit.Burst(pose.position + Vector3.up * 0.1f, Color.white, 25);
    }

    void Update()
    {
        // reticle
        hasPose = rays.Raycast(new Vector2(Screen.width / 2f, Screen.height / 2f), hits, TrackableType.PlaneWithinPolygon);
        reticle.SetActive(hasPose);
        if (hasPose)
        {
            pose = hits[0].pose;
            reticle.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        // animate placed objects
        for (int i = 0; i < placed.Count; i++)
        {
            var body = placed[i].Find("Body");
            if (body == null) continue;
            body.Rotate(0, 60f * Time.deltaTime, 0, Space.Self);
            var lp = body.localPosition;
            lp.y = 0.15f + 0.03f * Mathf.Sin(Time.time * 2f + i);
            body.localPosition = lp;
        }

        // two-finger gestures on the last object
        if (Input.touchCount == 2 && placed.Count > 0)
        {
            Touch a = Input.GetTouch(0), b = Input.GetTouch(1);
            Vector2 pa = a.position - a.deltaPosition, pb = b.position - b.deltaPosition;
            Vector2 prev = pa - pb, cur = a.position - b.position;
            if (prev.magnitude > 1f)
            {
                var t = placed[placed.Count - 1];
                t.localScale *= cur.magnitude / prev.magnitude;
                t.localScale = Vector3.one * Mathf.Clamp(t.localScale.x, 0.3f, 4f);
                t.Rotate(0, -Vector2.SignedAngle(prev, cur), 0);
            }
            return;
        }

        // tap
        bool tap = false; Vector2 tp = Vector2.zero;
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        { tap = true; tp = Input.GetTouch(0).position; }
        else if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
        { tap = true; tp = Input.mousePosition; }

        if (tap)
        {
            Vector2 gp = new Vector2(tp.x, Screen.height - tp.y);
            foreach (var r in Buttons()) if (r.Contains(gp)) return;   // tap was on a button
            if (hasPose) Place();
        }
    }

    void OnGUI()
    {
        int fs = Mathf.RoundToInt(Screen.height * 0.03f);
        var s = new GUIStyle(GUI.skin.label) { fontSize = fs, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
        s.normal.textColor = Color.white;
        int found = planes != null ? planes.trackables.count : 0;
        string msg = found == 0 ? "Move your phone slowly to scan the floor..."
                   : hasPose ? "Tap to place  |  pinch = scale, twist = rotate"
                   : "Point the circle at a detected surface";
        GUI.Label(new Rect(0, Screen.height * 0.03f, Screen.width, fs * 2), msg, s);
        GUI.Label(new Rect(0, Screen.height * 0.03f + fs * 1.6f, Screen.width, fs * 2),
                  "Planes: " + found + "   Objects: " + placed.Count, s);

        var b = new GUIStyle(GUI.skin.button) { fontSize = fs, fontStyle = FontStyle.Bold };
        var r = Buttons();
        if (GUI.Button(r[0], "Shape: " + shapes[shape])) shape = (shape + 1) % shapes.Length;
        if (GUI.Button(r[1], "Clear"))
        {
            foreach (var t in placed) if (t) Destroy(t.gameObject);
            placed.Clear();
        }
    }
}
