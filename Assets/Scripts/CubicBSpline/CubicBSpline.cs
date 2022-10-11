using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubicBSpline
{
    private List<float> _tFactors;
    public List<Vector3> controlPoints = new List<Vector3>();

    public CubicBSpline(Vector3[] points)
    {
        this._tFactors = this.CalulateChordLengthAtEachPoint(points);
        Matrix4x4 matrixA = new Matrix4x4();

        // calculate matrix A
        for (int i = 0; i < points.Length; i++)
        {
            float B0 = B3_0(this._tFactors[i]);
            float B1 = B3_1(this._tFactors[i]);
            float B2 = B3_2(this._tFactors[i]);
            float B3 = B3_3(this._tFactors[i]);

            matrixA.m00 += B0 * B0;
            matrixA.m01 += B0 * B1;
            matrixA.m02 += B0 * B2;
            matrixA.m03 += B0 * B3;

            matrixA.m11 += B1 * B1;
            matrixA.m12 += B1 * B2;
            matrixA.m13 += B1 * B3;

            matrixA.m22 += B2 * B3;
            matrixA.m23 += B2 * B3;

            matrixA.m33 += B3 * B3;
        }

        matrixA.m10 = matrixA.m01;
        matrixA.m20 = matrixA.m02;
        matrixA.m21 = matrixA.m12;
        matrixA.m30 = matrixA.m03;
        matrixA.m31 = matrixA.m13;
        matrixA.m32 = matrixA.m23;

        // calculate vector B
        Vector4 vectorBPosX = new Vector4();
        Vector4 vectorBPosY = new Vector4();
        Vector4 vectorBPosZ = new Vector4();

        for (int i = 0; i < points.Length; i++)
        {
            float B0 = B3_0(this._tFactors[i]);
            float B1 = B3_1(this._tFactors[i]);
            float B2 = B3_2(this._tFactors[i]);
            float B3 = B3_3(this._tFactors[i]);

            float test = vectorBPosX[0];
            vectorBPosX[0] += B0 * points[i].x;
            vectorBPosX[1] += B1 * points[i].x;
            vectorBPosX[2] += B2 * points[i].x;
            vectorBPosX[3] += B3 * points[i].x;

            vectorBPosY[0] += B0 * points[i].y;
            vectorBPosY[1] += B1 * points[i].y;
            vectorBPosY[2] += B2 * points[i].y;
            vectorBPosY[3] += B3 * points[i].y;

            vectorBPosZ[0] += B0 * points[i].z;
            vectorBPosZ[1] += B1 * points[i].z;
            vectorBPosZ[2] += B2 * points[i].z;
            vectorBPosZ[3] += B3 * points[i].z;
        }

        // solve Ax = b
        Vector4 vectorXPosX = matrixA.inverse * vectorBPosX;
        Vector4 vectorXPosY = matrixA.inverse * vectorBPosY;
        Vector4 vectorXPosZ = matrixA.inverse * vectorBPosZ;
        for (int i = 0; i < 4; i++)
        {
            this.controlPoints.Add(new Vector3(vectorXPosX[i], 0, vectorXPosZ[i]));
        }


    }

    public float GetChordLengthAtPoint(int index)
    {
        return this._tFactors[index];
    }

    public Vector3 GetPosition(float t)
    {
        return this.controlPoints[0] * B3_0(t) + this.controlPoints[1] * B3_1(t) + this.controlPoints[2] * B3_2(t) + this.controlPoints[3] * B3_3(t);
    }

    public Vector3 GetVelocity(float t)
    {
        if (t == 0)
        {

            return this.GetPosition(t + 0.001f) - this.GetPosition(0);
        }
        return this.GetPosition(t) - this.GetPosition(t - 0.001f);
    }

    public Vector3 GetDirection(int frameIndex)
    {
        return this.GetVelocity(this._tFactors[frameIndex]).normalized;
    }

    public Matrix4x4 GetTranslationMatrix(int frameIndex)
    {

        Vector3 position = this.GetPosition(this._tFactors[frameIndex]);
        Matrix4x4 result = Matrix4x4.identity;
        result.m03 = position.x;
        result.m13 = position.y;
        result.m23 = position.z;
        return result;
    }

    public Quaternion GetQuaternion(int frameIndex)
    {
        Vector3 direction = this.GetDirection(frameIndex);
        return Quaternion.FromToRotation(Vector3.forward, direction);
    }

    private float B3_0(float t)
    {
        return 0.16667f * (1 - t) * (1 - t) * (1 - t);
    }

    private float B3_1(float t)
    {
        return 0.16667f * (3 * t * t * t - 6 * t * t + 4);

    }

    private float B3_2(float t)
    {
        return 0.16667f * (-3 * t * t * t + 3 * t * t + 3 * t + 1);
    }

    private float B3_3(float t)
    {
        return 0.16667f * t * t * t;
    }

    private List<float> CalulateChordLengthAtEachPoint(Vector3[] points)
    {
        List<float> result = new List<float>() { 0 };
        float totalChordLength = 0;

        for (int i = 1; i < points.Length; i++)
        {
            totalChordLength += Vector3.Distance(points[i], points[i - 1]);
        }

        for (int i = 1; i < points.Length; i++)
        {
            result.Add(result[i - 1] + Vector3.Distance(points[i], points[i - 1]) / totalChordLength);
        }

        result[points.Length - 1] = 1;

        return result;
    }
}
