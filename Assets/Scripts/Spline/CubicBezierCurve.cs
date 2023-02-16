using UnityEngine;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

public class CubicBezierCurve
{
    public Vector3 knotPointA = new Vector3();
    public Vector3 knotPointB = new Vector3();
    public Vector3 controlPointA = new Vector3();
    public Vector3 controlPointB = new Vector3();
    public float startTFactor = 0;
    public float endTFactor = 0;

    public CubicBezierCurve(Vector3[] dataPoints, float[] tFactors, CubicBezierCurve previousCurve)
    {
        this.startTFactor = tFactors[0];
        this.endTFactor = tFactors[tFactors.Length - 1];
        tFactors = tFactors.Select(tFactor => this.NormalizeTFactor(tFactor)).ToArray();

        this.knotPointA = dataPoints[0];
        this.knotPointA.y = 0;
        this.knotPointB = dataPoints[dataPoints.Length - 1];
        this.knotPointB.y = 0;
        this.controlPointA = Vector3.Lerp(this.knotPointA, this.knotPointB, 1 / 3.0f);
        this.controlPointB = Vector3.Lerp(this.knotPointA, this.knotPointB, 2 / 3.0f);
        if (previousCurve != null)
        {
            this.EnsureContinuity(previousCurve);
        }

        // if (previousCurve == null)
        // {
        //     // x dimension
        //     Matrix<float> matrixA = this.CreateMatrixA(dataPoints.Select(dataPoint => dataPoint.x).ToArray(), tFactors);
        //     Vector<float> vectorB = this.CreateVectorB(dataPoints.Select(dataPoint => dataPoint.x).ToArray(), tFactors);
        //     Vector<float> vectorX = matrixA.PseudoInverse().Multiply(vectorB);
        //     this.controlPointA.x = vectorX[0] / vectorX[2];
        //     this.controlPointB.x = vectorX[1] / vectorX[2];

        //     // // y dimension
        //     // matrixA = this.CreateMatrixA(dataPoints.Select(dataPoint => dataPoint.y).ToArray(), tFactors);
        //     // vectorB = this.CreateVectorB(dataPoints.Select(dataPoint => dataPoint.y).ToArray(), tFactors);
        //     // vectorX = matrixA.PseudoInverse().Multiply(vectorB);
        //     // this.controlPointA.y = vectorX[0] / vectorX[2];
        //     // this.controlPointB.y = vectorX[1] / vectorX[2];

        //     // z dimension
        //     matrixA = this.CreateMatrixA(dataPoints.Select(dataPoint => dataPoint.z).ToArray(), tFactors);
        //     vectorB = this.CreateVectorB(dataPoints.Select(dataPoint => dataPoint.z).ToArray(), tFactors);
        //     vectorX = matrixA.PseudoInverse().Multiply(vectorB);
        //     this.controlPointA.z = vectorX[0] / vectorX[2];
        //     this.controlPointB.z = vectorX[1] / vectorX[2];
        // }
        // else
        // {
        //     this.controlPointA.x = this.knotPointA.x * 2 - previousCurve.controlPointB.x;
        //     this.controlPointA.z = this.knotPointA.z * 2 - previousCurve.controlPointB.z;

        //     float a = 0, b = 0, c = 0;

        //     // x dimension
        //     for (int i = 0; i < dataPoints.Length; i++)
        //     {
        //         float coefficientA = CubicBezierCurve.CoefficientA(tFactors[i]);
        //         float coefficientB = CubicBezierCurve.CoefficientB(tFactors[i]);
        //         float coefficientC = CubicBezierCurve.CoefficientC(tFactors[i]);
        //         float coefficientD = CubicBezierCurve.CoefficientD(tFactors[i]);
        //         a += coefficientC * coefficientC;
        //         b += (coefficientC * coefficientA * this.knotPointA.x) + (coefficientC * coefficientB * this.controlPointA.x) + (coefficientC * coefficientD * this.knotPointB.x);
        //         c += coefficientC * dataPoints[i].x;
        //     }
        //     this.controlPointB.x = (c - b) / a;

        //     // z dimension
        //     for (int i = 0; i < dataPoints.Length; i++)
        //     {
        //         float coefficientA = CubicBezierCurve.CoefficientA(tFactors[i]);
        //         float coefficientB = CubicBezierCurve.CoefficientB(tFactors[i]);
        //         float coefficientC = CubicBezierCurve.CoefficientC(tFactors[i]);
        //         float coefficientD = CubicBezierCurve.CoefficientD(tFactors[i]);
        //         a += coefficientC * coefficientC;
        //         b += (coefficientC * coefficientA * this.knotPointA.z) + (coefficientC * coefficientB * this.controlPointA.z) + (coefficientC * coefficientD * this.knotPointB.z);
        //         c += coefficientC * dataPoints[i].z;
        //     }
        //     this.controlPointB.z = (c - b) / a;

        //     this.EnsureContinuity(previousCurve);
        // }
    }

