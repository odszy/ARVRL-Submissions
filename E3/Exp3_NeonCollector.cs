using UnityEngine;
using System.Collections.Generic;

// EXP 3 - WebGL mini game "Neon Collector".
// Attach to an empty GameObject in an empty scene. Everything is built from code.
// Move: WASD / arrows. Collect gold coins, dodge red cubes. 60 s, 3 lives. R / Space = restart.
public class Exp3_NeonCollector : MonoBehaviour
{
    const float A = 14f; // arena half-size
    public float maxSpeed = 11f, accel = 30f, gameTime = 60f;

    Transform player, camT;
    Vector3 vel;
    readonly List<Transform> coins = new List<Transform>();
    readonly List<Transform> hazards = new List<Transform>();
    readonly List<Vector3> hazVel = new List<Vector3>();
    Material coinMat, hazMat;
    int score, best, lives;
    float timeLeft, invuln, nextHazard;
    bool over;
    Renderer playerRend;
    GUIStyle style;

    void Start()
    {
        Kit.Sky(new Color(0.02f, 0.02f, 0.08f));
        Kit.Light(0.8f);
        camT = Kit.Cam().transform;
        coinMat = Kit.Mat(new Color(1f, 0.85f, 0.1f), 1.5f);
        hazMat = Kit.Mat(new Color(1f, 0.1f, 0.2f), 1.5f);

        // floor + glowing grid
        Kit.Prim(PrimitiveType.Cube, new Vector3(0, -0.25f, 0), new Vector3(A * 2, 0.5f, A * 2),
                 Kit.Mat(new Color(0.05f, 0.08f, 0.2f), 0.2f, 0.9f), null, false);
        var line = Kit.Mat(new Color(0f, 0.8f, 1f), 1.2f);
        for (int i = -(int)A; i <= (int)A; i += 4)
        {
            Kit.Prim(PrimitiveType.Cube, new Vector3(i, 0.01f, 0), new Vector3(0.05f, 0.02f, A * 2), line, null, false);
            Kit.Prim(PrimitiveType.Cube, new Vector3(0, 0.01f, i), new Vector3(A * 2, 0.02f, 0.05f), line, null, false);
        }
        // neon walls
        var wm = Kit.Mat(new Color(1f, 0.2f, 0.9f), 1.8f);
        Kit.Prim(PrimitiveType.Cube, new Vector3(0, 0.4f, A), new Vector3(A * 2, 0.8f, 0.3f), wm, null, false);
        Kit.Prim(PrimitiveType.Cube, new Vector3(0, 0.4f, -A), new Vector3(A * 2, 0.8f, 0.3f), wm, null, false);
        Kit.Prim(PrimitiveType.Cube, new Vector3(A, 0.4f, 0), new Vector3(0.3f, 0.8f, A * 2), wm, null, false);
        Kit.Prim(PrimitiveType.Cube, new Vector3(-A, 0.4f, 0), new Vector3(0.3f, 0.8f, A * 2), wm, null, false);

        // player ball with glowing trail
        player = Kit.Prim(PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), Vector3.one,
                          Kit.Mat(new Color(0f, 0.9f, 1f), 1f, 0.9f), null, false).transform;
        playerRend = player.GetComponent<Renderer>();
        var tr = player.gameObject.AddComponent<TrailRenderer>();
        tr.time = 0.6f; tr.startWidth = 0.6f; tr.endWidth = 0f;
        tr.sharedMaterial = Kit.Mat(new Color(0f, 0.9f, 1f), 2f);

