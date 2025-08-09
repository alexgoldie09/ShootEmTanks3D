/*
 * CustomBounds.cs
 * ----------------------------------------------------------------
 * Represents an axis-aligned bounding box (AABB) using Coords.
 *
 * PURPOSE:
 * - Provide a simple, efficient volume for spatial reasoning and queries.
 * - Support collision detection for overlap, containment, and debug display.
 *
 * FEATURES:
 * - Stores center and size in world space.
 * - Computes extents, min, and max on the fly.
 * - Overlap test (AABB vs AABB) and point containment.
 * - String dump for quick debugging.
 */

public class CustomBounds
{
    #region Fields & Properties
    /// <summary>
    /// Center point of the bounds in world space.
    /// </summary>
    public Coords Center { get; private set; }

    /// <summary>
    /// Total size (width, height, depth) of the bounds.
    /// </summary>
    public Coords Size { get; private set; }

    /// <summary>
    /// Half of the size along each axis.
    /// </summary>
    public Coords Extents => new Coords(Size.x * 0.5f, Size.y * 0.5f, Size.z * 0.5f);

    /// <summary>
    /// Minimum corner (Center - Extents).
    /// </summary>
    public Coords Min => Center - Extents;

    /// <summary>
    /// Maximum corner (Center + Extents).
    /// </summary>
    public Coords Max => Center + Extents;
    #endregion

    #region Construction & Mutation
    /// <summary>
    /// Create a new AABB from center and size.
    /// </summary>
    public CustomBounds(Coords center, Coords size)
    {
        Center = center;
        Size = size;
    }

    /// <summary>
    /// Update the bounds with a new center and size.
    /// </summary>
    public void Set(Coords newCenter, Coords newSize)
    {
        Center = newCenter;
        Size = newSize;
    }
    #endregion

    #region Queries
    /// <summary>
    /// Returns true if the bounds contain the given point (inclusive on faces).
    /// </summary>
    public bool Contains(Coords point)
    {
        // Compute corners once to avoid recomputation per axis.
        Coords min = Min;
        Coords max = Max;

        // Point-in-AABB test (inclusive).
        return (point.x >= min.x && point.x <= max.x) &&
               (point.y >= min.y && point.y <= max.y) &&
               (point.z >= min.z && point.z <= max.z);
    }

    /// <summary>
    /// Returns true if this AABB overlaps another AABB (inclusive on faces).
    /// </summary>
    public bool Intersects(CustomBounds other)
    {
        // Grab corners once for both boxes.
        Coords aMin = this.Min;
        Coords aMax = this.Max;
        Coords bMin = other.Min;
        Coords bMax = other.Max;

        // Separating Axis Theorem for AABBs reduces to interval overlap on each axis.
        return (aMin.x <= bMax.x && aMax.x >= bMin.x) &&
               (aMin.y <= bMax.y && aMax.y >= bMin.y) &&
               (aMin.z <= bMax.z && aMax.z >= bMin.z);
    }
    #endregion

    #region Debugging
    /// <summary>
    /// Human-readable summary for logs and inspectors.
    /// </summary>
    public override string ToString() => $"Center: {Center}, Size: {Size}";
    #endregion
}
