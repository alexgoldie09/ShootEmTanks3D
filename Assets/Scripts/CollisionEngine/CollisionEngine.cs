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
using System.Linq;
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
    
    // Get the list of colliders.
    public List<CustomCollider> GetColliders() => colliders;
    
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

    // Performs a raycast against all colliders in the scene.
    public bool Raycast(Coords origin, Coords direction, out CustomCollider hit, float maxDistance = Mathf.Infinity, CustomCollider.ColliderType? filter = null, CustomCollider ignore = null)
    {
        // Output hit collider (null by default)
        hit = null;

        // Normalize the direction to ensure consistent ray length
        direction = MathEngine.Normalize(direction);

        // Create a ray struct (custom class) with origin and direction
        Ray ray = new Ray(origin, direction);

        // Track the closest collision hit within range
        float closestDist = maxDistance;
        CustomCollider closestHit = null;

        // Check against all registered colliders
        foreach (var col in colliders)
        {
            // Skip null, point, filtered-out, or ignored colliders
            if (col == null || col.colliderType == CustomCollider.ColliderType.POINT)
                continue;

            if (filter.HasValue && col.colliderType != filter.Value)
                continue;

            if (ignore != null && col == ignore)
                continue;

            float hitDist = -1f; // Distance to hit (set inside intersection test)

            switch (col.colliderType)
            {
                case CustomCollider.ColliderType.SPHERE:
                    // If ray intersects a sphere and it's the closest so far, store it
                    if (RayIntersectsSphere(ray, col, out hitDist) && hitDist < closestDist)
                    {
                        closestDist = hitDist;
                        closestHit = col;
                    }
                    break;

                case CustomCollider.ColliderType.PLAYER:
                    // If ray intersects a player (sphere collider) and it's the closest so far, store it
                    if (RayIntersectsSphere(ray, col, out hitDist) && hitDist < closestDist)
                    {
                        closestDist = hitDist;
                        closestHit = col;
                    }
                    break;

                case CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX:
                    // If ray intersects AABB and it's the closest so far, store it
                    if (RayIntersectsAABB(ray, col, out hitDist) && hitDist < closestDist)
                    {
                        closestDist = hitDist;
                        closestHit = col;
                    }
                    break;
            }
        }

        // Set final hit if any valid collider was intersected
        hit = closestHit;

        // Return true if something was hit
        return hit != null;
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
        
        // --- AABB vs AABB ---
        if (typeA == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX &&
            typeB == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX)
        {
            var boxA = a;
            var boxB = b;

            HandleAABBAABBCollision(boxA, boxB);
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
    // Handle AABB - AABB Collisions 
    private static void HandleAABBAABBCollision(CustomCollider a, CustomCollider b)
    {
        // Skip if not overlapping
        if (!a.GetBounds().Intersects(b.GetBounds()))
            return;

        // Determine roles
        CustomCollider ground = null;
        CustomCollider wall = null;
        CustomCollider trigger = null;
        CustomCollider crateA = (!a.isGround && !a.isWall && !a.isTrigger) ? a : null;
        CustomCollider crateB = (!b.isGround && !b.isWall && !b.isTrigger) ? b : null;

        if (a.isGround) ground = a;
        if (b.isGround) ground = b;

        if (a.isWall) wall = a;
        if (b.isWall) wall = b;

        if (a.isTrigger) trigger = a;
        if (b.isTrigger) trigger = b;

        // If either collider is a trigger, destroy the other crate (score later)
        if (trigger != null)
        {
            CustomCollider other = (trigger == a) ? b : a;
            if (!other.isGround && !other.isWall && !other.isTrigger) // Must be a crate
            {
                GameManager.Instance.AddScore(1);
                Destroy(other.gameObject);  // Destroy the crate
            }
            return;
        }

        // Ground vs Crate
        if (ground != null && (crateA != null || crateB != null))
        {
            CustomCollider crate = (ground == a) ? b : a;
            PhysicsBody body = crate.GetComponent<PhysicsBody>();
            if (body == null) return;

            float groundTop = ground.GetBounds().Max.y;
            float halfHeight = crate.GetBounds().Extents.y;

            body.StopOnGround(new Coords(0f, 1f, 0f), groundTop, halfHeight);
            return;
        }

        // Wall vs Crate
        if (wall != null && (crateA != null || crateB != null))
        {
            CustomCollider crate = (wall == a) ? b : a;
            PhysicsBody body = crate.GetComponent<PhysicsBody>();
            if (body == null) return;

            CustomBounds wallBounds = wall.GetBounds();
            CustomBounds crateBounds = crate.GetBounds();
            Coords crateCenter = crateBounds.Center;

            var (penX, penY, penZ) = GetPenetrationDepth(crateCenter, wallBounds);

            // Decide which axis is the main penetration
            if (penX < penZ)
            {
                // X-axis wall
                float wallEdge = (crateCenter.x < wallBounds.Center.x) ? wallBounds.Min.x : wallBounds.Max.x;
                Coords normal = (crateCenter.x < wallBounds.Center.x) ? new Coords(-1f, 0f, 0f) : new Coords(1f, 0f, 0f);
                body.StopOnWall(normal, wallEdge, 'x', crateBounds.Extents.x);
            }
            else
            {
                // Z-axis wall
                float wallEdge = (crateCenter.z < wallBounds.Center.z) ? wallBounds.Min.z : wallBounds.Max.z;
                Coords normal = (crateCenter.z < wallBounds.Center.z) ? new Coords(0f, 0f, -1f) : new Coords(0f, 0f, 1f);
                body.StopOnWall(normal, wallEdge, 'z', crateBounds.Extents.z);
            }

            return;
        }

        // Crate vs Crate bounce
        if (crateA != null && crateB != null)
        {
            PhysicsBody bodyA = crateA.GetComponent<PhysicsBody>();
            PhysicsBody bodyB = crateB.GetComponent<PhysicsBody>();

            if (bodyA != null && bodyB != null)
            {
                Coords dir = MathEngine.Normalize(crateB.GetBounds().Center - crateA.GetBounds().Center);
                Coords relativeVelocity = bodyB.GetVelocity() - bodyA.GetVelocity();

                // Reflect relative velocity along normal
                Coords impulse = MathEngine.Reflect(relativeVelocity, dir);
                bodyA.ApplyImpulse(-impulse * 0.1f);
                bodyB.ApplyImpulse(impulse * 0.1f);
            }
        }
    }

    // Handles Sphere - Sphere Collisions
    private static void HandleSphereSphereCollision(CustomCollider a, CustomCollider b)
    {
        Coords posA = a.GetBounds().Center;
        Coords posB = b.GetBounds().Center;
        float radiusA = a.radius;
        float radiusB = b.radius;

        float distance = MathEngine.Distance(posA, posB);
        float overlap = (radiusA + radiusB) - distance;

        if (overlap > 0f)
        {
            Coords normal = MathEngine.Normalize(posB - posA);
            if (distance == 0) normal = new Coords(1, 0, 0); // fallback

            PhysicsBody bodyA = a.GetComponent<PhysicsBody>();
            PhysicsBody bodyB = b.GetComponent<PhysicsBody>();

            // Split correction
            if (bodyA != null) bodyA.ResolveSphereCollision(-normal, overlap / 2f, bodyB);
            if (bodyB != null) bodyB.ResolveSphereCollision(normal, overlap / 2f, bodyA);
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
            PhysicsBody body = sphere.GetComponent<PhysicsBody>();
            if (body != null)
            {
                // Find penetration depth
                var (penX, penY, penZ) = GetPenetrationDepth(center, box.GetBounds());

                // Ground
                if (penY <= penX && penY <= penZ && box.isGround)
                {
                    Coords normal = (center.y > max.y) ? new Coords(0f, 1f, 0f) : new Coords(0f, -1f, 0f);
                    float groundHeight = (normal.y > 0) ? max.y : min.y;
                    body.StopOnGround(normal, groundHeight, sphere.radius); // unified version
                }
                // Wall X
                else if (penX <= penY && penX <= penZ && box.isWall)
                {
                    Coords normal = (center.x > max.x) ? new Coords(1f, 0f, 0f) : new Coords(-1f, 0f, 0f);
                    float sideX = (normal.x > 0) ? max.x : min.x;
                    body.StopOnWall(normal, sideX, 'x', sphere.radius);
                }
                // Wall Z
                else if (penZ <= penX && penZ <= penY && box.isWall)
                {
                    Coords normal = (center.z > max.z) ? new Coords(0f, 0f, 1f) : new Coords(0f, 0f, -1f);
                    float boundaryZ = (normal.z > 0) ? max.z : min.z;
                    body.StopOnWall(normal, boundaryZ, 'z', sphere.radius);
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

        // --- Player vs Sphere (Enemy) ---
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
                player.gameObject.SetActive(false);
                GameManager.Instance.TriggerGameOver();
            }
        }

        // --- Player vs AABB (but not ground/wall) ---
        else if (other.colliderType == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX &&
                 !other.isGround && !other.isWall)
        {
            // Check overlap: player sphere vs box
            Coords closestPoint = new Coords(
                Mathf.Clamp(playerPos.x, other.GetBounds().Min.x, other.GetBounds().Max.x),
                Mathf.Clamp(playerPos.y, other.GetBounds().Min.y, other.GetBounds().Max.y),
                Mathf.Clamp(playerPos.z, other.GetBounds().Min.z, other.GetBounds().Max.z)
            );

            float distance = MathEngine.Distance(playerPos, closestPoint);
            if (distance < playerRadius)
            {
                PhysicsBody body = other.GetComponent<PhysicsBody>();
                if (body != null)
                {
                    Coords direction = MathEngine.Normalize(other.GetBounds().Center - playerPos);
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
            // Destroy both objects
            Destroy(point.gameObject);
            if (sphere.GetComponent<Enemy>() != null)
            {
                sphere.GetComponent<Enemy>().TakeDamage(1);
            }
        }
    }
    
    // Handles Point - AABB Collisions
    private static void HandlePointToAABB(CustomCollider point, CustomCollider box)
    {
        Coords pointPos = point.GetBounds().Center;
        if (box.GetBounds().Contains(pointPos))
        {
            // Add impulse to AABB if it has a PhysicsBody
            PhysicsBody body = box.GetComponent<PhysicsBody>();
            PhysicsBody pointBody = point.GetComponent<PhysicsBody>();
            if (body != null && pointBody != null)
            {
                // Compute a simple impulse direction from point to box center
                Coords direction = MathEngine.Normalize(box.GetBounds().Center - pointPos);
                
                body.ApplyImpulse(direction * pointBody.projectileForce);
            }
            
            // Destroy point
            Destroy(point.gameObject);
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

            if (other.colliderType == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX && other.isWall)
            {
                Coords min = other.GetBounds().Min;
                Coords max = other.GetBounds().Max;

                // Closest point on box to sphere center
                float closestX = Mathf.Clamp(proposedPos.x, min.x, max.x);
                float closestY = Mathf.Clamp(proposedPos.y, min.y, max.y);
                float closestZ = Mathf.Clamp(proposedPos.z, min.z, max.z);
                Coords closestPoint = new Coords(closestX, closestY, closestZ);

                // Distance to closest point
                float distance = MathEngine.Distance(proposedPos, closestPoint);

                if (distance < moving.radius)
                {
                    // Penetration depth
                    float penetration = moving.radius - distance;

                    // Correction direction is from box → sphere
                    Coords normal = MathEngine.Normalize(proposedPos - closestPoint);

                    // Apply correction outward
                    corrected += normal * penetration;
                }
            }
        }

        return corrected;
    }

    // Returns true if the ray intersects a sphere.
    private bool RayIntersectsSphere(Ray ray, CustomCollider sphere, out float t)
    {
        // Sphere center and radius
        Coords center = sphere.GetBounds().Center;
        float radius = sphere.radius;

        // Vector from ray origin to sphere center
        Coords oc = ray.origin - center;

        // Quadratic coefficients
        float a = MathEngine.Dot(ray.direction, ray.direction);
        float b = 2.0f * MathEngine.Dot(oc, ray.direction);
        float c = MathEngine.Dot(oc, oc) - radius * radius;

        // Discriminant of the quadratic
        float discriminant = b * b - 4 * a * c;

        // No intersection if discriminant is negative
        if (discriminant < 0)
        {
            t = -1f;
            return false;
        }

        // Solve for t (distance)
        float sqrtDisc = Mathf.Sqrt(discriminant);
        float t0 = (-b - sqrtDisc) / (2f * a);
        float t1 = (-b + sqrtDisc) / (2f * a);

        // Return the nearest positive root
        t = (t0 >= 0) ? t0 : t1;
        return t >= 0 && t <= Mathf.Infinity;
    }

    // Returns true if the ray intersects an axis-aligned bounding box (AABB).
    private bool RayIntersectsAABB(Ray ray, CustomCollider box, out float t)
    {
        Coords min = box.GetBounds().Min;
        Coords max = box.GetBounds().Max;
        t = -1f;

        // X slab
        float tmin = (min.x - ray.origin.x) / ray.direction.x;
        float tmax = (max.x - ray.origin.x) / ray.direction.x;
        if (tmin > tmax) Swap(ref tmin, ref tmax);

        // Y slab
        float tymin = (min.y - ray.origin.y) / ray.direction.y;
        float tymax = (max.y - ray.origin.y) / ray.direction.y;
        if (tymin > tymax) Swap(ref tymin, ref tymax);

        // Early exit if slabs do not overlap
        if ((tmin > tymax) || (tymin > tmax))
            return false;

        // Update tmin/tmax to account for Y overlap
        if (tymin > tmin) tmin = tymin;
        if (tymax < tmax) tmax = tymax;

        // Z slab
        float tzmin = (min.z - ray.origin.z) / ray.direction.z;
        float tzmax = (max.z - ray.origin.z) / ray.direction.z;
        if (tzmin > tzmax) Swap(ref tzmin, ref tzmax);

        // Final overlap check
        if ((tmin > tzmax) || (tzmin > tmax))
            return false;

        // Final tmin/tmax update for Z
        if (tzmin > tmin) tmin = tzmin;
        if (tzmax < tmax) tmax = tzmax;

        // Output the closest entry distance
        t = tmin;
        return t >= 0;
    }

    // Utility method to swap two float values (used in slab axis checks)
    private void Swap(ref float a, ref float b)
    {
        float temp = a;
        a = b;
        b = temp;
    }
    #endregion
}