        StartGame();
    }

    void StartGame()
    {
        foreach (var c in coins) if (c) Destroy(c.gameObject);
        foreach (var h in hazards) if (h) Destroy(h.gameObject);
        coins.Clear(); hazards.Clear(); hazVel.Clear();
        score = 0; lives = 3; timeLeft = gameTime; over = false; invuln = 0; nextHazard = 10f;
        vel = Vector3.zero;
        player.position = new Vector3(0, 0.5f, 0);
        player.GetComponent<TrailRenderer>().Clear();
        for (int i = 0; i < 8; i++) SpawnCoin();
        for (int i = 0; i < 3; i++) SpawnHazard();
    }

    Vector3 RandPos(float y)
    {
        Vector3 p;
        do { p = new Vector3(Random.Range(-A + 2, A - 2), y, Random.Range(-A + 2, A - 2)); }
        while ((p - player.position).sqrMagnitude < 25f);
        return p;
    }

    void SpawnCoin()
    {
        var g = Kit.Prim(PrimitiveType.Cylinder, RandPos(0.8f), new Vector3(0.8f, 0.08f, 0.8f), coinMat, null, false);
        g.transform.rotation = Quaternion.Euler(90, 0, 0);
        coins.Add(g.transform);
    }

    void SpawnHazard()
    {
        var g = Kit.Prim(PrimitiveType.Cube, RandPos(0.6f), Vector3.one * 1.2f, hazMat, null, false);
        float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        hazVel.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Random.Range(4f, 7f));
        hazards.Add(g.transform);
    }

    void Follow()
    {
        Vector3 target = player.position + new Vector3(0, 12, -10);
        camT.position = Vector3.Lerp(camT.position, target, Time.deltaTime * 4f);
        camT.LookAt(player.position);
    }

    void End() { over = true; best = Mathf.Max(best, score); }

    void Update()
    {
        if (over)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space)) StartGame();
            Follow();
            return;
        }
        float dt = Time.deltaTime;
        timeLeft -= dt;
        if (timeLeft <= 0f) { timeLeft = 0f; End(); }

        Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        vel = Vector3.MoveTowards(vel, dir * maxSpeed, accel * dt);
        Vector3 p = player.position + vel * dt;
        p.x = Mathf.Clamp(p.x, -A + 0.5f, A - 0.5f);
        p.z = Mathf.Clamp(p.z, -A + 0.5f, A - 0.5f);
        player.position = p;
        if (vel.sqrMagnitude > 0.01f)
            player.Rotate(Vector3.Cross(Vector3.up, vel).normalized,
                          vel.magnitude * dt * Mathf.Rad2Deg / 0.5f, Space.World);

        // coins
        for (int i = coins.Count - 1; i >= 0; i--)
        {
            var c = coins[i];
            c.Rotate(0, 180f * dt, 0, Space.World);
            if ((c.position - p).sqrMagnitude < 1.2f)
            {
                score++;
                Kit.Burst(c.position, new Color(1f, 0.85f, 0.1f));
                Destroy(c.gameObject);
                coins.RemoveAt(i);
                SpawnCoin();
            }
        }

        // hazards
        invuln -= dt;
        for (int i = 0; i < hazards.Count; i++)
        {
            var h = hazards[i];
            Vector3 v = hazVel[i];
            h.position += v * dt;
            Vector3 hp = h.position;
            if (Mathf.Abs(hp.x) > A - 1f) { v.x = -v.x; hp.x = Mathf.Clamp(hp.x, -A + 1f, A - 1f); }
            if (Mathf.Abs(hp.z) > A - 1f) { v.z = -v.z; hp.z = Mathf.Clamp(hp.z, -A + 1f, A - 1f); }
            h.position = hp; hazVel[i] = v;
            h.Rotate(0, 120f * dt, 0);
            if (invuln <= 0f && (h.position - p).sqrMagnitude < 1.3f)
            {
                lives--; invuln = 1.5f;
                Kit.Burst(p, new Color(1f, 0.1f, 0.2f), 50);
                if (lives <= 0) End();
            }
        }
        playerRend.enabled = invuln <= 0f || Mathf.FloorToInt(Time.time * 10f) % 2 == 0;

        if (gameTime - timeLeft > nextHazard) { SpawnHazard(); nextHazard += 10f; }
        Follow();
    }

    void Label(Rect r, string s, int size, Color c, TextAnchor a)
    {
        if (style == null) style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        style.fontSize = size; style.alignment = a; style.normal.textColor = c;
        GUI.Label(r, s, style);
    }

    void OnGUI()
    {
        int fs = Mathf.RoundToInt(Screen.height * 0.045f);
        float w = Screen.width, h = Screen.height;
        Label(new Rect(20, 10, 400, fs * 1.6f), "SCORE  " + score, fs, Color.yellow, TextAnchor.UpperLeft);
        Label(new Rect(w / 2 - 150, 10, 300, fs * 1.6f), "TIME  " + Mathf.CeilToInt(timeLeft), fs, Color.white, TextAnchor.UpperCenter);
        Label(new Rect(w - 320, 10, 300, fs * 1.6f), "LIVES  " + lives, fs, new Color(1f, 0.3f, 0.4f), TextAnchor.UpperRight);
        if (!over) return;
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        Label(new Rect(0, h * 0.25f, w, fs * 2.5f), "GAME OVER", fs * 2, new Color(1f, 0.3f, 0.5f), TextAnchor.MiddleCenter);
        Label(new Rect(0, h * 0.42f, w, fs * 1.6f), "Score " + score + "    Best " + best, fs, Color.white, TextAnchor.MiddleCenter);
        if (GUI.Button(new Rect(w / 2 - 130, h * 0.58f, 260, fs * 1.8f), "PLAY AGAIN")) StartGame();
    }
}
