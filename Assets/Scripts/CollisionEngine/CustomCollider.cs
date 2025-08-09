/*
 * CustomCollider.cs
 * ----------------------------------------------------------------
 * Represents a collision shape for the custom physics system.
 *
 * PURPOSE:
 * - Provide world-space bounds (AABB/Sphere/Point/Player) for collision queries.
 * - Auto-register with CollisionEngine for centralized collision management.
 * - Recompute bounds each frame from the object's transform/renderer.
 *
 * FEATURES:
 * - Supports POINT, SPHERE, AXIS_ALIGNED_BOUNDING_BOX (AABB), and PLAYER collider types.
 * - Flags for AABB as Ground/Wall/Trigger to drive gameplay and collision responses.
 * - PLAYER is currently treated as a sphere with configurable center offsets.
 * - Gizmo visualization in the Scene view for quick debugging.
 */

using System;
using UnityEngine;

public class CustomCollider : MonoBehaviour
{
    // Shape options for this collider
    public enum ColliderType
    {
        POINT,
        AXIS_ALIGNED_BOUNDING_BOX,
        SPHERE,
        PLAYER
    }

    [Header("Collider Shape Type")]
    public ColliderType colliderType = ColliderType.POINT; // Collision shape type

    [Header("AABB Flags")]
    public bool isGround = false;   // True if AABB acts as ground
    public bool isWall = false;     // True if AABB acts as wall
    public bool isTrigger = false;  // True if collider is a trigger (events only)

    [Header("Player Settings")]
    public float playerOffsetX = 0f; // Horizontal offset for player collider center
    public float playerOffsetY = 1f; // Vertical offset for player collider center

    // Computed bounds in world space (AABB representation even for spheres)
    [HideInInspector] public CustomBounds colliderBounds;

    // Cached computed center (world space)
    [HideInInspector] public Coords center;

    // Radius used for SPHERE/PLAYER types
    [HideInInspector] public float radius;

    #region Unity Lifecycle
    /// <summary>
    /// Register this collider with the CollisionEngine when enabled.
    /// </summary>
    private void OnEnable()
    {
        CollisionEngine.Instance?.RegisterCollider(this);
    }

    /// <summary>
    /// Deregister this collider when disabled.
    /// </summary>
    private void OnDisable()
    {
        CollisionEngine.Instance?.DeregisterCollider(this);
    }

    /// <summary>
    /// Recompute bounds every frame and run optional cleanup.
    /// </summary>
    private void Update()
    {
        UpdateBounds();

        // Optional housekeeping: despawn if it falls too far below the world.
        if (transform.position.y < -10f)
            Destroy(gameObject);
    }
    #endregion

    #region Bounds & Collision Setup
    /// <summary>
    /// Updates world-space center, size, radius (if applicable), and AABB based on collider type.
    /// </summary>
    public void UpdateBounds()
    {
        // Base center at transform position
        center = new Coords(transform.position);

        // PLAYER: apply configurable center offsets
        if (colliderType == ColliderType.PLAYER)
            center += new Coords(playerOffsetX, playerOffsetY, 0f);

        // Determine visual/world size:
        // - If the object has a Renderer, use its world-space bounds.
        // - Otherwise, fallback to transform.localScale.
        Vector3 worldSize = TryGetComponent(out Renderer rend)
            ? rend.bounds.size
            : transform.localScale;

        Coords size = new Coords(worldSize);

        switch (colliderType)
        {
            case ColliderType.POINT:
                // Represented as a tiny box for convenience.
                colliderBounds = new CustomBounds(center, new Coords(0.01f, 0.01f, 0.01f));
                radius = 0f;
                break;

            case ColliderType.AXIS_ALIGNED_BOUNDING_BOX:
                colliderBounds = new CustomBounds(center, size);
                radius = 0f;
                break;

            case ColliderType.SPHERE:
                // Sphere radius is based on the largest axis (enclosing sphere).
                float diameter = Mathf.Max(size.x, size.y, size.z);
                radius = diameter * 0.5f;
                colliderBounds = new CustomBounds(center, new Coords(diameter, diameter, diameter));
                break;

            case ColliderType.PLAYER:
                // Treat the player as a sphere for now.
                // NOTE: Uses size.x as the radius source (project-specific choice).
                radius = size.x / 1f;
                colliderBounds = new CustomBounds(center, new Coords(radius * 2f, radius * 2f, radius * 2f));
                break;
        }
    }

    /// <summary>
    /// Returns the current AABB representing this collider.
    /// </summary>
    public CustomBounds GetBounds() => colliderBounds;

    /// <summary>
    /// Returns true if a world-space point lies within this collider's AABB.
    /// </summary>
    public bool Contains(Coords point) => colliderBounds.Contains(point);
    #endregion

    #region Gizmo Debugging
    /// <summary>
    /// Scene view visualization for debugging collider shapes and flags.
    /// </summary>
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // Keep gizmo rendering up-to-date if values change in the Inspector.
        UpdateBounds();

        switch (colliderType)
        {
            case ColliderType.AXIS_ALIGNED_BOUNDING_BOX:
                // Color-code AABBs by role for quick visual parsing.
                if (isGround)
                    Gizmos.color = Color.green;
                else if (isWall)
                    Gizmos.color = Color.red;
                else if (isTrigger)
                    Gizmos.color = Color.yellow;
                else
                    Gizmos.color = Color.magenta;

                Gizmos.DrawWireCube(colliderBounds.Center.ToVector3(), colliderBounds.Size.ToVector3());
                break;

            case ColliderType.SPHERE:
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(colliderBounds.Center.ToVector3(), radius);
                break;

            case ColliderType.PLAYER:
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(colliderBounds.Center.ToVector3(), radius);
                break;

            case ColliderType.POINT:
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(colliderBounds.Center.ToVector3(), new Vector3(0.05f, 0.05f, 0.05f));
                break;
        }
#endif
    }
    #endregion
}