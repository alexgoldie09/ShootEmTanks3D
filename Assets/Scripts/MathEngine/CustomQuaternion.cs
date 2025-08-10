/*
 * CustomQuaternion.cs
 * ----------------------------------------------------------------
 * A lightweight, immutable data container for quaternions.
 *
 * PURPOSE:
 * - Represent rotations as unit quaternions.
 * - Enable quaternion arithmetic (multiplication, normalization).
 * - Provide helper conversion methods (e.g., ToMatrix / FromMatrix).
 *
 * FEATURES:
 * - Immutable struct for safety and value semantics.
 * - Constructors from raw components or axis-angle (degrees).
 * - Quaternion * Quaternion and Quaternion * Coords multiplication.
 * - Unity interop via ToUnityQuaternion().
 * - Designed to be used alongside MathEngine for all practical operations.
 */

using UnityEngine;

public readonly struct CustomQuaternion
{
    // Public fields representing the quaternion parts (x*i + y*j + z*k + w).
    public readonly float x;
    public readonly float y;
    public readonly float z;
    public readonly float w;

    #region Constructors
    /// <summary>
    /// Constructs a quaternion directly from its components.
    /// </summary>
    public CustomQuaternion(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    /// <summary>
    /// Constructs a quaternion from an axis (Coords) and an angle in degrees.
    /// </summary>
    public CustomQuaternion(Coords axis, float angleDegrees)
    {
        Coords norm = MathEngine.Normalize(axis);
        float radians = angleDegrees * Mathf.Deg2Rad;
        float halfAngle = radians * 0.5f;

        w = Mathf.Cos(halfAngle);
        float s = Mathf.Sin(halfAngle);
        x = norm.x * s;
        y = norm.y * s;
        z = norm.z * s;
    }
    #endregion

    #region Conversion Methods
    /// <summary>
    /// Returns the inverse (for unit quaternions this equals the conjugate).
    /// </summary>
    public CustomQuaternion Inverse()
    {
        // Assumes unit quaternion usage throughout the project.
        return new CustomQuaternion(-x, -y, -z, w);
    }
    
    /// <summary>
    /// Converts to UnityEngine.Quaternion for Transform interop.
    /// </summary>
    public Quaternion ToUnityQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }

    /// <summary>
    /// Human-readable component dump.
    /// </summary>
    public override string ToString() => $"({x}, {y}, {z}, {w})";
    #endregion

    #region Quaternion Arithmetic Operators
    /// <summary>
    /// Quaternion multiplication (rotation composition): result = a ∘ b.
    /// </summary>
    public static CustomQuaternion operator *(CustomQuaternion a, CustomQuaternion b)
    {
        return new CustomQuaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z
        );
    }

    /// <summary>
    /// Rotates a vector by this quaternion: q * v * q⁻¹.
    /// </summary>
    public static Coords operator *(CustomQuaternion q, Coords v)
    {
        // Lift vector into a pure quaternion (w = 0).
        CustomQuaternion p = new CustomQuaternion(v.x, v.y, v.z, 0f);

        // For unit quaternions, inverse is conjugate.
        CustomQuaternion qInv = q.Inverse();

        // q * p * q⁻¹
        CustomQuaternion r = q * p * qInv;
        return new Coords(r.x, r.y, r.z);
    }
    #endregion
}
