using UnityEngine;

public class CameraFollowMulti : MonoBehaviour
{
    [Header("Area scoping")]
    [Tooltip("Set this to the root object of ONE environment/area. If null, uses this object's root.")]
    [SerializeField] private Transform areaRoot;

    [Header("Camera Motion")]
    [SerializeField] private Vector2 horizontalRangeMulti = Vector2.zero;
    [SerializeField] private float floorMulti = 1f;
    [SerializeField] private float smoothTimeMulti = 0.15f;

    [Header("Fighters (auto if empty)")]
    public GameObject[] fightersMulti;

    private Vector3 velocityMulti = Vector3.zero;

    private void Awake()
    {
        if (areaRoot == null) areaRoot = transform.root;

        // Auto-populate fighters ONLY from this area if not assigned in Inspector.
        if (fightersMulti == null || fightersMulti.Length == 0)
        {
            var localFighters = areaRoot.GetComponentsInChildren<NewFighter>(true);
            fightersMulti = new GameObject[localFighters.Length];
            for (int i = 0; i < localFighters.Length; i++)
                fightersMulti[i] = localFighters[i].gameObject;
        }
    }

    private void LateUpdate()
    {
        if (fightersMulti == null || fightersMulti.Length == 0) return;

        float sumX = 0f;
        float sumY = 0f;
        int count = 0;

        for (int i = 0; i < fightersMulti.Length; i++)
        {
            var f = fightersMulti[i];
            if (f == null) continue;
            sumX += f.transform.position.x;
            sumY += f.transform.position.y;
            count++;
        }

        if (count == 0) return;

        Vector3 target = transform.position;
        target.x = sumX / count;
        target.y = sumY / count;

        target.x = Mathf.Clamp(target.x, horizontalRangeMulti.x, horizontalRangeMulti.y);
        target.y = Mathf.Max(floorMulti, target.y);
        target.z = transform.position.z;

        // Keep your fixed timestep feel
        transform.position = Vector3.SmoothDamp(
            transform.position, target, ref velocityMulti, smoothTimeMulti, Mathf.Infinity, 0.0167f
        );
    }
}
