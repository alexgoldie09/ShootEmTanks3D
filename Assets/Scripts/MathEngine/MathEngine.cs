/*
 * MathEngine.cs
 * ----------------------------------------------------------------
 * A static math utility class for performing vector and matrix operations
 * in a custom linear algebra system (supports Coords and Matrix types).
 *
 * PURPOSE:
 * - Provide reusable, centralized methods for math operations like translation,
 *   rotation, scaling, shearing, reflection, interpolation, and quaternion rotation.
 *
 * FEATURES:
 * - Vector math: normalization, magnitude, dot/cross product, distance, angle, reflect.
 * - Transformation matrices: identity, translation, scale, shear, reflect, Euler rotation.
 * - Quaternion helpers: Euler construction, FromToRotation, LookRotation, normalize.
 * - Coordinate-space helpers: extract position and scale from 4x4 transforms.
 * - Designed to be used with immutable `Coords`, `Matrix`, and `CustomQuaternion`.
 *
 * DESIGN:
 * - Pure static class with no instance state.
 * - Centralizes transformation logic for consistency across the codebase.
 */

using System;
using UnityEngine;

public static class MathEngine
{
    #region Vector Operations
    /// <summary>
    /// Returns the unit-length version of the input vector.
    /// </summary>
    public static Coords Normalize(Coords vector)
    {
        // Length computed via Distance to origin; assumes non-zero length.
        float length = Distance(new Coords(0, 0, 0), vector);
        return new Coords(vector.x / length, vector.y / length, vector.z / length);
    }

    /// <summary>
    /// Computes the Euclidean magnitude (length) of a vector.
    /// </summary>
    public static float Magnitude(Coords a)
    {
        return Mathf.Sqrt(a.x * a.x + a.y * a.y + a.z * a.z);
    }

    /// <summary>
    /// Computes the Euclidean distance between two points.
    /// </summary>
    public static float Distance(Coords a, Coords b)
    {
        return Mathf.Sqrt(Square(a.x - b.x) + Square(a.y - b.y) + Square(a.z - b.z));
    }

    /// <summary>
    /// Squares a float value (micro-helper used by distance).
    /// </summary>
    public static float Square(float value) => value * value;

