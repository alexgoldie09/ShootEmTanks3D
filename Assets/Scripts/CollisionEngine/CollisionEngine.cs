/*
 * CollisionEngine.cs
 * ----------------------------------------------------------------
 * Singleton system for handling 3D collision detection between CustomCollider components.
 *
 * PURPOSE:
 * - Register and track all custom colliders in the scene.
 * - Run pairwise collision checks and dispatch to shape-specific handlers.
 * - Provide utility raycasting and position clamping against walls.
 *
 * FEATURES:
 * - Collider registration/deregistration API.
 * - Supports AABB, Sphere, Point, and Player (sphere) colliders.
 * - Centralized raycast against all custom colliders.
 * - Scene Gizmo-safe logic (handlers are independent of Gizmos).
 */

using System.Collections.Generic;
using UnityEngine;

public class CollisionEngine : MonoBehaviour
{
    // Singleton instance
    public static CollisionEngine Instance { get; private set; }

    // All active colliders registered with the engine
    private readonly List<CustomCollider> colliders = new List<CustomCollider>();

    #region Unity Lifecycle
    /// <summary>
    /// Ensures a single instance exists (basic singleton).
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    /// <summary>
    /// Per-frame entry point for running collision checks.
    /// </summary>
    private void Update()
    {
        RunCollisionChecks();
    }
    #endregion

    #region Registration API
    /// <summary>
    /// Registers a collider with the engine (idempotent).
    /// </summary>
    public void RegisterCollider(CustomCollider col)
    {
        if (col != null && !colliders.Contains(col))
            colliders.Add(col);
    }

    /// <summary>
    /// Deregisters a collider from the engine (idempotent).
    /// </summary>
    public void DeregisterCollider(CustomCollider col)
    {
        if (col != null && colliders.Contains(col))
            colliders.Remove(col);
    }

    /// <summary>
    /// Returns the current list of colliders (live reference).
    /// </summary>
    public List<CustomCollider> GetColliders() => colliders;
    #endregion

    #region Broad Phase (Pairwise)
    /// <summary>
    /// Naive O(n²) pairwise collision loop. Dispatches to shape-specific handlers.
    /// </summary>
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

    #region Raycasting
    /// <summary>
    /// Raycasts against all registered colliders. Returns true if any collider is hit.
    /// - Optionally filters by collider type and/or ignores a specific collider.
    /// - Outputs the closest hit collider within maxDistance.
    /// </summary>
    public bool Raycast(
        Coords origin,
        Coords direction,
        out CustomCollider hit,
        float maxDistance = Mathf.Infinity,
        CustomCollider.ColliderType? filter = null,
        CustomCollider ignore = null)
    {
        hit = null;

        // Normalize to guarantee consistent distance semantics
        direction = MathEngine.Normalize(direction);
        Ray ray = new Ray(origin, direction);

        float closestDist = maxDistance;
        CustomCollider closestHit = null;

        // Test against all colliders
        foreach (var col in colliders)
        {
            if (col == null || col.colliderType == CustomCollider.ColliderType.POINT)
                continue; // skip invalid or purely point colliders for ray tests

            if (filter.HasValue && col.colliderType != filter.Value)
                continue;

            if (ignore != null && col == ignore)
                continue;

            float hitDist;

            switch (col.colliderType)
            {
                case CustomCollider.ColliderType.SPHERE:
                case CustomCollider.ColliderType.PLAYER:
                    if (RayIntersectsSphere(ray, col, out hitDist) && hitDist < closestDist)
                    {
                        closestDist = hitDist;
                        closestHit = col;
                    }
                    break;

                case CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX:
                    if (RayIntersectsAABB(ray, col, out hitDist) && hitDist < closestDist)
                    {
                        closestDist = hitDist;
                        closestHit = col;
                    }
                    break;
            }
        }

        hit = closestHit;
        return hit != null;
    }
    
