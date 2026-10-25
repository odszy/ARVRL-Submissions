using UnityEngine;

// Virtual target with a shrinking timing ring (the "timing indicator").
// Prefab layout: root (1-unit disc/sphere visual) + child "Ring" (1-unit ring mesh).
// Root scale is set to the radius so the target is sized in real metres.
public class TargetController : MonoBehaviour
{
    public Transform ring;
    public float ringStart = 2.8f, ringEnd = 0.6f;   // ring meets target edge (scale 1) at ~82% of life

    public float Radius { get; private set; }
    public bool Expired { get; private set; }

    float life, t0;

    public void Init(float radius, float lifeSeconds)
    {
        Radius = radius;
        life = lifeSeconds;
        t0 = Time.time;
        transform.localScale = Vector3.one * radius * 2f;
    }

    void Update()
    {
        float p = (Time.time - t0) / life;
        if (ring != null)
            ring.localScale = Vector3.one * Mathf.Lerp(ringStart, ringEnd, Mathf.Clamp01(p));
        if (p >= 1f) Expired = true;

        // billboard toward the camera
        Vector3 dir = transform.position - Camera.main.transform.position;
        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
    }

    // hitPoint = tap projected onto the target's plane (world space)
    public (float accuracy, float timing, bool inside) Evaluate(Vector3 hitPoint)
    {
        float d = Vector3.Distance(hitPoint, transform.position);
        bool inside = d <= Radius * 1.25f;
        float accuracy = inside ? Mathf.Clamp01(1f - d / (Radius * 1.25f)) * 100f : 0f;
        float ringScale = ring != null ? ring.localScale.x : 1f;
        float timing = inside ? Mathf.Clamp01(1f - Mathf.Abs(ringScale - 1f)) * 100f : 0f;
        return (accuracy, timing, inside);
    }
}
