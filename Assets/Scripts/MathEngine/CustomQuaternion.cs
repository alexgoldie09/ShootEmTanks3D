/*
 * CustomQuaternion.cs
 * ----------------------------------------------------------------
 * A lightweight, immutable data container for quaternions.
 *
 * PURPOSE:
 * - Represent rotations as unit quaternions.
 * - Enable quaternion arithmetic (multiplication, normalization).
 * - Provide helper conversion methods (e.g., ToMatrix).
 *
 * FEATURES:
 * - Immutable struct for safety and value semantics.
 * - Basic constructors from components or axis-angle.
 * - Quaternion * Quaternion and Quaternion * Coords multiplication.
 * - Designed to be used alongside MathEngine for all practical operations.
 */

using UnityEngine;

public readonly struct CustomQuaternion
{
    // Public fields representing the quaternion parts.
    public readonly float x;
    public readonly float y;
    public readonly float z;
    public readonly float w;
    
    #region Constructors
    // Constructor from components
    public CustomQuaternion(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    // Constructor from axis-angle (degrees)
    public CustomQuaternion(Coords axis, float angleDegrees)
    {
        Coords norm = MathEngine.Normalize(axis);
        float radians = angleDegrees * Mathf.Deg2Rad;
        float halfAngle = radians / 2f;

        w = Mathf.Cos(halfAngle);
        float sin = Mathf.Sin(halfAngle);
        x = norm.x * sin;
        y = norm.y * sin;
        z = norm.z * sin;
    }
    #endregion
    
    #region Conversion Methods
    // Returns the inverse (for unit quaternions, this is just the conjugate)
    public CustomQuaternion Inverse()
    {
        return new CustomQuaternion(-x, -y, -z, w);
    }

    // Converts to 4x4 rotation matrix (for use in MathEngine)
    public Matrix ToMatrix()
    {
        float[] m = {
            1 - 2 * y * y - 2 * z * z, 2 * x * y - 2 * w * z,     2 * x * z + 2 * w * y,     0,
            2 * x * y + 2 * w * z,     1 - 2 * x * x - 2 * z * z, 2 * y * z - 2 * w * x,     0,
            2 * x * z - 2 * w * y,     2 * y * z + 2 * w * x,     1 - 2 * x * x - 2 * y * y, 0,
            0,                         0,                         0,                         1
        };

        return new Matrix(4, 4, m);
    }
    // Returns a string of the Quaternion
    public override string ToString() => $"({x}, {y}, {z}, {w})";
    // Converts this custom quaternion to UnityEngine.Quaternion
    public Quaternion ToUnityQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
    #endregion
    
    #region Quaternion Arithmetic Operators
    // Quaternion multiplication (composition of rotations)
    public static CustomQuaternion operator *(CustomQuaternion a, CustomQuaternion b)
    {
        return new CustomQuaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z
        );
    }

    // Rotates a vector using this quaternion (q * v * q⁻¹)
    public static Coords operator *(CustomQuaternion q, Coords v)
    {
        CustomQuaternion p = new CustomQuaternion(v.x, v.y, v.z, 0);
        CustomQuaternion qInv = q.Inverse();
        CustomQuaternion result = q * p * qInv;
        return new Coords(result.x, result.y, result.z);
    }
    #endregion
}
