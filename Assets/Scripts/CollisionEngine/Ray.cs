/*
 * Ray.cs
 * ----------------------------------------------------------------
 * Represents a mathematical ray with an origin and a direction.
 *
 * PURPOSE:
 * - Used for custom raycasting in the physics/collision engine.
 * - Provides a simple way to calculate a point along the ray at a given distance.
 *
 * FEATURES:
 * - Always stores direction as a normalized vector.
 * - Supports point calculation using ray origin + direction * distance.
 */

public struct Ray
{
    // Starting position of the ray in world space
    public Coords origin;

    // Direction of the ray (normalized to length 1)
    public Coords direction;

    #region Constructors
    /// <summary>
    /// Creates a ray from an origin and direction.
    /// Direction is normalized automatically.
    /// </summary>
    public Ray(Coords origin, Coords direction)
    {
        this.origin = origin;
        this.direction = MathEngine.Normalize(direction);
    }
    #endregion

    #region Accessors
    /// <summary>
    /// Returns a point along the ray at the specified distance from the origin.
    /// </summary>
    public Coords GetPoint(float distance)
    {
        return origin + direction * distance;
    }
    #endregion
}