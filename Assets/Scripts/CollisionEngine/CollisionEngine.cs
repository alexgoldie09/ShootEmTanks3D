/*
* CollisionEngine.cs
* ----------------------------------------------------------------
* Singleton system for handling 3D collision detection between CustomCollider components.
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
    private List<CustomCollider> colliders = new List<CustomCollider>();

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
    public void RegisterCollider(CustomCollider col)
    {
        if (!colliders.Contains(col))
            colliders.Add(col);
    }
    
    // Deregisters a collider from the engine.
    public void DeregisterCollider(CustomCollider col)
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
    
    #region Dispatcher
    // Dispatches shape-based collision logic depending on collider types.
    private static void HandleCollision(CustomCollider a, CustomCollider b)
    {
        var typeA = a.colliderType;
        var typeB = b.colliderType;
        
        // Always update bounds
        a.UpdateBounds();
        b.UpdateBounds();

        // --- Sphere vs AABB ---
        if ((typeA == CustomCollider.ColliderType.SPHERE && typeB == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX) ||
            (typeA == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX && typeB == CustomCollider.ColliderType.SPHERE))
        {
            var sphere = typeA == CustomCollider.ColliderType.SPHERE ? a : b;
            var box    = typeA == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX ? a : b;

            HandleAABBSphereCollision(box, sphere);
            return;
        }

        // --- Sphere vs Sphere ---
        if (typeA == CustomCollider.ColliderType.SPHERE && typeB == CustomCollider.ColliderType.SPHERE)
        {
            HandleSphereSphereCollision(a, b);
            return;
        }
        
        // --- Player vs (Sphere, AABB, or Point) ---
        if (typeA == CustomCollider.ColliderType.PLAYER || typeB == CustomCollider.ColliderType.PLAYER)
        {
            var player = typeA == CustomCollider.ColliderType.PLAYER ? a : b;
            var other  = typeA == CustomCollider.ColliderType.PLAYER ? b : a;

            HandlePlayerCollision(player, other);
            return;
        }

        // --- Point vs (Sphere or AABB) ---
        if (typeA == CustomCollider.ColliderType.POINT || typeB == CustomCollider.ColliderType.POINT)
        {
            var point = typeA == CustomCollider.ColliderType.POINT ? a : b;
            var other = typeA == CustomCollider.ColliderType.POINT ? b : a;

            if (other.colliderType == CustomCollider.ColliderType.SPHERE)
                HandlePointToSphere(point, other);
            else if (other.colliderType == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX)
                HandlePointToAABB(point, other);
        }
    }
    #endregion

    #region Type-Based Handlers
    // Handles Sphere - Sphere Collisions
    private static void HandleSphereSphereCollision(CustomCollider a, CustomCollider b)
    {
        Coords posA = a.GetBounds().Center;
        Coords posB = b.GetBounds().Center;
        float radiusA = a.radius;
        float radiusB = b.radius;

        float distance = MathEngine.Distance(posA, posB);
        float minDistance = radiusA + radiusB;
        
        if (distance < minDistance)
        {
            // Normal pointing from B → A
            Coords normal = MathEngine.Normalize(posA - posB);

            // Penetration depth
            float penetration = minDistance - distance;

            PhysicsBody bodyA = a.GetComponent<PhysicsBody>();
            PhysicsBody bodyB = b.GetComponent<PhysicsBody>();

            // Correct positions to separate spheres (half each if both dynamic)
            if (bodyA != null && bodyB != null)
            {
                a.transform.position += (normal * (penetration * 0.5f)).ToVector3();
                b.transform.position -= (normal * (penetration * 0.5f)).ToVector3();
            }
            else if (bodyA != null)
            {
                a.transform.position += (normal * penetration).ToVector3();
            }
            else if (bodyB != null)
            {
                b.transform.position -= (normal * penetration).ToVector3();
            }

            // Apply bounce velocities
            if (bodyA != null)
                bodyA.ReflectFromNormal(normal);
            if (bodyB != null)
                bodyB.ReflectFromNormal(-normal);
        }
    }
    
    // Handles AABB - Sphere Collisions
    private static void HandleAABBSphereCollision(CustomCollider box, CustomCollider sphere)
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
            // Debug.Log($"📦 AABB ↔ ⚪ Sphere → {sphere.name} hit {box.name}");
            
            PhysicsBody body = sphere.GetComponent<PhysicsBody>();
            if (body != null)
            {
                // Find penetration depth
                var (penX, penY, penZ) = GetPenetrationDepth(center, box.GetBounds());

                // --- Ground ---
                if (penY <= penX && penY <= penZ && box.isGround)
                {
                    Coords normal = (center.y > max.y) ? new Coords(0f, 1f, 0f) : new Coords(0f, -1f, 0f);
                    float groundHeight = (normal.y > 0) ? max.y + sphere.radius : min.y - sphere.radius;
                    body.StopOnGround(normal, groundHeight);
                }
                // --- Wall X ---
                else if (penX <= penY && penX <= penZ && box.isWall)
                {
                    Coords normal = (center.x > max.x) ? new Coords(1f, 0f, 0f) : new Coords(-1f, 0f, 0f);
                    float sideX = (normal.x > 0) ? max.x + sphere.radius : min.x - sphere.radius;
                    body.StopOnWall(normal, sideX, axis: 'x');
                }
                // --- Wall Z ---
                else if (penZ <= penX && penZ <= penY && box.isWall)
                {
                    Coords normal = (center.z > max.z) ? new Coords(0f, 0f, 1f) : new Coords(0f, 0f, -1f);
                    float boundaryZ = (normal.z > 0) ? max.z + sphere.radius : min.z - sphere.radius;
                    body.StopOnWall(normal, boundaryZ, 'z');
                }
                else
                {
                    // Neutral AABB → trigger (ignore physics for now)
                    // Debug.Log($"📦 Trigger AABB {box.name} overlapped ⚪ Sphere {sphere.name}");
                }
            }
        }
    }
    
    // Handles Player Collisions
    private static void HandlePlayerCollision(CustomCollider player, CustomCollider other)
    {
        Coords playerPos = player.GetBounds().Center;

        float playerRadius = player.radius;

        if (other.colliderType == CustomCollider.ColliderType.SPHERE)
        {
            Coords otherPos = other.GetBounds().Center;
            float otherRadius = other.radius;
            float distance = MathEngine.Distance(playerPos, otherPos);

            if (distance < playerRadius + otherRadius)
            {
                PhysicsBody body = other.GetComponent<PhysicsBody>();
                if (body != null)
                {
                    Coords direction = MathEngine.Normalize(otherPos - playerPos);
                    body.ApplyImpulse(direction * 8f);
                }
            }
        }
    }
    
    // Handles Point - Sphere Collisions
    private static void HandlePointToSphere(CustomCollider point, CustomCollider sphere)
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
    
    // Handles Point - AABB Collisions
    private static void HandlePointToAABB(CustomCollider point, CustomCollider box)
    {
        Coords pointPos = point.GetBounds().Center;
        if (box.GetBounds().Contains(pointPos))
        {
            Debug.Log($"🟢 Point {point.name} is inside 📦 AABB {box.name}");
        }
    }
    #endregion
    
    #region Helper Methods
    // Calculates penetration depths along X, Y, Z axes between a point and an AABB.
    private static (float penX, float penY, float penZ) GetPenetrationDepth(Coords pos, CustomBounds bounds)
    {
        Coords min = bounds.Min;
        Coords max = bounds.Max;

        float penX = Mathf.Min(Mathf.Abs(pos.x - min.x), Mathf.Abs(pos.x - max.x));
        float penY = Mathf.Min(Mathf.Abs(pos.y - min.y), Mathf.Abs(pos.y - max.y));
        float penZ = Mathf.Min(Mathf.Abs(pos.z - min.z), Mathf.Abs(pos.z - max.z));

        return (penX, penY, penZ);
    }
    
    // Clamps a proposed position for a moving collider so it cannot penetrate walls (AABBs).
    public Coords ClampToBounds(CustomCollider moving, Coords proposedPos)
    {
        Coords corrected = proposedPos;

        foreach (var other in colliders)
        {
            if (other == moving) continue;

            // Only block against wall AABBs
            if (other.colliderType == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX && other.isWall)
            {
                Coords min = other.GetBounds().Min;
                Coords max = other.GetBounds().Max;

                // Find closest point on AABB to proposed center
                float closestX = Mathf.Clamp(proposedPos.x, min.x, max.x);
                float closestY = Mathf.Clamp(proposedPos.y, min.y, max.y);
                float closestZ = Mathf.Clamp(proposedPos.z, min.z, max.z);
                Coords closestPoint = new(closestX, closestY, closestZ);

                float distance = MathEngine.Distance(proposedPos, closestPoint);

                // If overlapping → correct along the shallowest axis
                if (distance < moving.radius)
                {
                    var (penX, penY, penZ) = GetPenetrationDepth(proposedPos, other.GetBounds());

                    if (penX <= penZ)
                    {
                        float boundaryX = (proposedPos.x > max.x)
                            ? max.x + moving.radius
                            : min.x - moving.radius;
                        corrected = new Coords(boundaryX, proposedPos.y, proposedPos.z);
                    }
                    else
                    {
                        float boundaryZ = (proposedPos.z > max.z)
                            ? max.z + moving.radius
                            : min.z - moving.radius;
                        corrected = new Coords(proposedPos.x, proposedPos.y, boundaryZ);
                    }
                }
            }
        }

        return corrected;
    }
    #endregion
}
