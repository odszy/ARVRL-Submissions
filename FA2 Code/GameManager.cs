using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The 8 workflow steps from the FA1 report.
public enum Stage { Start, Scan, Setup, Train, Detect, Analyze, Score, Feedback }

// AR Training Engine: runs a session, scores performance, adapts difficulty.
public class GameManager : MonoBehaviour
{
    public SportConfig[] sports;
    public PlacementController placement;
    public TrajectoryRenderer trajectory;
    public UIController ui;
    public FeedbackManager feedback;
    public int targetsPerSession = 10;

    int sportIdx;
    bool pro;
    float radiusMul = 1f, lifeMul = 1f;
    int score, hits;
    readonly List<float> accs = new List<float>();
    readonly List<float> rts = new List<float>();
    Camera cam;

    void Start() { cam = Camera.main; ui.ShowHome(); ui.SetStage(Stage.Start); }

    // --- hooked to UI buttons ---
    public void SelectSport(int i) => sportIdx = Mathf.Clamp(i, 0, sports.Length - 1);
    public void SelectMode(int m)  => pro = (m == 1);
    public void StartSession()     { StopAllCoroutines(); StartCoroutine(Session()); }
    public void BackToHome()       { StopAllCoroutines(); ui.ShowHome(); }

    void SetStage(Stage s) => ui.SetStage(s);

    IEnumerator Session()
    {
        score = hits = 0; accs.Clear(); rts.Clear(); radiusMul = lifeMul = 1f;
        ui.ShowHUD(); ui.SetScore(0);
        var cfg = sports[sportIdx];

        SetStage(Stage.Scan);
        ui.Hint("Move your phone slowly to scan the floor");
        placement.Enabled = false;
        yield return new WaitUntil(() => placement.PlanesFound);

        SetStage(Stage.Setup);
        ui.Hint("Tap a surface to place the training zone");
        placement.ResetPlacement();
        yield return new WaitUntil(() => placement.Placed);
        Transform zone = placement.ZoneRoot;

        for (int i = 1; i <= targetsPerSession; i++)
        {
            ui.SetCount(i, targetsPerSession);
            SetStage(Stage.Train);
            ui.Hint(cfg.instruction);
            yield return RunTarget(cfg, zone);
        }
        Finish(cfg);
    }

    IEnumerator RunTarget(SportConfig cfg, Transform zone)
    {
        float r = cfg.baseRadius * (pro ? 0.7f : 1f) * radiusMul;
        float life = cfg.baseLife * (pro ? 0.65f : 1f) * lifeMul;
        Vector3 local = new Vector3(Random.Range(-cfg.areaWidth / 2f, cfg.areaWidth / 2f),
                                    Random.Range(cfg.minHeight, cfg.maxHeight), 0f);

        GameObject go = Instantiate(cfg.targetPrefab, zone);
        go.transform.localPosition = local;
        var target = go.GetComponent<TargetController>();
        target.Init(r, life);

        float start = Time.time;
        bool tapped = false;
        Vector3 hitPoint = Vector3.zero;

        while (!target.Expired)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                // project the tap onto the plane of the target (camera-facing)
                Ray ray = cam.ScreenPointToRay(Input.GetTouch(0).position);
                var plane = new Plane(-cam.transform.forward, target.transform.position);
                if (plane.Raycast(ray, out float d)) { hitPoint = ray.GetPoint(d); tapped = true; break; }
            }
            yield return null;
        }

        float reaction = Time.time - start;
        int pts = 0; bool inside = false; float acc = 0f;

        if (tapped)
        {
            SetStage(Stage.Detect);
            Vector3 from = cam.transform.position + cam.transform.forward * 0.3f - cam.transform.up * 0.3f;
            yield return trajectory.Play(from, hitPoint);

            SetStage(Stage.Analyze);
            var res = target.Evaluate(hitPoint);
            inside = res.inside; acc = res.accuracy;
            yield return new WaitForSeconds(0.2f);

            SetStage(Stage.Score);
            pts = inside ? Mathf.RoundToInt(res.accuracy * 0.6f + res.timing * 0.4f) : 0;
            score += pts; ui.SetScore(score);
            if (inside) { hits++; accs.Add(acc); rts.Add(reaction * 1000f); }
            yield return new WaitForSeconds(0.2f);
        }

        SetStage(Stage.Feedback);
        string msg = !tapped ? "TOO SLOW" : pts >= 80 ? $"PERFECT +{pts}" : pts >= 45 ? $"GOOD +{pts}" : inside ? $"LATE +{pts}" : "MISS";
        Color col = pts >= 80 ? Color.yellow : pts >= 45 ? Color.green : Color.red;
        ui.Flash(msg, col);
        feedback.Play(pts, inside);

        // adaptive difficulty: personalised training
        float k = pts >= 70 ? 0.94f : 1.06f;
        radiusMul = Mathf.Clamp(radiusMul * k, 0.7f, 1.4f);
        lifeMul   = Mathf.Clamp(lifeMul * k, 0.7f, 1.3f);

        Destroy(go);
        yield return new WaitForSeconds(0.7f);
    }

    void Finish(SportConfig cfg)
    {
        float avgAcc = accs.Count > 0 ? accs.Average() : 0f;
        float avgRt  = rts.Count > 0 ? rts.Average() : 0f;

        string tip = hits < 5 ? "Many targets missed. Track the shrinking ring and tap as it meets the target edge."
                   : avgAcc < 60 ? "Good timing, but aim for the centre of the target."
                   : avgRt > 1300 ? "Accurate but slow. React earlier to build decision speed."
                   : "Excellent control. Next session will use smaller targets and shorter timing.";

        // progress tracking across sessions
        string key = "ar_history";
        var hist = PlayerPrefs.GetString(key, "").Split(',', System.StringSplitOptions.RemoveEmptyEntries).ToList();
        hist.Add(score.ToString());
        if (hist.Count > 10) hist.RemoveRange(0, hist.Count - 10);
        PlayerPrefs.SetString(key, string.Join(",", hist));
        PlayerPrefs.Save();

        ui.ShowResults(
            $"{cfg.displayName} - {(pro ? "Pro" : "Beginner")}\n\n" +
            $"Total score: {score}\nHit rate: {hits * 100 / targetsPerSession}%\n" +
            $"Avg accuracy: {avgAcc:0}%\nAvg reaction: {avgRt:0} ms\n\n" +
            $"Coach tip: {tip}\n\nLast scores: {string.Join(", ", hist)}");
    }
}
