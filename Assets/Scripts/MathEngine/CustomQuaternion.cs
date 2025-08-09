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
    /// Converts this quaternion to a 4x4 rotation matrix.
    /// </summary>
    public Matrix ToMatrix()
    {
        // Standard quaternion-to-matrix (right-handed) conversion.
        float[] m = {
            1 - 2 * y * y - 2 * z * z, 2 * x * y - 2 * w * z,     2 * x * z + 2 * w * y,     0,
            2 * x * y + 2 * w * z,     1 - 2 * x * x - 2 * z * z, 2 * y * z - 2 * w * x,     0,
            2 * x * z - 2 * w * y,     2 * y * z + 2 * w * x,     1 - 2 * x * x - 2 * y * y, 0,
            0,                         0,                         0,                         1
        };
        return new Matrix(4, 4, m);
    }
    
    /// <summary>
    /// Builds a quaternion from a 4x4 rotation matrix using the trace method.
    /// </summary>
    public static CustomQuaternion FromMatrix(Matrix m)
    {
        // The trace is the sum of the matrix's diagonal rotation elements.
        // If trace > 0, it means the scalar (w) component is the largest contributor.
        float trace = m.GetValue(0, 0) + m.GetValue(1, 1) + m.GetValue(2, 2);

        float w, x, y, z;

        if (trace > 0f)
        {
            // Compute scale factor (s) to extract w first.
            float s = Mathf.Sqrt(trace + 1f) * 2f; // s = 4 * w
            w = 0.25f * s;
            x = (m.GetValue(2, 1) - m.GetValue(1, 2)) / s;
            y = (m.GetValue(0, 2) - m.GetValue(2, 0)) / s;
            z = (m.GetValue(1, 0) - m.GetValue(0, 1)) / s;
        }
        else if (m.GetValue(0, 0) > m.GetValue(1, 1) && m.GetValue(0, 0) > m.GetValue(2, 2))
        {
            // X-axis term is the largest — extract x first for numerical stability.
            float s = Mathf.Sqrt(1f + m.GetValue(0, 0) - m.GetValue(1, 1) - m.GetValue(2, 2)) * 2f; // s = 4 * x
            w = (m.GetValue(2, 1) - m.GetValue(1, 2)) / s;
            x = 0.25f * s;
            y = (m.GetValue(0, 1) + m.GetValue(1, 0)) / s;
            z = (m.GetValue(0, 2) + m.GetValue(2, 0)) / s;
        }
        else if (m.GetValue(1, 1) > m.GetValue(2, 2))
        {
            // Y-axis term is the largest — extract y first.
            float s = Mathf.Sqrt(1f + m.GetValue(1, 1) - m.GetValue(0, 0) - m.GetValue(2, 2)) * 2f; // s = 4 * y
            w = (m.GetValue(0, 2) - m.GetValue(2, 0)) / s;
            x = (m.GetValue(0, 1) + m.GetValue(1, 0)) / s;
            y = 0.25f * s;
            z = (m.GetValue(1, 2) + m.GetValue(2, 1)) / s;
        }
        else
        {
            // Z-axis term is the largest — extract z first.
            float s = Mathf.Sqrt(1f + m.GetValue(2, 2) - m.GetValue(0, 0) - m.GetValue(1, 1)) * 2f; // s = 4 * z
            w = (m.GetValue(1, 0) - m.GetValue(0, 1)) / s;
            x = (m.GetValue(0, 2) + m.GetValue(2, 0)) / s;
            y = (m.GetValue(1, 2) + m.GetValue(2, 1)) / s;
            z = 0.25f * s;
        }

        // Return the constructed quaternion from extracted components.
        return new CustomQuaternion(x, y, z, w);
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
