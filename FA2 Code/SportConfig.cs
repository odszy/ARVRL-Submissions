using UnityEngine;

// One asset per sport (Cricket, Football, Basketball, Tennis).
// Create via: Assets > Create > ARSports > Sport Config
[CreateAssetMenu(menuName = "ARSports/Sport Config")]
public class SportConfig : ScriptableObject
{
    public string displayName = "Cricket";
    [TextArea] public string instruction = "Tap the target when the ring meets its edge";
    public GameObject targetPrefab;      // must have TargetController + child "Ring"

    [Header("Spawn area (local to the training zone, metres)")]
    public float areaWidth = 1.2f;
    public float minHeight = 0.3f;
    public float maxHeight = 1.2f;

    [Header("Difficulty (beginner values)")]
    public float baseRadius = 0.18f;     // metres
    public float baseLife = 2.5f;        // seconds
}
