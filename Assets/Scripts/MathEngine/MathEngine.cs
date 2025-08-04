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
   * - Vector math: normalization, dot/cross product, distance, angle.
   * - Transformation matrices: identity, translation, scale, shear, reflect, rotate.
   * - Supports both matrix-based and direct geometric operations.
   * - Quaternion-based rotation for arbitrary axis rotation (in radians or degrees).
   * - Safe mathematical abstractions for use with immutable `Coords` and `Matrix`.
   *
   * DESIGN:
   * - Pure static class: no instance state.
   * - Future-compatible with custom quaternion types.
   * - All transformation logic is built in one place.
*/

using System;
using UnityEngine;

public static class MathEngine
{
    #region Vector Operations
    // Returns a normal version of the input vector (unit length)
    public static Coords Normalize(Coords vector)
    {
        float length = Distance(new Coords(0, 0, 0), vector);
        return new Coords(vector.x / length, vector.y / length, vector.z / length);
    }
    
    // Returns the scalar magnitude (Euclidean length) of a Coords vector.
    public static float Magnitude(Coords a)
    {
        return Mathf.Sqrt(a.x * a.x + a.y * a.y + a.z * a.z);
    }
    
    // Calculate the Euclidean distance between two points
    public static float Distance(Coords a, Coords b)
    {
        return Mathf.Sqrt(Square(a.x - b.x) + Square(a.y - b.y) + Square(a.z - b.z));
    }
    
    // Squares a float value (used in distance calculations)
    public static float Square(float value) => value * value;
    
    // Returns the dot product between two vectors (measures alignment)
    public static float Dot(Coords a, Coords b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }
    
    // Returns the cross product between two vectors (perp vector)
    public static Coords Cross(Coords a, Coords b)
    {
        return new Coords(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x
        );
    }
    
    // Reflects a vector off a surface with the given normal.
    public static Coords Reflect(Coords vector, Coords normal)
    {
        float dot = Dot(vector, normal);
        return vector - (normal * (dot * 2f));
    }
    
    // Returns the angle in radians between two vectors
    public static float Angle(Coords a, Coords b)
    {
        float denominator = Distance(new Coords(0, 0, 0), a) * Distance(new Coords(0, 0, 0), b);
        return Mathf.Acos(Dot(a, b) / denominator); // radians
    }
    
    // Linearly interpolates between A and B using a normalized factor t
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
    // Creates an identity matrix of the given size (defaults to 4x4)
    public static Matrix IdentityMatrix(int size = 4)
    {
        float[] values = new float[size * size];
        for (int i = 0; i < size; i++)
            values[i * size + i] = 1f;

        return new Matrix(size, size, values);
    }
    
    // Creates a translation matrix from a direction vector
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
    
    // Creates a scale matrix for scaling in X, Y, and Z axes
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
    
    // Creates a 4x4 shear matrix using shearX and shearY in the XY plane
    public static Matrix CreateShearMatrix(float shearX, float shearY)
    {
        float[] m = {
            1,      shearY, 0, 0,
            shearX, 1,      0, 0,
            0,      0,      1, 0,
            0,      0,      0, 1
        };
        return new Matrix(4, 4, m);
    }
    
    // Creates a matrix that reflects across the X-axis
    public static Matrix CreateReflectXMatrix()
    {
        float[] m = {
            -1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        };
        return new Matrix(4, 4, m);
    }
    
    // Creates a composite rotation matrix using Euler angles (in radians)
    public static Matrix CreateRotationMatrixXYZ(float angleX, bool clockwiseX,
        float angleY, bool clockwiseY,
        float angleZ, bool clockwiseZ)
    {
        if (clockwiseX) angleX = 2 * Mathf.PI - angleX;
        if (clockwiseY) angleY = 2 * Mathf.PI - angleY;
        if (clockwiseZ) angleZ = 2 * Mathf.PI - angleZ;

        float[] xRoll = {
            1, 0, 0, 0,
            0, Mathf.Cos(angleX), -Mathf.Sin(angleX), 0,
            0, Mathf.Sin(angleX),  Mathf.Cos(angleX), 0,
            0, 0, 0, 1
        };

        float[] yRoll = {
            Mathf.Cos(angleY), 0, Mathf.Sin(angleY), 0,
            0, 1, 0, 0,
            -Mathf.Sin(angleY), 0, Mathf.Cos(angleY), 0,
            0, 0, 0, 1
        };

        float[] zRoll = {
            Mathf.Cos(angleZ), -Mathf.Sin(angleZ), 0, 0,
            Mathf.Sin(angleZ),  Mathf.Cos(angleZ), 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        };

        Matrix X = new Matrix(4, 4, xRoll);
        Matrix Y = new Matrix(4, 4, yRoll);
        Matrix Z = new Matrix(4, 4, zRoll);

        return Z * Y * X;
    }
    