    /// <summary>
    /// Computes the dot product (projection / alignment measure).
    /// </summary>
    public static float Dot(Coords a, Coords b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    /// <summary>
    /// Computes the cross product (perpendicular vector) a × b.
    /// </summary>
    public static Coords Cross(Coords a, Coords b)
    {
        return new Coords(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x
        );
    }

    /// <summary>
    /// Reflects a vector around a given surface normal.
    /// </summary>
    public static Coords Reflect(Coords vector, Coords normal)
    {
        float dot = Dot(vector, normal);
        return vector - (normal * (dot * 2f));
    }
    
    /// <summary>
    /// Linearly interpolates between A and B using t ∈ [0, 1].
    /// </summary>
    public static Coords Lerp(Coords A, Coords B, float t)
    {
        t = Mathf.Clamp01(t);
        return new Coords(
            A.x + (B.x - A.x) * t,
            A.y + (B.y - A.y) * t,
            A.z + (B.z - A.z) * t
        );
    }
    #endregion

    #region Matrix Generators
    /// <summary>
    /// Creates a translation matrix that offsets by the given vector.
    /// </summary>
    public static Matrix CreateTranslationMatrix(Coords vector)
    {
        float[] m = {
            1, 0, 0, vector.x,
            0, 1, 0, vector.y,
            0, 0, 1, vector.z,
            0, 0, 0, 1
        };
        return new Matrix(4, 4, m);
    }

    /// <summary>
    /// Creates a scale matrix for independent X, Y, Z scaling.
    /// </summary>
    public static Matrix CreateScaleMatrix(float sx, float sy, float sz)
    {
        float[] m = {
            sx, 0,  0,  0,
            0,  sy, 0,  0,
            0,  0,  sz, 0,
            0,  0,  0,  1
        };
        return new Matrix(4, 4, m);
    }
    
    /// <summary>
    /// Creates a rotation matrix from an axis and angle (degrees) via quaternion conversion.
    /// </summary>
    public static Matrix CreateRotationMatrixFromQuaternion(Coords axis, float angleDegrees)
    {
        return new CustomQuaternion(axis, angleDegrees).ToMatrix();
    }
    #endregion

    #region Quaternion Operations
    /// <summary>
    /// Builds a quaternion from Euler angles (degrees).
    /// </summary>
    public static CustomQuaternion Euler(float xDeg, float yDeg, float zDeg)
    {
        // Axis-angle quaternions for each axis
        CustomQuaternion qx = new CustomQuaternion(new Coords(1, 0, 0), xDeg);
        CustomQuaternion qy = new CustomQuaternion(new Coords(0, 1, 0), yDeg);
        CustomQuaternion qz = new CustomQuaternion(new Coords(0, 0, 1), zDeg);

        // Combine (project uses this specific order; matches existing behavior)
        return qy * qx * qz;
    }

    /// <summary>
    /// Creates a quaternion that rotates vector 'from' to vector 'to' (like Quaternion.FromToRotation).
    /// </summary>
    public static CustomQuaternion FromToRotation(Coords from, Coords to)
    {
        from = Normalize(from);
        to   = Normalize(to);

        float dot = Dot(from, to);
        dot = Mathf.Clamp(dot, -1f, 1f); // numerical safety

        if (dot >= 1f)
        {
            // Same direction → no rotation
            return new CustomQuaternion(0, 0, 0, 1);
        }
        else if (dot <= -1f)
        {
            // Opposite direction → 180° around any orthogonal axis
            Coords orthogonal = Mathf.Abs(from.x) > Mathf.Abs(from.z)
                ? new Coords(-from.y, from.x, 0)
                : new Coords(0, -from.z, from.y);

            orthogonal = Normalize(orthogonal);
            return new CustomQuaternion(orthogonal, 180f);
        }
        else
        {
            // General case → axis = cross(from, to), angle = acos(dot)
            Coords axis = Cross(from, to);
            float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
            return new CustomQuaternion(Normalize(axis), angle);
        }
    }

    /// <summary>
    /// Creates a quaternion that looks in 'forward' while keeping 'up' as vertical as possible.
    /// </summary>
    public static CustomQuaternion LookRotation(Coords forward, Coords up)
    {
        forward = Normalize(forward);
        up      = Normalize(up);

        // Build orthonormal basis: right = up × forward, re-orthonormalize up
        Coords right = Normalize(Cross(up, forward));
        up = Cross(forward, right);

        // Rotation matrix from basis (column-major style layout for 3x3 block)
        float[] m = {
            right.x,    up.x,    forward.x,   0,
            right.y,    up.y,    forward.y,   0,
            right.z,    up.z,    forward.z,   0,
            0,          0,       0,           1
        };

        Matrix rotMat = new Matrix(4, 4, m);

        return CustomQuaternion.FromMatrix(rotMat);
    }
    #endregion

    #region Coordinate Transforms (Return Coords directly)
    /// <summary>
    /// Extracts position (the last column) from a 4x4 transform matrix.
    /// </summary>
    public static Coords ExtractPosition(Matrix matrix)
    {
        if (matrix.Rows != 4 || matrix.Cols != 4)
            throw new InvalidOperationException("Matrix must be 4x4 to extract position.");

        return new Coords(
            matrix.GetValue(0, 3), // X
            matrix.GetValue(1, 3), // Y
            matrix.GetValue(2, 3)  // Z
        );
    }

    /// <summary>
    /// Extracts non-uniform scale from a 4x4 transform by measuring basis vector lengths.
    /// </summary>
    public static Coords ExtractScale(Matrix m)
    {
        if (m.Rows != 4 || m.Cols != 4)
            throw new InvalidOperationException("Matrix must be 4x4 to extract scale.");

        // Column vectors represent transformed basis axes; their lengths are the scales.
        float scaleX = Mathf.Sqrt(m.GetValue(0,0) * m.GetValue(0,0) +
                                  m.GetValue(1,0) * m.GetValue(1,0) +
                                  m.GetValue(2,0) * m.GetValue(2,0));

        float scaleY = Mathf.Sqrt(m.GetValue(0,1) * m.GetValue(0,1) +
                                  m.GetValue(1,1) * m.GetValue(1,1) +
                                  m.GetValue(2,1) * m.GetValue(2,1));

        float scaleZ = Mathf.Sqrt(m.GetValue(0,2) * m.GetValue(0,2) +
                                  m.GetValue(1,2) * m.GetValue(1,2) +
                                  m.GetValue(2,2) * m.GetValue(2,2));

        return new Coords(scaleX, scaleY, scaleZ);
    }
    #endregion
}