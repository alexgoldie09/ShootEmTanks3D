/*
 * CustomBounds.cs
 * ----------------------------------------------------------------
 * Represents an axis-aligned bounding box (AABB) using Coords.
 *
 * PURPOSE:
 * - Provides a simple and efficient volume for spatial reasoning.
 * - Used in collision detection for overlap, containment, and visualization.
 *
 * FEATURES:
 * - Holds center and size in world space.
 * - Computes extents, min, max from size.
 * - Includes overlap and point containment checks.
 * - Editor Gizmo support for wireframe debug rendering.
 */

using UnityEngine;

public class CustomBounds
{
    #region Bounds Fields
    // The center point of the bounds in world space.
    public Coords Center { get; private set; }
    
    // The total size (width, height, depth) of the bounds.
    public Coords Size { get; private set; }
    
    // Half of the size in each axis direction.
    public Coords Extents => new Coords(Size.x / 2f, Size.y / 2f, Size.z / 2f);
    
    // The minimum corner of the bounds.
    public Coords Min => Center - Extents;
    
    // The maximum corner of the bounds.
    public Coords Max => Center + Extents;
    #endregion
    
    #region Bounds Methods
    // Creates a new CustomBounds from a center and size.
    public CustomBounds(Coords center, Coords size)
    {
        Center = center;
        Size = size;
    }
    
    // Updates the bounds' center and size.
    public void Set(Coords newCenter, Coords newSize)
    {
        Center = newCenter;
        Size = newSize;
    }
    
    // Returns true if the bounds contain the given point.
    public bool Contains(Coords point)
    {
        Coords min = Min;
        Coords max = Max;

        return (point.x >= min.x && point.x <= max.x) &&
               (point.y >= min.y && point.y <= max.y) &&
               (point.z >= min.z && point.z <= max.z);
    }
    
    // Returns true if this bounds overlaps with another bounds.
    public bool Intersects(CustomBounds other)
    {
        Coords aMin = this.Min;
        Coords aMax = this.Max;
        Coords bMin = other.Min;
        Coords bMax = other.Max;

        return (aMin.x <= bMax.x && aMax.x >= bMin.x) &&
               (aMin.y <= bMax.y && aMax.y >= bMin.y) &&
               (aMin.z <= bMax.z && aMax.z >= bMin.z);
    }
    #endregion
    
    #region Debugging
    
    // String representation for debugging.
    public override string ToString() => $"Center: {Center}, Size: {Size}";
    #endregion
}

