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
    // Returns a normalized quaternion by dividing each component by the magnitude.
    public static CustomQuaternion Normalize(CustomQuaternion q)
    {
        float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        if (mag == 0f) return new CustomQuaternion(0, 0, 0, 1); // fallback to identity
        return new CustomQuaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
    }
    #endregion
    
    #region Coordinate Transforms (Return Coords directly)
    // Applies a translation to a position using a direction vector
    public static Coords Translate(Coords position, Coords direction)
    {
        Matrix result = CreateTranslationMatrix(direction) * new Matrix(4, 1, position.AsFloats());
        return result.AsCoords();
    }
    
    // Scales a position relative to origin using provided scale factors
    public static Coords Scale(Coords position, float sx, float sy, float sz)
    {
        Matrix result = CreateScaleMatrix(sx, sy, sz) * new Matrix(4, 1, position.AsFloats());
        return result.AsCoords();
    }
    
    // Applies a shear transform to a position
    public static Coords Shear(Coords position, float shearX, float shearY)
    {
        Matrix result = CreateShearMatrix(shearX, shearY) * new Matrix(4, 1, position.AsFloats());
        return result.AsCoords();
    }
    
    // Reflects a position across the X-axis
    public static Coords ReflectX(Coords position)
    {
        Matrix result = CreateReflectXMatrix() * new Matrix(4, 1, position.AsFloats());
        return result.AsCoords();
    }
    
    // Applies rotation to a position using Euler angles and rotation order X->Y->Z
    public static Coords Rotate(Coords position, float angleX, bool cwX,
        float angleY, bool cwY,
        float angleZ, bool cwZ)
    {
        Matrix result = CreateRotationMatrixXYZ(angleX, cwX, angleY, cwY, angleZ, cwZ) *
                        new Matrix(4, 1, position.AsFloats());

        return result.AsCoords();
    }
    
    // Rotates a position using a quaternion (q * v * q⁻¹)
    public static Coords QRotate(Coords position, CustomQuaternion rotation)
    {
        return rotation * position;
    }

    // Overload for convenience — builds quaternion from axis + angle
    public static Coords QRotate(Coords position, Coords axis, float angleDegrees)
    {
        CustomQuaternion q = new CustomQuaternion(axis, angleDegrees);
        return q * position;
    }
    
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
    #endregion

    #region Rotation Axis & Angle Extraction
    // Extracts the angle (in radians) from a rotation matrix
    public static float GetRotationAngle(Matrix rotation)
    {
        return Mathf.Acos(0.5f * (
            rotation.GetValue(0, 0) +
            rotation.GetValue(1, 1) +
            rotation.GetValue(2, 2) +
            rotation.GetValue(3, 3) - 2));
    }
    
    // Extracts the rotation axis vector from a rotation matrix and known angle
    public static Coords GetRotationAxis(Matrix rotation, float angle)
    {
        float vx = (rotation.GetValue(2, 1) - rotation.GetValue(1, 2)) / (2 * Mathf.Sin(angle));
        float vy = (rotation.GetValue(0, 2) - rotation.GetValue(2, 0)) / (2 * Mathf.Sin(angle));
        float vz = (rotation.GetValue(1, 0) - rotation.GetValue(0, 1)) / (2 * Mathf.Sin(angle));
        return new Coords(vx, vy, vz, 0);
    }
    #endregion
}
