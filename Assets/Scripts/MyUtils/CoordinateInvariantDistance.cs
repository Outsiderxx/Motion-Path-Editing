using UnityEngine;
using System.Collections.Generic;

public class CoordinateInvariantDistance
{
    public float distance { get; private set; } = 0;
    private float theta = 0;
    private float x0 = 0;
    private float z0 = 0;

    public Quaternion rotation
    {
        get
        {
            return Quaternion.Euler(0, this.theta, 0);
        }
    }

    public Vector3 translation
    {
        get
        {
            return new Vector3(this.x0, 0, this.z0);
        }
    }

    public CoordinateInvariantDistance(Vector3[,] sequenceA, Vector3[,] sequenceB)
    {
        float weight = 1.0f / sequenceA.Length;
        float xA = 0, zA = 0, xB = 0, zB = 0;
        float tanA1 = 0, tanA2 = 0, tanB1 = 0, tanB2 = 0;

        // calculate theta、x0、z0
        for (int i = 0; i < sequenceA.GetLength(0); i++)
        {
            for (int j = 0; j < sequenceA.GetLength(1); j++)
            {
                Vector3 pointA = sequenceA[i, j], pointB = sequenceB[i, j];
                xA += pointA.x * weight;
                zA += pointA.z * weight;
                xB += pointB.x * weight;
                zB += pointB.z * weight;

                tanA1 += (pointA.x * pointB.z - pointB.x * pointA.z) * weight;
                tanB1 += (pointA.x * pointB.x + pointA.z * pointB.z) * weight;
            }
        }
        tanA2 = xA * zB - xB * zA;
        tanB2 = xA * xB + zA * zB;

        this.theta = Mathf.Atan((tanA1 - tanA2) / (tanB1 - tanB2));
        this.x0 = xA - xB * Mathf.Cos(this.theta) - zB * Mathf.Sin(this.theta);
        this.z0 = zA - xB * Mathf.Sin(this.theta) - zB * Mathf.Cos(this.theta);

        // calculate distance
        for (int i = 0; i < sequenceA.GetLength(1); i++)
        {
            distance += Vector3.Distance(sequenceA[2, i], this.TransformPoint(sequenceB[2, i]));
        }
    }

    public Vector3 TransformPoint(Vector3 originPoint)
    {
        return Quaternion.Euler(0, this.theta, 0) * (originPoint + new Vector3(this.x0, 0, this.z0));
    }
}
