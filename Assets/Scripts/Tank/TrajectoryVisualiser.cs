using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryVisualiser : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;          // Where the shell spawns
    public float stepTime = 0.05f;       // Simulation step size (smaller = smoother)
    public int maxSteps = 100;           // Max steps in trajectory

    [Header("Physics")]
    public float fireForce = 15f;        // Launch speed, should match tank's fireForce
    private const float GRAVITY = -9.81f;
    
    [Header("Impact Marker")]
    public GameObject impactMarkerPrefab; // Prefab to show at predicted hit
    private GameObject activeMarker;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // Optional: nice settings
        lineRenderer.positionCount = 0;
        lineRenderer.widthMultiplier = 0.2f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.green;
        
        // Instantiate impact marker if prefab assigned
        if (impactMarkerPrefab != null)
        {
            activeMarker = Instantiate(impactMarkerPrefab, Vector3.zero, Quaternion.identity);
            activeMarker.SetActive(false);
        }
    }

    private void Update()
    {
        DrawTrajectory();
    }

    private void DrawTrajectory()
    {
        if (firePoint == null || CollisionEngine.Instance == null) return;

        List<Vector3> points = new List<Vector3>();

        Coords pos = new Coords(firePoint.position);
        Coords vel = new Coords(firePoint.forward) * fireForce;
        Coords acc = new Coords(0f, GRAVITY, 0f);

        for (int i = 0; i < maxSteps; i++)
        {
            points.Add(pos.ToVector3());
            Coords prevPos = pos;

            // Integrate
            vel += acc * stepTime;
            pos += vel * stepTime;

            // --- Collision check with segment (prevPos -> pos) ---
            foreach (var col in CollisionEngine.Instance.GetColliders())
            {
                if (col == null || col.colliderType == CustomCollider.ColliderType.POINT)
                    continue;

                // Sphere check
                if (col.colliderType == CustomCollider.ColliderType.SPHERE &&
                    SegmentIntersectsSphere(prevPos, pos, col.GetBounds().Center, col.radius, out Coords sphereHit))
                {
                    FinalizeTrajectory(points, sphereHit.ToVector3(), false);
                    return;
                }

                // AABB check
                if (col.colliderType == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX &&
                    SegmentIntersectsAABB(prevPos, pos, col.GetBounds(), out Coords boxHit))
                {
                    FinalizeTrajectory(points, boxHit.ToVector3(), col.isGround);
                    return;
                }
            }
        }

        // No collision hit
        ApplyTrajectory(points);
        if (activeMarker != null) activeMarker.SetActive(false);
    }

    private void ApplyTrajectory(List<Vector3> points)
    {
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    private void FinalizeTrajectory(List<Vector3> points, Vector3 hitPoint, bool isGround)
    {
        points.Add(hitPoint);
        ApplyTrajectory(points);

        if (activeMarker != null)
        {
            CustomQuaternion rotation;
            Matrix scaleMat;
            Matrix translation = MathEngine.CreateTranslationMatrix(new Coords(hitPoint));

            if (isGround)
            {
                // Ground → flat orientation + scale
                rotation = MathEngine.Euler(-90f, -45f, 0f);
                scaleMat    = MathEngine.CreateScaleMatrix(3.5f, 3.5f, 0.05f);
            }
            else
            {
                // Non-ground → look forward in tank yaw, no extra scale
                Coords forward = new Coords(firePoint.forward);
                rotation = MathEngine.LookRotation(forward, new Coords(0f, 1f, 0f));
                scaleMat    = MathEngine.CreateScaleMatrix(1.5f, 1.5f, 0.05f);
            }
            
            // Apply to Unity transform
            activeMarker.transform.position   = MathEngine.ExtractPosition(translation).ToVector3();
            activeMarker.transform.rotation   = rotation.ToUnityQuaternion();
            activeMarker.transform.localScale = MathEngine.ExtractScale(scaleMat).ToVector3();

            activeMarker.SetActive(true);
        }
    }

    #region Segment Intersection Helpers
    private bool SegmentIntersectsSphere(Coords p0, Coords p1, Coords center, float radius, out Coords hit)
    {
        Coords d = p1 - p0;
        Coords m = p0 - center;

        float a = MathEngine.Dot(d, d);
        float b = 2f * MathEngine.Dot(m, d);
        float c = MathEngine.Dot(m, m) - radius * radius;

        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
        {
            hit = Coords.Zero();
            return false;
        }

        float sqrtD = Mathf.Sqrt(discriminant);
        float t0 = (-b - sqrtD) / (2f * a);
        float t1 = (-b + sqrtD) / (2f * a);

        float t = (t0 >= 0f && t0 <= 1f) ? t0 : ((t1 >= 0f && t1 <= 1f) ? t1 : -1f);

        if (t >= 0f)
        {
            hit = p0 + d * t;
            return true;
        }

        hit = Coords.Zero();
        return false;
    }

    private bool SegmentIntersectsAABB(Coords p0, Coords p1, CustomBounds bounds, out Coords hit)
    {
        Coords min = bounds.Min;
        Coords max = bounds.Max;
        Coords dir = p1 - p0;

        float tMin = 0f;
        float tMax = 1f;

        for (int i = 0; i < 3; i++)
        {
            float origin = (i == 0) ? p0.x : (i == 1 ? p0.y : p0.z);
            float direction = (i == 0) ? dir.x : (i == 1 ? dir.y : dir.z);
            float minB = (i == 0) ? min.x : (i == 1 ? min.y : min.z);
            float maxB = (i == 0) ? max.x : (i == 1 ? max.y : max.z);

            if (Mathf.Abs(direction) < Mathf.Epsilon)
            {
                if (origin < minB || origin > maxB)
                {
                    hit = Coords.Zero();
                    return false;
                }
            }
            else
            {
                float ood = 1f / direction;
                float t1 = (minB - origin) * ood;
                float t2 = (maxB - origin) * ood;

                if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }

                tMin = Mathf.Max(tMin, t1);
                tMax = Mathf.Min(tMax, t2);

                if (tMin > tMax)
                {
                    hit = Coords.Zero();
                    return false;
                }
            }
        }

        float tHit = tMin;
        hit = p0 + dir * tHit;
        return true;
    }
    #endregion
}