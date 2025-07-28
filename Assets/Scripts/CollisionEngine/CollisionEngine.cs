/*
* CollisionEngine.cs
* ----------------------------------------------------------------
* Singleton system for handling 3D collision detection between Collider components.
* 
* PURPOSE:
* - Registers and tracks all colliders in the scene.
* - Performs efficient pairwise collision checks each frame.
* - Supports AABB, Sphere, and Point colliders.
* 
* FEATURES:
* - Collider registration/deregistration.
* - Shape-based collision dispatching.
* - Gizmo-safe logic for visual debugging.
*/

using System.Collections.Generic;
using UnityEngine;

public class CollisionEngine : MonoBehaviour
{
    // Singleton instance
    public static CollisionEngine Instance { get; private set; }

    // All active colliders
    private List<Collider> colliders = new List<Collider>();

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Update()
    {
        RunCollisionChecks();
    }
    #endregion
    
    #region Collision System
    // Registers a collider with the engine.
    public void RegisterCollider(Collider col)
    {
        if (!colliders.Contains(col))
            colliders.Add(col);
    }
    
    // Deregisters a collider from the engine.
    public void DeregisterCollider(Collider col)
    {
        if (colliders.Contains(col))
            colliders.Remove(col);
    }
    
    // Runs pairwise collision checks between all registered colliders.
    private void RunCollisionChecks()
    {
        for (int i = 0; i < colliders.Count; i++)
        {
            for (int j = i + 1; j < colliders.Count; j++)
            {
                HandleCollision(colliders[i], colliders[j]);
            }
        }
    }
    #endregion
    
    // Dispatches shape-based collision logic between two colliders.
    #region Dispatcher
    private static void HandleCollision(Collider a, Collider b)
    {
        var typeA = a.colliderType;
        var typeB = b.colliderType;
        
        a.UpdateBounds();
        b.UpdateBounds();

        // Normalize ordering if needed
        if (typeA == Collider.ColliderType.SPHERE && typeB == Collider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX)
        {
            HandleAABBSphereCollision(b, a);
        }
        else if (typeA == Collider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX && typeB == Collider.ColliderType.SPHERE)
        {
            HandleAABBSphereCollision(a, b);
        }
        else if (typeA == Collider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX && typeB == Collider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX)
        {
            HandleAABBAABBCollision(a, b);
        }
        else if (typeA == Collider.ColliderType.SPHERE && typeB == Collider.ColliderType.SPHERE)
        {
            HandleSphereSphereCollision(a, b);
        }
        else if (typeA == Collider.ColliderType.POINT || typeB == Collider.ColliderType.POINT)
        {
            HandlePointCollision(a, b);
        }
    }
    #endregion

    #region Type-Based Handlers
    private static void HandleAABBAABBCollision(Collider a, Collider b)
    {
        if (!a.GetBounds().Intersects(b.GetBounds())) return;

        Debug.Log($"📦 AABB collided with 📦 AABB → {a.name} ↔ {b.name}");
    }

    private static void HandleSphereSphereCollision(Collider a, Collider b)
    {
        Coords posA = a.GetBounds().Center;
        Coords posB = b.GetBounds().Center;
        float radiusA = a.radius;
        float radiusB = b.radius;

        float distance = MathEngine.Distance(posA, posB);
        float minDistance = radiusA + radiusB;
        
        if (distance < minDistance)
        {
            Debug.Log($"⚪ Sphere ↔ ⚪ Sphere → {a.name} hit {b.name}");

            Vector3 normal = (posA - posB).ToVector3().normalized;
            Rigidbody rbA = a.GetComponent<Rigidbody>();
            Rigidbody rbB = b.GetComponent<Rigidbody>();

            float bounceForce = 5f;

            if (rbA)
                rbA.AddForce(normal * bounceForce, ForceMode.Impulse);

            if (rbB)
                rbB.AddForce(-normal * bounceForce, ForceMode.Impulse);
        }
    }

    private static void HandleAABBSphereCollision(Collider box, Collider sphere)
    {
        Coords center = sphere.GetBounds().Center;
        Coords min = box.GetBounds().Min;
        Coords max = box.GetBounds().Max;

        float closestX = Mathf.Clamp(center.x, min.x, max.x);
        float closestY = Mathf.Clamp(center.y, min.y, max.y);
        float closestZ = Mathf.Clamp(center.z, min.z, max.z);

        Coords closestPoint = new(closestX, closestY, closestZ);
        float distance = MathEngine.Distance(center, closestPoint);

        if (distance < sphere.radius)
        {
            Debug.Log($"📦 AABB ↔ ⚪ Sphere → {sphere.name} hit {box.name}");

            Vector3 normal = (center - closestPoint).ToVector3().normalized;
            Rigidbody rb = sphere.GetComponent<Rigidbody>();

            if (rb)
            {
                float bounceForce = 5f;
                rb.AddForce(normal * bounceForce, ForceMode.Impulse);
            }
        }
    }

    private static void HandlePointCollision(Collider a, Collider b)
    {
        if (a.colliderType == Collider.ColliderType.POINT)
        {
            if (b.colliderType == Collider.ColliderType.SPHERE)
                HandlePointToSphere(a, b);
            else if (b.colliderType == Collider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX)
                HandlePointToAABB(a, b);
        }
        else if (b.colliderType == Collider.ColliderType.POINT)
        {
            if (a.colliderType == Collider.ColliderType.SPHERE)
                HandlePointToSphere(b, a);
            else if (a.colliderType == Collider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX)
                HandlePointToAABB(b, a);
        }
    }

    private static void HandlePointToSphere(Collider point, Collider sphere)
    {
        Coords pointPos = point.GetBounds().Center;
        Coords sphereCenter = sphere.GetBounds().Center;
        float radius = sphere.radius;

        float distance = MathEngine.Distance(pointPos, sphereCenter);
        if (distance <= radius)
        {
            Debug.Log($"🟢 Point {point.name} is inside ⚪ Sphere {sphere.name}");
        }
    }

    private static void HandlePointToAABB(Collider point, Collider box)
    {
        Coords pointPos = point.GetBounds().Center;
        if (box.GetBounds().Contains(pointPos))
        {
            Debug.Log($"🟢 Point {point.name} is inside 📦 AABB {box.name}");
        }
    }
    #endregion
}