    /// <summary>
    /// Ray ↔ Sphere intersection (solves quadratic for t). Returns nearest valid t ≥ 0.
    /// </summary>
    private bool RayIntersectsSphere(Ray ray, CustomCollider sphere, out float t)
    {
        Coords center = sphere.GetBounds().Center;
        float radius  = sphere.radius;

        // Ray in sphere space: (origin - center) + t * direction
        Coords oc = ray.origin - center;

        float a = MathEngine.Dot(ray.direction, ray.direction);     // should be 1 if normalized
        float b = 2.0f * MathEngine.Dot(oc, ray.direction);
        float c = MathEngine.Dot(oc, oc) - radius * radius;

        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0f)
        {
            t = -1f;
            return false;
        }

        float sqrtDisc = Mathf.Sqrt(discriminant);
        float t0 = (-b - sqrtDisc) / (2f * a);
        float t1 = (-b + sqrtDisc) / (2f * a);

        // Choose nearest non-negative root
        t = (t0 >= 0f) ? t0 : t1;
        return t >= 0f && t <= Mathf.Infinity;
    }

    /// <summary>
    /// Ray ↔ AABB intersection via slab method. Returns entry distance t if hit.
    /// </summary>
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

        // Reject if slabs don't overlap
        if (tmin > tymax || tymin > tmax)
            return false;

        // Merge X/Y intervals
        if (tymin > tmin) tmin = tymin;
        if (tymax < tmax) tmax = tymax;

        // Z slab
        float tzmin = (min.z - ray.origin.z) / ray.direction.z;
        float tzmax = (max.z - ray.origin.z) / ray.direction.z;
        if (tzmin > tzmax) Swap(ref tzmin, ref tzmax);

        if (tmin > tzmax || tzmin > tmax)
            return false;

        if (tzmin > tmin) tmin = tzmin;
        if (tzmax < tmax) tmax = tzmax;

        // Earliest valid entry
        t = tmin;
        return t >= 0f;
    }
    #endregion
    
    #region Segments
    /// <summary>
    /// Checks if a line segment intersects a sphere and outputs the hit point.
    /// Uses quadratic equation to solve for intersection between ray and sphere.
    /// </summary>
    public bool SegmentIntersectsSphere(Coords p0, Coords p1, Coords center, float radius, out Coords hit)
    {
        // Vector from p0 to p1
        Coords d = p1 - p0;
        // Vector from sphere center to p0
        Coords m = p0 - center;

        // Quadratic coefficients
        float a = MathEngine.Dot(d, d);                    // d·d
        float b = 2f * MathEngine.Dot(m, d);               // 2m·d
        float c = MathEngine.Dot(m, m) - radius * radius;  // m·m - r²

        // Discriminant check (b² - 4ac)
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
        {
            // No intersection — ray misses sphere entirely
            hit = Coords.Zero();
            return false;
        }

        // Compute potential intersection points (t0, t1) along the segment
        float sqrtD = Mathf.Sqrt(discriminant);
        float t0 = (-b - sqrtD) / (2f * a);
        float t1 = (-b + sqrtD) / (2f * a);

        // Choose the first valid t in [0, 1] range
        float t = (t0 >= 0f && t0 <= 1f) ? t0 :
                  ((t1 >= 0f && t1 <= 1f) ? t1 : -1f);

        if (t >= 0f)
        {
            // Intersection point = p0 + t*d
            hit = p0 + d * t;
            return true;
        }

        // No intersection within segment bounds
        hit = Coords.Zero();
        return false;
    }

    /// <summary>
    /// Checks if a line segment intersects an axis-aligned bounding box (AABB).
    /// Uses the "slab method" for ray-box intersection.
    /// </summary>
    public bool SegmentIntersectsAABB(Coords p0, Coords p1, CustomBounds bounds, out Coords hit)
    {
        Coords min = bounds.Min;
        Coords max = bounds.Max;
        Coords dir = p1 - p0; // Direction vector of the segment

        float tMin = 0f; // Enter time along segment
        float tMax = 1f; // Exit time along segment

        // Loop through X, Y, Z axes
        for (int i = 0; i < 3; i++)
        {
            // Pick component based on axis index
            float origin = (i == 0) ? p0.x : (i == 1 ? p0.y : p0.z);
            float direction = (i == 0) ? dir.x : (i == 1 ? dir.y : dir.z);
            float minB = (i == 0) ? min.x : (i == 1 ? min.y : min.z);
            float maxB = (i == 0) ? max.x : (i == 1 ? max.y : max.z);

            // Ray is parallel to slab
            if (Mathf.Abs(direction) < Mathf.Epsilon)
            {
                // Outside slab → no intersection
                if (origin < minB || origin > maxB)
                {
                    hit = Coords.Zero();
                    return false;
                }
            }
            else
            {
                // Compute intersection t values with near and far planes
                float ood = 1f / direction; // Inverse direction
                float t1 = (minB - origin) * ood;
                float t2 = (maxB - origin) * ood;

                // Swap if needed so t1 is near and t2 is far
                if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }

                // Expand entry and shrink exit interval
                tMin = Mathf.Max(tMin, t1);
                tMax = Mathf.Min(tMax, t2);

                // If the interval is invalid, no hit occurs
                if (tMin > tMax)
                {
                    hit = Coords.Zero();
                    return false;
                }
            }
        }

        // If we reach here, there is an intersection
        float tHit = tMin; // First intersection point along segment
        hit = p0 + dir * tHit;
        return true;
    }
    #endregion
    
    #region Dispatcher
    /// <summary>
    /// Routes a pair of colliders to the correct shape-based collision handler.
    /// </summary>
    private void HandleCollision(CustomCollider a, CustomCollider b)
    {
        var typeA = a.colliderType;
        var typeB = b.colliderType;

        // Always refresh bounds so decisions use up-to-date centers/extents
        a.UpdateBounds();
        b.UpdateBounds();

        // Sphere ↔ AABB
        if ((typeA == CustomCollider.ColliderType.SPHERE && typeB == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX) ||
            (typeA == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX && typeB == CustomCollider.ColliderType.SPHERE))
        {
            var sphere = typeA == CustomCollider.ColliderType.SPHERE ? a : b;
            var box    = typeA == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX ? a : b;
            HandleAABBSphereCollision(box, sphere);
            return;
        }

        // Sphere ↔ Sphere
        if (typeA == CustomCollider.ColliderType.SPHERE && typeB == CustomCollider.ColliderType.SPHERE)
        {
            HandleSphereSphereCollision(a, b);
            return;
        }

        // AABB ↔ AABB
        if (typeA == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX &&
            typeB == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX)
        {
            HandleAABBAABBCollision(a, b);
            return;
        }

        // Player ↔ (Sphere / AABB / Point)
        if (typeA == CustomCollider.ColliderType.PLAYER || typeB == CustomCollider.ColliderType.PLAYER)
        {
            var player = typeA == CustomCollider.ColliderType.PLAYER ? a : b;
            var other  = typeA == CustomCollider.ColliderType.PLAYER ? b : a;
            HandlePlayerCollision(player, other);
            return;
        }

        // Point ↔ (Sphere / AABB)
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
    /// <summary>
    /// AABB ↔ AABB collision handling:
    /// - Trigger crates (score and destroy)
    /// - Ground vs crate (settle on ground)
    /// - Wall vs crate (stop against wall)
    /// - Crate vs crate (basic bounce impulse)
    /// </summary>
    private void HandleAABBAABBCollision(CustomCollider a, CustomCollider b)
    {
        if (!a.GetBounds().Intersects(b.GetBounds()))
            return; // early out if not overlapping

        // Tag roles for readability
        CustomCollider ground  = null;
        CustomCollider wall    = null;
        CustomCollider trigger = null;

        CustomCollider crateA = (!a.isGround && !a.isWall && !a.isTrigger) ? a : null;
        CustomCollider crateB = (!b.isGround && !b.isWall && !b.isTrigger) ? b : null;

        if (a.isGround)  ground  = a;
        if (b.isGround)  ground  = b;

        if (a.isWall)    wall    = a;
        if (b.isWall)    wall    = b;

        if (a.isTrigger) trigger = a;
        if (b.isTrigger) trigger = b;

        // Trigger vs crate → destroy crate (score later)
        if (trigger != null)
        {
            CustomCollider other = (trigger == a) ? b : a;
            if (!other.isGround && !other.isWall && !other.isTrigger) // must be crate
            {
                GameManager.Instance.AddScore(1, false);
                Destroy(other.gameObject);
            }
            return;
        }

        // Ground vs crate → settle vertically
        if (ground != null && (crateA != null || crateB != null))
        {
            CustomCollider crate = (ground == a) ? b : a;
            PhysicsBody body = crate.GetComponent<PhysicsBody>();
            if (body == null) return;

            float groundTop  = ground.GetBounds().Max.y;
            float halfHeight = crate.GetBounds().Extents.y;

            body.StopOnGround(new Coords(0f, 1f, 0f), groundTop, halfHeight);
            return;
        }

        // Wall vs crate → stop against X/Z boundary
        if (wall != null && (crateA != null || crateB != null))
        {
            CustomCollider crate = (wall == a) ? b : a;
            PhysicsBody body = crate.GetComponent<PhysicsBody>();
            if (body == null) return;

            CustomBounds wallBounds  = wall.GetBounds();
            CustomBounds crateBounds = crate.GetBounds();
            Coords crateCenter       = crateBounds.Center;

            // Penetration depth used to choose primary axis of resolution
            var (penX, penY, penZ) = GetPenetrationDepth(crateCenter, wallBounds);

            if (penX < penZ)
            {
                // X-axis wall
                float wallEdge = (crateCenter.x < wallBounds.Center.x) ? wallBounds.Min.x : wallBounds.Max.x;
                Coords normal  = (crateCenter.x < wallBounds.Center.x) ? new Coords(-1f, 0f, 0f) : new Coords(1f, 0f, 0f);
                body.StopOnWall(normal, wallEdge, 'x', crateBounds.Extents.x);
            }
            else
            {
                // Z-axis wall
                float wallEdge = (crateCenter.z < wallBounds.Center.z) ? wallBounds.Min.z : wallBounds.Max.z;
                Coords normal  = (crateCenter.z < wallBounds.Center.z) ? new Coords(0f, 0f, -1f) : new Coords(0f, 0f, 1f);
                body.StopOnWall(normal, wallEdge, 'z', crateBounds.Extents.z);
            }
            return;
        }

        // Crate vs crate → simple opposing impulses along center-to-center axis
        if (crateA != null && crateB != null)
        {
            PhysicsBody bodyA = crateA.GetComponent<PhysicsBody>();
            PhysicsBody bodyB = crateB.GetComponent<PhysicsBody>();

            if (bodyA != null && bodyB != null)
            {
                Coords dir = MathEngine.Normalize(crateB.GetBounds().Center - crateA.GetBounds().Center);
                Coords relativeVelocity = bodyB.GetVelocity() - bodyA.GetVelocity();

                // Reflect relative velocity along collision normal to create a simple impulse
                Coords impulse = MathEngine.Reflect(relativeVelocity, dir);
                bodyA.ApplyImpulse(-impulse * 0.1f);
                bodyB.ApplyImpulse( impulse * 0.1f);
            }
        }
    }

    /// <summary>
    /// Sphere ↔ Sphere resolution (split correction using ResolveSphereCollision).
    /// </summary>
    private void HandleSphereSphereCollision(CustomCollider a, CustomCollider b)
    {
        Coords posA   = a.GetBounds().Center;
        Coords posB   = b.GetBounds().Center;
        float radiusA = a.radius;
        float radiusB = b.radius;

        float distance = MathEngine.Distance(posA, posB);
        float overlap  = (radiusA + radiusB) - distance;

        if (overlap > 0f)
        {
            // Normal direction from A → B (fallback when co-located)
            Coords normal = distance > 0f ? MathEngine.Normalize(posB - posA) : new Coords(1, 0, 0);

            PhysicsBody bodyA = a.GetComponent<PhysicsBody>();
            PhysicsBody bodyB = b.GetComponent<PhysicsBody>();

            // Split correction evenly between both bodies
            if (bodyA != null) bodyA.ResolveSphereCollision(-normal, overlap * 0.5f, bodyB);
            if (bodyB != null) bodyB.ResolveSphereCollision( normal, overlap * 0.5f, bodyA);
        }
    }

    /// <summary>
    /// AABB ↔ Sphere handling:
    /// - Ground: settle on top/bottom
    /// - Wall: stop at X/Z faces
    /// - Neutral AABB: currently treated like trigger (no physics)
    /// </summary>
    private void HandleAABBSphereCollision(CustomCollider box, CustomCollider sphere)
    {
        Coords center = sphere.GetBounds().Center;
        Coords min    = box.GetBounds().Min;
        Coords max    = box.GetBounds().Max;

        // Closest point on AABB to sphere center
        float closestX = Mathf.Clamp(center.x, min.x, max.x);
        float closestY = Mathf.Clamp(center.y, min.y, max.y);
        float closestZ = Mathf.Clamp(center.z, min.z, max.z);
        Coords closestPoint = new Coords(closestX, closestY, closestZ);

        // Overlap test
        float distance = MathEngine.Distance(center, closestPoint);
        if (distance >= sphere.radius)
            return;

        PhysicsBody body = sphere.GetComponent<PhysicsBody>();
        if (body == null) return;

        // Choose primary penetration axis for resolution
        var (penX, penY, penZ) = GetPenetrationDepth(center, box.GetBounds());

        // Ground (Y-axis) handling
        if (penY <= penX && penY <= penZ && box.isGround)
        {
            Coords normal = (center.y > max.y) ? new Coords(0f, 1f, 0f) : new Coords(0f, -1f, 0f);
            float groundHeight = (normal.y > 0f) ? max.y : min.y;
            body.StopOnGround(normal, groundHeight, sphere.radius);
        }
        // X-wall handling
        else if (penX <= penY && penX <= penZ && box.isWall)
        {
            Coords normal = (center.x > max.x) ? new Coords(1f, 0f, 0f) : new Coords(-1f, 0f, 0f);
            float sideX = (normal.x > 0f) ? max.x : min.x;
            body.StopOnWall(normal, sideX, 'x', sphere.radius);
        }
        // Z-wall handling
        else if (penZ <= penX && penZ <= penY && box.isWall)
        {
            Coords normal = (center.z > max.z) ? new Coords(0f, 0f, 1f) : new Coords(0f, 0f, -1f);
            float sideZ = (normal.z > 0f) ? max.z : min.z;
            body.StopOnWall(normal, sideZ, 'z', sphere.radius);
        }
        // Neutral AABB → currently acts like a trigger (no physics here)
    }

    /// <summary>
    /// Player ↔ (Sphere/AABB/Point) interactions:
    /// - Player vs Sphere: knock sphere away, trigger game over and FX.
    /// - Player vs AABB (non-ground, non-wall): push AABB if it has PhysicsBody.
    /// </summary>
    private void HandlePlayerCollision(CustomCollider player, CustomCollider other)
    {
        Coords playerPos   = player.GetBounds().Center;
        float  playerRadius = player.radius;

        // Player ↔ Sphere
        if (other.colliderType == CustomCollider.ColliderType.SPHERE)
        {
            Coords otherPos   = other.GetBounds().Center;
            float  otherRadius = other.radius;

            if (MathEngine.Distance(playerPos, otherPos) < playerRadius + otherRadius)
            {
                // Knock sphere away (if it has physics)
                PhysicsBody body = other.GetComponent<PhysicsBody>();
                if (body != null)
                {
                    Coords direction = MathEngine.Normalize(otherPos - playerPos);
                    body.ApplyImpulse(direction * 8f);
                }

                // Player death: FX + game over
                player.gameObject.SetActive(false);
                var tc = player.gameObject.GetComponent<TankController>();
                if (tc != null && tc.destroyFx != null)
                    Object.Instantiate(tc.destroyFx, player.transform.position, Quaternion.identity);

                GameManager.Instance.TriggerGameOver();
            }
        }
        // Player ↔ AABB (only for neutral crates)
        else if (other.colliderType == CustomCollider.ColliderType.AXIS_ALIGNED_BOUNDING_BOX &&
                 !other.isGround && !other.isWall)
        {
            // Project player sphere center onto box
            Coords closestPoint = new Coords(
                Mathf.Clamp(playerPos.x, other.GetBounds().Min.x, other.GetBounds().Max.x),
                Mathf.Clamp(playerPos.y, other.GetBounds().Min.y, other.GetBounds().Max.y),
                Mathf.Clamp(playerPos.z, other.GetBounds().Min.z, other.GetBounds().Max.z)
            );

            // Push the box if within radius
            if (MathEngine.Distance(playerPos, closestPoint) < playerRadius)
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

    /// <summary>
    /// Point ↔ Sphere: destroy point and damage the sphere's Enemy component if present.
    /// </summary>
    private void HandlePointToSphere(CustomCollider point, CustomCollider sphere)
    {
        Coords pointPos    = point.GetBounds().Center;
        Coords sphereCenter = sphere.GetBounds().Center;
        float radius       = sphere.radius;

        if (MathEngine.Distance(pointPos, sphereCenter) <= radius)
        {
            // Remove the point (e.g., projectile) and damage the enemy
            Object.Destroy(point.gameObject);
            var enemy = sphere.GetComponent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(1);
        }
    }

    /// <summary>
    /// Point ↔ AABB: apply an impulse to the box (if it has PhysicsBody) and destroy the point.
    /// </summary>
    private void HandlePointToAABB(CustomCollider point, CustomCollider box)
    {
        Coords pointPos = point.GetBounds().Center;

        if (box.GetBounds().Contains(pointPos))
        {
            PhysicsBody body      = box.GetComponent<PhysicsBody>();
            PhysicsBody pointBody = point.GetComponent<PhysicsBody>();

            if (body != null && pointBody != null)
            {
                // Direction from point to box center; impulse magnitude from projectileForce
                Coords direction = MathEngine.Normalize(box.GetBounds().Center - pointPos);
                body.ApplyImpulse(direction * pointBody.projectileForce);
            }

            Object.Destroy(point.gameObject);
        }
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Computes penetration distances along X, Y, Z from a position into an AABB.
    /// Used to decide the primary axis of resolution.
    /// </summary>
    private (float penX, float penY, float penZ) GetPenetrationDepth(Coords pos, CustomBounds bounds)
    {
        Coords min = bounds.Min;
        Coords max = bounds.Max;

        float penX = Mathf.Min(Mathf.Abs(pos.x - min.x), Mathf.Abs(pos.x - max.x));
        float penY = Mathf.Min(Mathf.Abs(pos.y - min.y), Mathf.Abs(pos.y - max.y));
        float penZ = Mathf.Min(Mathf.Abs(pos.z - min.z), Mathf.Abs(pos.z - max.z));

        return (penX, penY, penZ);
    }

    /// <summary>
    /// Clamps a proposed position for a moving sphere against wall AABBs (prevents penetration).
    /// </summary>
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

                // Closest point on the wall box to the moving sphere center
                float closestX = Mathf.Clamp(proposedPos.x, min.x, max.x);
                float closestY = Mathf.Clamp(proposedPos.y, min.y, max.y);
                float closestZ = Mathf.Clamp(proposedPos.z, min.z, max.z);
                Coords closestPoint = new Coords(closestX, closestY, closestZ);

                // If we're overlapping, push out along the contact normal
                float distance = MathEngine.Distance(proposedPos, closestPoint);
                if (distance < moving.radius)
                {
                    float penetration = moving.radius - distance;
                    Coords normal = MathEngine.Normalize(proposedPos - closestPoint); // from wall → sphere
                    corrected += normal * penetration;
                }
            }
        }

        return corrected;
    }

    /// <summary>
    /// Utility: swaps two floats (used by slab method).
    /// </summary>
    private void Swap(ref float a, ref float b)
    {
        float tmp = a;
        a = b;
        b = tmp;
    }
    #endregion
}