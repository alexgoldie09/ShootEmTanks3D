/*
 * Matrix.cs
 * ----------------------------------------
 * A basic implementation of a mathematical matrix class.
 *
 * PURPOSE:
 * - To represent matrices and support basic matrix operations such as
 *   addition and multiplication.
 * - Designed to complement the Coords system by enabling transformations
 *   and linear algebra operations in a custom math framework.
 *
 * FEATURES:
 * - Stores matrix data in a 1D array for performance.
 * - Constructor for defining arbitrary-sized matrices with custom values.
 * - Supports:
 *     - Matrix addition (same dimensions)
 *     - Matrix multiplication (standard dot product logic)
 *     - Conversion to a 4D coordinate (`Coords`) if the shape is compatible
 * - String output for readable debugging.
 *
 * NOTES:
 * - Assumes row-major order.
 * - Minimal validation and error handling; intended for internal tools or
 *   educational purposes, not production-grade math libraries.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public readonly struct Matrix
{
    // Internal flat array for storing values in row-major order
    private readonly float[] values;

    // Matrix dimensions
    public readonly int Rows;
    public readonly int Cols;
    
    #region Constructors
    // Constructor: initializes matrix with given dimensions and copies values into internal array
    public Matrix(int rows, int cols, float[] inputValues)
    {
        if (inputValues.Length != rows * cols)
            throw new ArgumentException("Input values length does not match matrix dimensions.");
        
        Rows = rows;
        Cols = cols;
        values = new float[rows * cols];
        Array.Copy(inputValues, values, inputValues.Length);
    }
    #endregion
    
    #region Getters
    // Gets the value at the specified row and column (zero-indexed)
    public float GetValue(int r, int c)
    {
        if (r < 0 || r >= Rows || c < 0 || c >= Cols)
            throw new IndexOutOfRangeException($"Matrix index out of range: ({r}, {c})");
        
        return values[r * Cols + c];
    }
    
    // Optionally expose internal values safely
    public float[] GetValuesCopy()
    {
        var copy = new float[values.Length];
        Array.Copy(values, copy, values.Length);
        return copy;
    }
    #endregion

    #region Conversion Methods
    // Converts a 4x1 matrix into a Coords (for transformation result use)
    public Coords AsCoords()
    {
        if (Rows == 4 && Cols == 1)
            return new Coords(values[0], values[1], values[2], values[3]);
        else
            throw new InvalidOperationException("Matrix must be 4x1 to convert to Coords.");
    }

    // Returns the matrix as a readable string for debugging
    public override string ToString()
    {
        string matrix = "";
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                matrix += values[r * Cols + c] + " ";
            }
            matrix += "\n";
        }

        return matrix;
    }
    #endregion
    
    #region Matrix Arithmetic Operators
    // Adds two matrices element-wise, returning a new matrix
    public static Matrix operator +(Matrix a, Matrix b)
    {
        if (a.Rows != b.Rows || a.Cols != b.Cols)
            throw new InvalidOperationException("Matrix addition failed: dimensions do not match.");

        float[] result = new float[a.values.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = a.values[i] + b.values[i];

        return new Matrix(a.Rows, a.Cols, result);
    }

    // Multiplies two matrices using standard matrix multiplication rules
    public static Matrix operator *(Matrix a, Matrix b)
    {
        if (a.Cols != b.Rows)
            throw new InvalidOperationException($"Matrix multiplication failed: {a.Rows}x{a.Cols} * {b.Rows}x{b.Cols}");

        float[] result = new float[a.Rows * b.Cols];

        for (int i = 0; i < a.Rows; i++)
        {
            for (int j = 0; j < b.Cols; j++)
            {
                float sum = 0f;
                for (int k = 0; k < a.Cols; k++)
                {
                    sum += a.values[i * a.Cols + k] * b.values[k * b.Cols + j];
                }
                result[i * b.Cols + j] = sum;
            }
        }

        return new Matrix(a.Rows, b.Cols, result);
    }
    #endregion
}
