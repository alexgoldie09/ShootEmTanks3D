/*
 * Matrix.cs
 * ----------------------------------------------------------------
 * A lightweight matrix type used for linear algebra and 3D transforms.
 *
 * PURPOSE:
 * - Represent matrices and support essential operations (addition, multiplication).
 * - Interoperate with the custom `Coords` type for transformation results.
 * - Back the math stack used by gameplay scripts (e.g., transform building).
 *
 * FEATURES:
 * - Immutable, row-major storage in a flat float array.
 * - Dimension-safe construction and element access.
 * - Matrix + Matrix and Matrix * Matrix operators.
 * - Convert a 4x1 matrix into `Coords` for transform results.
 * - Readable `ToString()` for quick debugging.
 *
 * DESIGN:
 * - Row-major indexing: index = r * Cols + c.
 * - Intentional minimal surface area for performance and clarity.
 */

using System;

public readonly struct Matrix
{
    // Internal flat array storing values in row-major order.
    private readonly float[] values;

    // Public dimensions.
    public readonly int Rows;
    public readonly int Cols;

    #region Constructors
    /// <summary>
    /// Creates a matrix with explicit dimensions and copies provided values.
    /// Values are expected in row-major order and must match rows*cols.
    /// </summary>
    public Matrix(int rows, int cols, float[] inputValues)
    {
        if (inputValues == null) throw new ArgumentNullException(nameof(inputValues));
        if (inputValues.Length != rows * cols)
            throw new ArgumentException("Input values length does not match matrix dimensions.");

        Rows = rows;
        Cols = cols;
        values = new float[rows * cols];
        Array.Copy(inputValues, values, inputValues.Length);
    }
    #endregion

    #region Accessors
    /// <summary>
    /// Returns the value at (r, c). Indices are zero-based.
    /// </summary>
    public float GetValue(int r, int c)
    {
        if (r < 0 || r >= Rows || c < 0 || c >= Cols)
            throw new IndexOutOfRangeException($"Matrix index out of range: ({r}, {c})");

        // Row-major index
        return values[r * Cols + c];
    }
    #endregion

    #region Conversion Methods
    /// <summary>
    /// Treats this matrix as a 4x1 column vector and converts to Coords.
    /// Use after multiplying a 4x4 transform by a 4x1 position vector.
    /// </summary>
    public Coords AsCoords()
    {
        if (Rows == 4 && Cols == 1)
            return new Coords(values[0], values[1], values[2], values[3]);

        throw new InvalidOperationException("Matrix must be 4x1 to convert to Coords.");
    }

    /// <summary>
    /// Returns a human-readable string of matrix values.
    /// </summary>
    public override string ToString()
    {
        string s = "";
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
                s += values[r * Cols + c] + " ";
            s += "\n";
        }
        return s;
    }
    #endregion

    #region Matrix Arithmetic Operators
    /// <summary>
    /// Element-wise addition. Dimensions must match.
    /// </summary>
    public static Matrix operator +(Matrix a, Matrix b)
    {
        if (a.Rows != b.Rows || a.Cols != b.Cols)
            throw new InvalidOperationException("Matrix addition failed: dimensions do not match.");

        float[] result = new float[a.values.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = a.values[i] + b.values[i];

        return new Matrix(a.Rows, a.Cols, result);
    }

    /// <summary>
    /// Standard matrix multiplication: (a.Rows x a.Cols) * (b.Rows x b.Cols).
    /// Requires a.Cols == b.Rows. Result is (a.Rows x b.Cols).
    /// </summary>
    public static Matrix operator *(Matrix a, Matrix b)
    {
        if (a.Cols != b.Rows)
            throw new InvalidOperationException(
                $"Matrix multiplication failed: {a.Rows}x{a.Cols} * {b.Rows}x{b.Cols}");

        float[] result = new float[a.Rows * b.Cols];

        // Triple loop: row (i), column (j), and accumulator over k
        for (int i = 0; i < a.Rows; i++)
        {
            for (int j = 0; j < b.Cols; j++)
            {
                float sum = 0f;
                for (int k = 0; k < a.Cols; k++)
                {
                    // a[i,k] * b[k,j]
                    sum += a.values[i * a.Cols + k] * b.values[k * b.Cols + j];
                }
                result[i * b.Cols + j] = sum;
            }
        }

        return new Matrix(a.Rows, b.Cols, result);
    }
    #endregion
}