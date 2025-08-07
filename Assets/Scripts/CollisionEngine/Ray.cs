/*
 * Ray.cs
 * ------------------------------------------------------------
 * Represents a mathematical ray with origin and direction.
 *
 * PURPOSE:
 * - Used for custom raycasting in the physics/collision engine.
 *
 */

public struct Ray
{
    public Coords origin;
    public Coords direction;

    public Ray(Coords origin, Coords direction)
    {
        this.origin = origin;
        this.direction = MathEngine.Normalize(direction);
    }

    public Coords GetPoint(float distance)
    {
        return origin + direction * distance;
    }
}
