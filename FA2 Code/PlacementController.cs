using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Workflow steps 2-3 (Scan, Setup): detects floor planes, then lets the
// player tap a plane to anchor the virtual training zone (registration).
[RequireComponent(typeof(ARRaycastManager))]
public class PlacementController : MonoBehaviour
{
    public ARPlaneManager planeManager;
    public GameObject zonePrefab;

    public bool Enabled { get; set; }
    public bool Placed { get; private set; }
    public Transform ZoneRoot { get; private set; }
    public bool PlanesFound => planeManager != null && planeManager.trackables.count > 0;

    ARRaycastManager rays;
    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake() => rays = GetComponent<ARRaycastManager>();

    public void ResetPlacement()
    {
        Placed = false;
        Enabled = true;
        if (ZoneRoot != null) ZoneRoot.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!Enabled || Input.touchCount == 0) return;
        Touch t = Input.GetTouch(0);
        if (t.phase != TouchPhase.Began) return;

        if (rays.Raycast(t.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;
            if (ZoneRoot == null) ZoneRoot = Instantiate(zonePrefab).transform;
            ZoneRoot.gameObject.SetActive(true);
            ZoneRoot.position = pose.position;

            // face the player so targets are visible
            Vector3 toCam = Camera.main.transform.position - pose.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.001f)
                ZoneRoot.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);

            // hide plane visuals once placed
            foreach (var p in planeManager.trackables) p.gameObject.SetActive(false);

            Enabled = false;
            Placed = true;
        }
    }
}
