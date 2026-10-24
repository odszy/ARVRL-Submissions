using UnityEngine;
using System.Collections.Generic;
using Vuforia;   // import the Vuforia Engine package BEFORE adding this file

// EXP 7 - helpers for the Vuforia AR app (image/marker tracking, object tracking, ground plane).
//
// Exp7_TargetFeedback: put on every Image Target / Object Target / Multi Target / Model Target.
//   Its first child (or "content") pops in with an animated scale when the target is tracked,
//   spins slowly, and shrinks away when tracking is lost.
// Exp7_HUD: put on ANY one GameObject. Shows which targets are tracked on screen.
[RequireComponent(typeof(ObserverBehaviour))]
public class Exp7_TargetFeedback : MonoBehaviour
{
    public static readonly HashSet<string> Active = new HashSet<string>();

    public Transform content;
    public string label = "";
    public float spinSpeed = 40f;

    ObserverBehaviour obs;
    Vector3 baseScale = Vector3.one;
    float s;
    bool tracked;

    void Awake()
    {
        obs = GetComponent<ObserverBehaviour>();
        obs.OnTargetStatusChanged += OnStatus;
        if (label == "") label = gameObject.name;
    }

    void Start()
    {
        if (content == null && transform.childCount > 0) content = transform.GetChild(0);
        if (content != null) baseScale = content.localScale;
    }

    void OnDestroy()
    {
        if (obs != null) obs.OnTargetStatusChanged -= OnStatus;
        Active.Remove(label);
    }

    void OnStatus(ObserverBehaviour b, TargetStatus st)
    {
        tracked = st.Status == Status.TRACKED || st.Status == Status.EXTENDED_TRACKED;
        if (tracked) Active.Add(label); else Active.Remove(label);
    }

    void Update()
    {
        if (content == null) return;
        s = Mathf.MoveTowards(s, tracked ? 1f : 0f, Time.deltaTime * 3f);
        // ease-out-back pop
        float k = s < 1f ? 1f + 2.7f * Mathf.Pow(s - 1f, 3f) + 1.7f * Mathf.Pow(s - 1f, 2f) : 1f;
        content.localScale = baseScale * Mathf.Max(0f, k);
        if (tracked) content.Rotate(0, spinSpeed * Time.deltaTime, 0, Space.Self);
    }
}

public class Exp7_HUD : MonoBehaviour
{
    void OnGUI()
    {
        int fs = Mathf.RoundToInt(Screen.height * 0.035f);
        var s = new GUIStyle(GUI.skin.label) { fontSize = fs, fontStyle = FontStyle.Bold };
        s.normal.textColor = Color.white;
        GUI.color = new Color(0, 0, 0, 0.55f);
        GUI.DrawTexture(new Rect(10, 10, Screen.width * 0.6f, fs * 2.4f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        string txt = Exp7_TargetFeedback.Active.Count == 0
            ? "Scanning... show a marker / object"
            : "Tracking: " + string.Join(", ", Exp7_TargetFeedback.Active);
        GUI.Label(new Rect(20, 14, Screen.width * 0.6f, fs * 2f), txt, s);
    }
}
