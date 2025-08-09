/*
 * Coords.cs
 * ----------------------------------------------------------------
 * A lightweight, immutable structure representing a coordinate or vector 
 * in 2D, 3D, or 4D space for custom math systems.
 *
 * PURPOSE:
 * - Represent points or direction vectors for linear algebra operations.
 * - Provide compatibility with Unity's Vector3 and other Unity types.
 *
 * FEATURES:
 * - Immutable readonly struct for value safety.
 * - Explicit constructor overloads for 2D, 3D, and 4D initialization.
 * - Operator overloads for addition, subtraction, scalar multiply/divide.
 * - Methods to convert to Vector3 and float[] for integration with Unity and matrices.
 * - Designed for compatibility with custom matrix and quaternion systems.
 *
 */

using UnityEngine;

public struct Coords
{
    // Public fields representing spatial coordinates (and optional homogeneous component w)
    public float x;
    public float y;
    public float z;
    public float w;

    #region Constructors
    /// <summary>
    /// Constructs a 2D coordinate (defaults z = -1, w = 0).
    /// </summary>
    public Coords(float x, float y)
    {
        this.x = x;
        this.y = y;
        this.z = -1f;
        this.w = 0f;
    }

    /// <summary>
    /// Constructs a 3D coordinate.
    /// </summary>
    public Coords(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = 0f;
    }

    /// <summary>
    /// Constructs a 4D coordinate (e.g., homogeneous coordinates).
    /// </summary>
    public Coords(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    /// <summary>
    /// Constructs a Coords from Unity's Vector3 (sets w = 0).
    /// </summary>
    public Coords(Vector3 vec) : this(vec.x, vec.y, vec.z) { }

    /// <summary>
    /// Constructs a Coords from Unity's Vector3 with a custom w value.
    /// </summary>
    public Coords(Vector3 vec, float w = 0f) : this(vec.x, vec.y, vec.z, w) { }

    /// <summary>
    /// Returns a Coords at (0,0,0) with w = 0.
    /// </summary>
    public static Coords Zero() => new Coords(0f, 0f, 0f);
    #endregion

    #region Conversion Methods
    /// <summary>
    /// Converts to Unity's Vector3 (ignores w).
    /// </summary>
    public Vector3 ToVector3() => new Vector3(x, y, z);

    /// <summary>
    /// Converts to float array [x, y, z, w] for matrix/vector math operations.
    /// </summary>
    public float[] AsFloats() => new float[] { x, y, z, w };

    /// <summary>
    /// Returns a human-readable string representation (ignores w).
    /// </summary>
    public override string ToString() => $"({x}, {y}, {z})";

    /// <summary>
    /// Returns the inverse (negated) vector.
    /// </summary>
    public static Coords operator -(Coords a) =>
        new Coords(-a.x, -a.y, -a.z);
    #endregion

    #region Vector Arithmetic Operators
    /// <summary>
    /// Adds two Coords vectors element-wise.
    /// </summary>
    public static Coords operator +(Coords a, Coords b) =>
        new Coords(a.x + b.x, a.y + b.y, a.z + b.z);

    /// <summary>
    /// Subtracts one Coords vector from another element-wise.
    /// </summary>
    public static Coords operator -(Coords a, Coords b) =>
        new Coords(a.x - b.x, a.y - b.y, a.z - b.z);

    /// <summary>
    /// Multiplies each component of the Coords vector by a scalar.
    /// </summary>
    public static Coords operator *(Coords a, float scalar) =>
        new Coords(a.x * scalar, a.y * scalar, a.z * scalar);

    /// <summary>
    /// Divides each component of the Coords vector by a scalar.
    /// </summary>
    public static Coords operator /(Coords a, float scalar) =>
        new Coords(a.x / scalar, a.y / scalar, a.z / scalar);
    #endregion
}