    public void EnsureContinuity(CubicBezierCurve previousCurve)
    {
        this.controlPointA.x = this.knotPointA.x * 2 - previousCurve.controlPointB.x;
        this.controlPointA.z = this.knotPointA.z * 2 - previousCurve.controlPointB.z;
        Debug.Log(this.controlPointA);
    }

    public float NormalizeTFactor(float tFactor)
    {
        return (tFactor - this.startTFactor) / (this.endTFactor - this.startTFactor);
    }

    public Vector3 Interpolate(float t)
    {
        return this.knotPointA * CubicBezierCurve.CoefficientA(t) + this.controlPointA * CubicBezierCurve.CoefficientB(t) + this.controlPointB * CubicBezierCurve.CoefficientC(t) + this.knotPointB * CubicBezierCurve.CoefficientD(t);
    }

    private Matrix<float> CreateMatrixA(float[] values, float[] tFactors)
    {
        Vector<float> firstRow = CreateVector.Dense<float>(3);
        Vector<float> lastRow = CreateVector.Dense<float>(3);
        for (int i = 0; i < values.Length; i++)
        {
            float coefficientA = CubicBezierCurve.CoefficientA(tFactors[i]);
            float coefficientB = CubicBezierCurve.CoefficientB(tFactors[i]);
            float coefficientC = CubicBezierCurve.CoefficientC(tFactors[i]);
            float coefficientD = CubicBezierCurve.CoefficientD(tFactors[i]);
            firstRow[0] += coefficientB * coefficientB;
            firstRow[1] += coefficientB * coefficientC;
            firstRow[2] += (coefficientB * coefficientA * values[0]) + (coefficientB * coefficientD * values[values.Length - 1]);
            lastRow[0] += coefficientB * coefficientC;
            lastRow[1] += coefficientC * coefficientC;
            lastRow[2] += (coefficientC * coefficientA * values[0]) + (coefficientC * coefficientD * values[values.Length - 1]);

        }
        return CreateMatrix.DenseOfRowVectors(firstRow, lastRow);
    }

    private Vector<float> CreateVectorB(float[] values, float[] tFactors)
    {
        Vector<float> result = CreateVector.Dense<float>(2);
        for (int i = 0; i < values.Length; i++)
        {
            float coefficientB = CubicBezierCurve.CoefficientB(tFactors[i]);
            float coefficientC = CubicBezierCurve.CoefficientC(tFactors[i]);
            result[0] += coefficientB * values[i];
            result[0] += coefficientC * values[i];
        }
        return result;
    }

    private static float CoefficientA(float tFactor)
    {
        return Mathf.Pow(1 - tFactor, 3);
    }

    private static float CoefficientB(float tFactor)
    {
        return 3 * tFactor * Mathf.Pow(1 - tFactor, 2);
    }

    private static float CoefficientC(float tFactor)
    {
        return 3 * Mathf.Pow(tFactor, 2) * (1 - tFactor);
    }

    private static float CoefficientD(float tFactor)
    {
        return Mathf.Pow(tFactor, 3);
    }
}