    // Creates a rotation matrix from an axis and angle using quaternion math
    public static Matrix CreateRotationMatrixFromQuaternion(Coords axis, float angleDegrees)
    {
        return new CustomQuaternion(axis, angleDegrees).ToMatrix();
    }
    #endregion
    
    #region Quaternion Operations
    public static CustomQuaternion Euler(float xDeg, float yDeg, float zDeg)
    {
        // Convert to quaternions for each axis
        CustomQuaternion qx = new CustomQuaternion(new Coords(1, 0, 0), xDeg);
        CustomQuaternion qy = new CustomQuaternion(new Coords(0, 1, 0), yDeg);
        CustomQuaternion qz = new CustomQuaternion(new Coords(0, 0, 1), zDeg);

        // Combine in Unity's order: Z * X * Y (by default Unity uses ZXY for Euler)
        return qy * qx * qz;
    }
    // Returns a normalized quaternion by dividing each component by the magnitude.
    public static CustomQuaternion Normalize(CustomQuaternion q)
    {
        float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        if (mag == 0f) return new CustomQuaternion(0, 0, 0, 1); // fallback to identity
        return new CustomQuaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
    }
    
    // Creates a quaternion that rotates from one direction vector to another.
    // Equivalent to Unity's Quaternion.FromToRotation.
    public static CustomQuaternion FromToRotation(Coords from, Coords to)
    {
        from = Normalize(from);
        to = Normalize(to);

        float dot = Dot(from, to);
        dot = Mathf.Clamp(dot, -1f, 1f); // avoid precision errors

        if (dot >= 1f)
        {
            // Vectors are the same → no rotation
            return new CustomQuaternion(0, 0, 0, 1);
        }
        else if (dot <= -1f)
        {
            // Vectors are opposite - need a 180° rotation around any orthogonal axis
            Coords orthogonal = Mathf.Abs(from.x) > Mathf.Abs(from.z)
                ? new Coords(-from.y, from.x, 0)
                : new Coords(0, -from.z, from.y);

            orthogonal = Normalize(orthogonal);
            return new CustomQuaternion(orthogonal, 180f);
        }
        else
        {
            // General case → axis = cross product, angle = acos(dot)
            Coords axis = Cross(from, to);
            float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
            return new CustomQuaternion(Normalize(axis), angle);
        }
    }
    
    // Creates a quaternion that rotates a forward vector to look in the given direction,
    // while keeping a provided up vector as close to vertical as possible.
    public static CustomQuaternion LookRotation(Coords forward, Coords up)
    {
        forward = Normalize(forward);
        up = Normalize(up);

        // Build orthonormal basis
        Coords right = Normalize(Cross(up, forward));
        up = Cross(forward, right);

        // Column-major (Unity-style basis)
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
    // Extracts position (last column of the 4x4 matrix) and returns as Coords
    public static Coords ExtractPosition(Matrix matrix)
    {
        if (matrix.Rows != 4 || matrix.Cols != 4)
            throw new InvalidOperationException("Matrix must be 4x4 to extract position.");

        return new Coords(
            matrix.GetValue(0, 3), // X position
            matrix.GetValue(1, 3), // Y position
            matrix.GetValue(2, 3)  // Z position
        );
    }
    
    // Extracts scale from a 4x4 transformation matrix.
    public static Coords ExtractScale(Matrix m)
    {
        if (m.Rows != 4 || m.Cols != 4)
            throw new InvalidOperationException("Matrix must be 4x4 to extract scale.");
        
        // Each axis vector length = scale
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
