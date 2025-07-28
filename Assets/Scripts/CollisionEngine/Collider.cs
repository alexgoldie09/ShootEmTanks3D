/*
 * Collider.cs
 * ----------------------------------------------------------------
 * A MonoBehaviour component that represents a collision shape using a custom math system.
 *
 * PURPOSE:
 * - Serves as a unified interface for collision objects in the scene.
 * - Integrates with a central collision engine using Coords and CustomBounds.
 *
 * FEATURES:
 * - Supports POINT, AABB (box), and SPHERE types.
 * - Automatically registers with the collision engine.
 * - Calculates bounds in world space and handles Gizmo debugging.
 */

using UnityEngine;

public class Collider : MonoBehaviour
{
    // Enum to define the shape of the collider.
    public enum ColliderType
    {
        POINT,
        AXIS_ALIGNED_BOUNDING_BOX,
        SPHERE
    }

    [Header("Collider Shape Type")]
    public ColliderType colliderType = ColliderType.POINT;

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
        }
    }

    public CustomBounds GetBounds() => colliderBounds;

    public bool Contains(Coords point)
    {
        return colliderBounds.Contains(point);
    }
    #endregion

    #region Gizmo Debugging
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        UpdateBounds();

        switch (colliderType)
        {
            case ColliderType.AXIS_ALIGNED_BOUNDING_BOX:
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(colliderBounds.Center.ToVector3(), colliderBounds.Size.ToVector3());
                break;

            case ColliderType.SPHERE:
                Gizmos.color = Color.yellow;
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
