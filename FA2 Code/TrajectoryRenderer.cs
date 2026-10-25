using System.Collections;
using UnityEngine;

// Draws the animated ball-trajectory arc from the player to the tapped point.
[RequireComponent(typeof(LineRenderer))]
public class TrajectoryRenderer : MonoBehaviour
{
    public int points = 30;
    public float arcHeight = 0.5f;
    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 0;
    }

    public IEnumerator Play(Vector3 from, Vector3 to, float duration = 0.5f)
    {
        lr.positionCount = points;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float progress = Mathf.Clamp01(t);
            for (int i = 0; i < points; i++)
            {
                float k = Mathf.Min(i / (float)(points - 1), progress);
                lr.SetPosition(i, Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * arcHeight);
            }
            yield return null;
        }
        yield return new WaitForSeconds(0.4f);
        lr.positionCount = 0;
    }
}
