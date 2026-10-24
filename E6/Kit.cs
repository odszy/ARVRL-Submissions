using UnityEngine;
using UnityEngine.XR;

// Shared helpers: glowing materials, primitives, 3D text, particle bursts, XR input.
// Works in Built-in RP and URP (copies whatever material Unity gives a primitive).
public static class Kit
{
    static Material baseMat;

    public static Material Mat(Color c, float glow = 0f, float smooth = 0.5f)
    {
        if (baseMat == null)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = p.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(p);
        }
        var m = new Material(baseMat);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (glow > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * glow);
        }
        return m;
    }

    public static GameObject Prim(PrimitiveType t, Vector3 pos, Vector3 scale, Material m,
                                  Transform parent = null, bool collider = true)
    {
        var g = GameObject.CreatePrimitive(t);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Object.Destroy(g.GetComponent<Collider>());
        return g;
    }

    public static TextMesh Text(string s, Vector3 pos, float size, Color c, Transform parent = null)
    {
        var g = new GameObject("Text");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        var tm = g.AddComponent<TextMesh>();
        tm.text = s; tm.characterSize = size; tm.fontSize = 64;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = c;
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        tm.font = f;
        g.GetComponent<MeshRenderer>().sharedMaterial = f.material;
        return tm;
    }

    public static void Burst(Vector3 p, Color c, int n = 30)
    {
        var g = new GameObject("Burst");
        g.transform.position = p;
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop();
        var main = ps.main;
        main.duration = 1f; main.loop = false; main.startLifetime = 0.8f;
        main.startSpeed = 4f; main.startSize = 0.15f; main.startColor = c;
        main.gravityModifier = 0.5f;
        var em = ps.emission;
        em.rateOverTime = 0;
        em.SetBurst(0, new ParticleSystem.Burst(0f, (short)n));
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
        g.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat(c, 2f);
        ps.Play();
        Object.Destroy(g, 2f);
    }

    public static Camera Cam()
    {
        var c = Camera.main;
        if (c == null)
        {
            var g = new GameObject("Main Camera");
            g.tag = "MainCamera";
            c = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        return c;
    }

    public static void Sky(Color c)
    {
        var cam = Cam();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = c;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(c, Color.white, 0.55f);
    }

    public static void Light(float intensity = 1.1f)
    {
        if (GameObject.Find("Directional Light") != null) return;
        var g = new GameObject("Directional Light");
        var l = g.AddComponent<Light>();
        l.type = LightType.Directional; l.intensity = intensity;
        g.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    // ---- XR input without extra packages ----
    public static bool Trigger(XRNode hand)
    {
        var d = InputDevices.GetDeviceAtXRNode(hand);
        bool v;
        return d.isValid && d.TryGetFeatureValue(CommonUsages.triggerButton, out v) && v;
    }

    public static Vector2 Stick(XRNode hand)
    {
        var d = InputDevices.GetDeviceAtXRNode(hand);
        Vector2 v;
        if (d.isValid && d.TryGetFeatureValue(CommonUsages.primary2DAxis, out v)) return v;
        return Vector2.zero;
    }
}
