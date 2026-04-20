using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CameraVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraDetector detector;

    [Header("Mesh")]
    [SerializeField] private bool isStatic;
    [SerializeField, Range(1f, 10f)] private float rayStepDegrees = 4f;
    [SerializeField, Min(0)] private int edgeResolveIterations = 4;
    [SerializeField, Min(0f)] private float edgeDistanceThreshold = 0.5f;
    [SerializeField] private LayerMask environmentMask;

    [Header("Colors")]
    [SerializeField] private Color scanningColor = new Color(0.2f, 1f, 0.3f, 0.2f);
    [SerializeField] private Color suspiciousColor = new Color(1f, 0.8f, 0.2f, 0.35f);
    [SerializeField] private Color detectedColor = new Color(1f, 0.2f, 0.2f, 0.45f);

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh fovMesh;
    private readonly List<Vector3> vertices = new List<Vector3>(128);
    private readonly List<int> triangles = new List<int>(384);

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (detector == null)
        {
            detector = GetComponent<CameraDetector>();
        }

        fovMesh = new Mesh { name = "CameraFOVMesh" };
        fovMesh.MarkDynamic();
        meshFilter.sharedMesh = fovMesh;
    }

    private void Start()
    {
        RebuildMesh();
    }

    private void LateUpdate()
    {
        if (!isStatic)
        {
            RebuildMesh();
        }

        UpdateColor();
    }

    private void RebuildMesh()
    {
        if (detector == null)
        {
            return;
        }

        float viewAngle = detector.ViewAngle;
        float range = detector.DetectionRange;

        int rayCount = Mathf.Max(2, Mathf.CeilToInt(viewAngle / rayStepDegrees));
        float stepAngle = viewAngle / rayCount;
        float startAngle = -viewAngle * 0.5f;

        vertices.Clear();
        triangles.Clear();

        vertices.Add(Vector3.zero);

        ViewCastInfo previousCast = ViewCast(startAngle, range);
        vertices.Add(transform.InverseTransformPoint(previousCast.point));

        for (int i = 1; i <= rayCount; i++)
        {
            float angle = startAngle + stepAngle * i;
            ViewCastInfo currentCast = ViewCast(angle, range);

            bool edgeDiscontinuity = previousCast.hit != currentCast.hit || Mathf.Abs(previousCast.distance - currentCast.distance) > edgeDistanceThreshold;
            if (edgeDiscontinuity)
            {
                if (FindEdge(previousCast, currentCast, range, out Vector3 edgePointA, out Vector3 edgePointB))
                {
                    vertices.Add(transform.InverseTransformPoint(edgePointA));
                    vertices.Add(transform.InverseTransformPoint(edgePointB));
                }
            }

            vertices.Add(transform.InverseTransformPoint(currentCast.point));
            previousCast = currentCast;
        }

        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        fovMesh.Clear();
        fovMesh.SetVertices(vertices);
        fovMesh.SetTriangles(triangles, 0);
        fovMesh.RecalculateBounds();
    }

    private ViewCastInfo ViewCast(float localAngle, float range)
    {
        Vector2 direction = DirectionFromAngle(localAngle);
        Vector2 origin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, range, environmentMask);

        if (hit.collider != null)
        {
            return new ViewCastInfo(true, hit.point, hit.distance, localAngle);
        }

        return new ViewCastInfo(false, origin + direction * range, range, localAngle);
    }

    private bool FindEdge(ViewCastInfo minCast, ViewCastInfo maxCast, float range, out Vector3 edgePointA, out Vector3 edgePointB)
    {
        float minAngle = minCast.angle;
        float maxAngle = maxCast.angle;
        ViewCastInfo minResult = minCast;
        ViewCastInfo maxResult = maxCast;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) * 0.5f;
            ViewCastInfo newCast = ViewCast(angle, range);
            bool sameSide = newCast.hit == minResult.hit && Mathf.Abs(newCast.distance - minResult.distance) <= edgeDistanceThreshold;

            if (sameSide)
            {
                minAngle = angle;
                minResult = newCast;
            }
            else
            {
                maxAngle = angle;
                maxResult = newCast;
            }
        }

        edgePointA = minResult.point;
        edgePointB = maxResult.point;
        return true;
    }

    private Vector2 DirectionFromAngle(float localAngle)
    {
        float worldAngle = transform.eulerAngles.z + localAngle;
        float radians = worldAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private void UpdateColor()
    {
        if (detector == null || meshRenderer == null || meshRenderer.sharedMaterial == null)
        {
            return;
        }

        float normalizedSuspicion = Mathf.Clamp01(detector.Suspicion / 100f);
        Color target;

        if (normalizedSuspicion < 0.5f)
        {
            target = Color.Lerp(scanningColor, suspiciousColor, normalizedSuspicion / 0.5f);
        }
        else
        {
            target = Color.Lerp(suspiciousColor, detectedColor, (normalizedSuspicion - 0.5f) / 0.5f);
        }

        meshRenderer.sharedMaterial.color = target;
    }

    private readonly struct ViewCastInfo
    {
        public readonly bool hit;
        public readonly Vector3 point;
        public readonly float distance;
        public readonly float angle;

        public ViewCastInfo(bool hit, Vector3 point, float distance, float angle)
        {
            this.hit = hit;
            this.point = point;
            this.distance = distance;
            this.angle = angle;
        }
    }
}
