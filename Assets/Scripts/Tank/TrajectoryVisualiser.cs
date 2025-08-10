/*
 * TrajectoryVisualiser.cs
 * ----------------------------------------------------------------
 * Renders a visual prediction of the tank shell's path using physics simulation.
 *
 * PURPOSE:
 * - Simulate projectile trajectory based on current fire force and gravity.
 * - Display a line renderer showing the predicted path.
 * - Show an impact marker at the predicted collision point.
 *
 * FEATURES:
 * - Uses custom `Coords` math for projectile motion.
 * - Integrates with `CollisionEngine` for hit detection.
 * - Supports both sphere and AABB intersection checks.
 * - Dynamically updates to match the player's current fire force.
 */

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryVisualiser : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;              // Origin of projectile spawn
    public float stepTime = 0.05f;            // Time step for simulation
    public int maxSteps = 100;                // Maximum steps to simulate

    private float playerFireForce = 15f;      // Matches TankController.fireForce
    private const float GRAVITY = -9.81f;     // Gravity acceleration

    [Header("Impact Marker")]
    public GameObject impactMarkerPrefab;     // Prefab for predicted hit location
    private GameObject activeMarker;          // Instance of the impact marker

    // Cached references
    private LineRenderer lineRenderer;
    private TankController playerController;

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the line renderer, marker, and player reference.
    /// </summary>
    private void Awake()
    {
        // Set up line renderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.widthMultiplier = 0.2f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.green;

        // Create impact marker if prefab is assigned and set parent transform to this
        if (impactMarkerPrefab != null)
        {
            activeMarker = Instantiate(impactMarkerPrefab, Vector3.zero, Quaternion.identity, transform);
            activeMarker.transform.parent = transform;
            activeMarker.SetActive(false);
        }

        // Get player reference
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<TankController>();
        }
    }

    /// <summary>
    /// Updates the trajectory visual each frame.
    /// </summary>
    private void Update()
    {
        DrawTrajectory();
    }
    #endregion

    #region Trajectory Simulation
    /// <summary>
    /// Simulates and draws the projectile path, stopping at first collision.
    /// </summary>
    private void DrawTrajectory()
    {
        if (firePoint == null || CollisionEngine.Instance == null) return;

        List<Vector3> points = new List<Vector3>();

        // Sync with tank's current fire force
        if (playerController != null)
        {
            playerFireForce = playerController.fireForce;
        }

        // Initial projectile state
        Coords pos = new Coords(firePoint.position);
        Coords vel = new Coords(firePoint.forward) * playerFireForce;
        Coords acc = new Coords(0f, GRAVITY, 0f);

        // Step through simulation
        for (int i = 0; i < maxSteps; i++)
        {
            points.Add(pos.ToVector3());
            Coords prevPos = pos;

            // Integrate velocity and position
            vel += acc * stepTime;
            pos += vel * stepTime;

            // Collision detection for this segment
            foreach (var col in CollisionEngine.Instance.GetColliders())
            {
                if (col == null || col.colliderType == CustomCollider.ColliderType.POINT)
                    continue;

                // Sphere check
                if (col.colliderType == CustomCollider.ColliderType.SPHERE &&
                    CollisionEngine.Instance.SegmentIntersectsSphere(prevPos, pos, col.GetBounds().Center, col.radius, out Coords sphereHit))
                {
                    FinalizeTrajectory(points, sphereHit.ToVector3(), false);
                    return;
                }

                // AABB check
                if (col.colliderType == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX &&
                    CollisionEngine.Instance.SegmentIntersectsAABB(prevPos, pos, col.GetBounds(), out Coords boxHit))
                {
                    FinalizeTrajectory(points, boxHit.ToVector3(), col.isGround);
                    return;
                }
            }
        }

        // No hit detected
        ApplyTrajectory(points);
        if (activeMarker != null) activeMarker.SetActive(false);
    }

    /// <summary>
    /// Applies the given points to the line renderer.
    /// </summary>
    private void ApplyTrajectory(List<Vector3> points)
    {
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    /// <summary>
    /// Finalizes trajectory and places the impact marker.
    /// </summary>
    private void FinalizeTrajectory(List<Vector3> points, Vector3 hitPoint, bool isGround)
    {
        points.Add(hitPoint);
        ApplyTrajectory(points);

        if (activeMarker != null)
        {
            // Choose rotation and scale based on ground or object hit
            CustomQuaternion rotation;
            Matrix scaleMat;
            Matrix translation = MathEngine.CreateTranslationMatrix(new Coords(hitPoint));

            if (isGround)
            {
                rotation = MathEngine.Euler(-90f, -45f, 0f);
                scaleMat = MathEngine.CreateScaleMatrix(3.5f, 3.5f, 0.05f);
            }
            else
            {
                Coords forward = new Coords(firePoint.forward);
                rotation = MathEngine.LookRotation(forward, new Coords(0f, 1f, 0f));
                scaleMat = MathEngine.CreateScaleMatrix(1.5f, 1.5f, 0.05f);
            }

            // Apply to marker transform
            activeMarker.transform.position = MathEngine.ExtractPosition(translation).ToVector3();
            activeMarker.transform.rotation = rotation.ToUnityQuaternion();
            activeMarker.transform.localScale = MathEngine.ExtractScale(scaleMat).ToVector3();
            activeMarker.SetActive(true);
        }
    }
    #endregion
}
