/*
 * Coords.cs
 * ----------------------------------------------------------------
 * A lightweight, immutable structure representing a coordinate or vector 
 * in 2D, 3D, or 4D space for custom math systems.
 *
 * PURPOSE:
 * - Represent points or direction vectors for linear algebra operations.
 * - Provide compatibility with Unity's Vector3.
 *
 * FEATURES:
 * - Immutable readonly struct for value safety.
 * - Explicit constructor overloads for 2D, 3D, and 4D initialization.
 * - Operator overloads for addition, subtraction, scalar multiply/divide.
 * - Methods to convert to Vector3 and float[].
 * - Designed for compatibility with custom matrix and quaternion systems.
 *
 * NOTE:
 * - Complex math operations (normalization, cross product, rotations)
 *   are handled externally in the HolisticMath utility class.
 */

using UnityEngine;

public readonly struct Coords 
{

    // Public fields representing spatial coordinates.
    public readonly float x;
    public readonly float y;
    public readonly float z;
    public readonly float w;
    
    #region Constructors
    // Constructor for 2D coordinates (defaults z to -1).
    public Coords(float x, float y)
    {
        this.x = x;
        this.y = y;
        this.z = -1f;
        this.w = 0f;
    }

    // Constructor for 3D coordinates.
    public Coords(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = 0f;
    }
    
    // Constructor for 4D coordinates (e.g., homogeneous coordinates).
    public Coords(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    // Constructor from Unity's Vector3.
    public Coords(Vector3 vec) : this(vec.x, vec.y, vec.z) {}
    
    // Construct from Unity's Vector3 with an additional w value.
    public Coords(Vector3 vec, float w = 0f) : this(vec.x, vec.y, vec.z, w) {}
    #endregion
    
    #region Conversion Methods
    // Converts to Unity's Vector3 (ignores w)
    public Vector3 ToVector3() => new Vector3(x, y, z);
    
    // Converts to float array [x, y, z, w] (useful for matrix ops)
    public float[] AsFloats() => new float[] { x, y, z, w };

    // Returns a string representation
    public override string ToString() => $"({x}, {y}, {z})";
    #endregion

    #region Vector Arithmetic Operators
    // Operator overload for vector addition.
    public static Coords operator +(Coords a, Coords b) =>
        new Coords(a.x + b.x, a.y + b.y, a.z + b.z);

    // Operator overload for vector subtraction.
    public static Coords operator -(Coords a, Coords b) =>
        new Coords(a.x - b.x, a.y - b.y, a.z - b.z);

    // Operator overload for scalar multiplication.
    public static Coords operator *(Coords a, float scalar) =>
        new Coords(a.x * scalar, a.y * scalar, a.z * scalar);

    // Operator overload for scalar division.
    public static Coords operator /(Coords a, float scalar) =>
        new Coords(a.x / scalar, a.y / scalar, a.z / scalar);
    #endregion
}