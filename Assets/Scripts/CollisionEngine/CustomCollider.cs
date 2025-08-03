/*
* Collider.cs
* ----------------------------------------------------------------
* A MonoBehaviour component that acts as a physics collider.
*
* PURPOSE:
* - Represents a physical collision shape using a custom math system (Coords, CustomBounds).
* - Automatically registers/unregisters with the central CollisionEngine.
* - Updates its bounds each frame based on its shape type and transform.
*
* FEATURES:
* - Supports POINT, SPHERE, AABB, and PLAYER collider types.
* - AABB colliders can be flagged as Ground or Wall for specialized behavior.
* - PLAYER treated as a sphere collider with custom push behavior.
* - Uses Coords and CustomBounds for custom physics math integration.
* - Visualizes bounds in the Unity Editor via Gizmos.
*/

using System;
using UnityEngine;

public class CustomCollider : MonoBehaviour
{
    // Enum to define the shape of the collider.
    public enum ColliderType
    {
        POINT,
        AXIS_ALIGNED_BOUNDING_BOX,
        SPHERE,
        PLAYER
    }

    [Header("Collider Shape Type")]
    public ColliderType colliderType = ColliderType.POINT; // Specified collider type in Unity.
    
    [Header("AABB Settings")]
    public bool isGround = false;  // If true, acts like floor.
    public bool isWall = false;    // If true, acts like wall.
    
    [Header("Player Settings")]
    [Tooltip("Horizontal offset applied to the player collider center.")]
    public float playerOffsetX = 0f;
    [Tooltip("Vertical offset applied to the player collider center.")]
    public float playerOffsetY = 1f;

    // The bounds representing this collider in world space.
    [HideInInspector] public CustomBounds colliderBounds;

    // Center point of the collider in world space.
    [HideInInspector] public Coords center;

    // Radius for sphere colliders.
    [HideInInspector] public float radius;

    #region Unity Lifecycle
    private void OnEnable()
    {
        CollisionEngine.Instance?.RegisterCollider(this);
    }

    private void OnDisable()
    {
        CollisionEngine.Instance?.DeregisterCollider(this);
    }

    private void Update()
    {
        UpdateBounds();

        // Optional: Clean-up check
        if (transform.position.y < -100f)
            Destroy(gameObject);
    }
    #endregion

    #region Bounds & Collision Setup
    // Updates the collider bounds and radius based on type and world scale
    public void UpdateBounds()
    {
        center = new Coords(transform.position);
        
        // PLAYER: apply offsets
        if (colliderType == ColliderType.PLAYER)
            center += new Coords(playerOffsetX, playerOffsetY, 0f);

        Vector3 worldSize = TryGetComponent(out Renderer rend)
            ? rend.bounds.size
            : transform.localScale;

        Coords size = new Coords(worldSize);

        switch (colliderType)
        {
            case ColliderType.POINT:
                colliderBounds = new CustomBounds(center, new Coords(0.01f, 0.01f, 0.01f)); // Tiny AABB
                radius = 0f;
                break;

            case ColliderType.AXIS_ALIGNED_BOUNDING_BOX:
                colliderBounds = new CustomBounds(center, size);
                radius = 0f;
                break;

            case ColliderType.SPHERE:
                float diameter = Mathf.Max(size.x, size.y, size.z);
                radius = diameter / 2f;
                colliderBounds = new CustomBounds(center, new Coords(diameter, diameter, diameter));
                break;
            
            case ColliderType.PLAYER:
                // Treat player as a sphere for now
                radius = size.x / 1f; 
                colliderBounds = new CustomBounds(center, new Coords(radius * 2f, radius * 2f, radius * 2f));
                break;
        }
    }

    public CustomBounds GetBounds() => colliderBounds;

    public bool Contains(Coords point) => colliderBounds.Contains(point);
    #endregion

    #region Gizmo Debugging
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        UpdateBounds();

        switch (colliderType)
        {
            case ColliderType.AXIS_ALIGNED_BOUNDING_BOX:
                if (isGround)
                {
                    Gizmos.color = Color.green;
                }
                else if (isWall)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = Color.magenta;
                }
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
