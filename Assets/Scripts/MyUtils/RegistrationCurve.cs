using System;
using System.Collections.Generic;
using UnityEngine;

public class RegistrationCurve
{
    private class TimewrapTableElement
    {
        public int previousRowIndex = 0;
        public int previousColumnIndex = 0;
        public int consecutiveRowIncreaseTimes = 0;
        public int consecutiveColumnIncreaseTimes = 0;
        public float cumulatedDistance = 0;
    }

    private static readonly int REFERENCE_NEIGHBOR_COUNT = 5;
    private List<Tuple<int, int>> timewrapCurve = new List<Tuple<int, int>>();
    private List<Tuple<Vector3, Vector3>> alignmentCurve = new List<Tuple<Vector3, Vector3>>();
    private float[,] distanceMap;
    private Vector3[,] transformationMap;
    private BVHMotion motionA;
    private BVHMotion motionB;

    public int timewrapCurveLength
    {
        get
        {
            return this.timewrapCurve.Count;
        }
    }

    public RegistrationCurve(BVHMotion motionA, BVHMotion motionB)
    {
        if (motionA.allBones.Count != motionB.allBones.Count)
        {
            throw new Exception("Skeleton structure mismatch");
        }

        this.motionA = motionA;
        this.motionB = motionB;

        this.CreateTransformationMap();
        this.CreateDistanceMap();
        this.CreateTimewrapCurve();
        this.CreateAlignmentCurve();
    }

    public Tuple<float, float> S(float u)
    {
        int low = Mathf.FloorToInt(u);
        int high = Mathf.CeilToInt(u);
        float alpha = u - low;
        if (low == high)
        {
            return new Tuple<float, float>(this.timewrapCurve[low].Item1, this.timewrapCurve[low].Item2);
        }
        return new Tuple<float, float>(Mathf.Lerp(this.timewrapCurve[low].Item1, this.timewrapCurve[high].Item1, alpha), Mathf.Lerp(this.timewrapCurve[low].Item2, this.timewrapCurve[high].Item2, alpha));
    }

    public Tuple<Vector3, Vector3> A(float u)
    {
        int low = Mathf.FloorToInt(u);
        int high = Mathf.CeilToInt(u);
        float alpha = u - low;
        if (low == high)
        {
            return new Tuple<Vector3, Vector3>(this.alignmentCurve[low].Item1, this.alignmentCurve[low].Item2);
        }
        return new Tuple<Vector3, Vector3>(Vector3.Lerp(this.alignmentCurve[low].Item1, this.alignmentCurve[high].Item1, alpha), Vector3.Lerp(this.alignmentCurve[low].Item2, this.alignmentCurve[high].Item2, alpha));
    }

    private void CreateTransformationMap()
    {
        this.transformationMap = new Vector3[this.motionA.frames, this.motionB.frames];
        for (int i = 0; i < this.motionA.frames; i++)
        {
            for (int j = 0; j < this.motionB.frames; j++)
            {
                Vector3[,] sequenceA = new Vector3[RegistrationCurve.REFERENCE_NEIGHBOR_COUNT, this.motionA.allBones.Count];
                Vector3[,] sequenceB = new Vector3[RegistrationCurve.REFERENCE_NEIGHBOR_COUNT, this.motionA.allBones.Count];
                for (int u = 0; u < 5; u++)
                {
                    int clippedI = Mathf.Clamp(i + u - 2, 0, this.motionA.frames - 1);
                    int clippedJ = Mathf.Clamp(j + u - 2, 0, this.motionB.frames - 1);
                    for (int v = 0; v < this.motionA.allBones.Count; v++)
                    {
                        sequenceA[u, v] = this.motionA.allBones[v].GetWorldPosition(clippedI);
                        sequenceB[u, v] = this.motionB.allBones[v].GetWorldPosition(clippedJ);
                    }
                }
                this.transformationMap[i, j] = RegistrationCurve.GetAlignmentTransformation(sequenceA, sequenceB);
            }
        }
    }

    private void CreateDistanceMap()
    {
        this.distanceMap = new float[this.motionA.frames, this.motionB.frames];
        for (int i = 0; i < this.motionA.frames; i++)
        {
            for (int j = 0; j < this.motionB.frames; j++)
            {
                Vector3 transformation = this.transformationMap[i, j];
                float distance = 0;
                for (int u = 0; u < this.motionA.allBones.Count; u++)
                {
                    Vector3 pointA = this.motionA.allBones[u].GetWorldPosition(i);
                    Vector3 pointB = this.motionB.allBones[u].GetWorldPosition(j);
                    distance += Vector3.Distance(pointA, MyUtils.MakeTransformationMatrix(transformation).MultiplyPoint(pointB));
                }
                this.distanceMap[i, j] = distance;
            }
        }
    }

    private void CreateTimewrapCurve()
    {
        int width = this.distanceMap.GetLength(0), height = this.distanceMap.GetLength(1);
        int lastI = 0, lastJ = 0;
        TimewrapTableElement[,] table = new TimewrapTableElement[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                // find path
                TimewrapTableElement left = j > 0 ? table[i, j - 1] : null;
                TimewrapTableElement leftUp = (j > 0 && i > 0) ? table[i - 1, j - 1] : null;
                TimewrapTableElement up = i > 0 ? table[i - 1, j] : null;
                float leftUpCumulativeDistance = leftUp != null ? leftUp.cumulatedDistance : Mathf.Infinity;
                float leftCumulativeDistance = left != null ? left.cumulatedDistance : Mathf.Infinity;
                float upCumulativeDistance = up != null ? up.cumulatedDistance : Mathf.Infinity;

                // ensure slope limit
                if (left != null && left.consecutiveColumnIncreaseTimes >= 2)
                {
                    leftCumulativeDistance = Mathf.Infinity;
                }
                if (up != null && up.consecutiveRowIncreaseTimes >= 2)
                {
                    upCumulativeDistance = Mathf.Infinity;
                }

                // choose path which has minimal cost
                if (leftUpCumulativeDistance < leftCumulativeDistance && leftUpCumulativeDistance < upCumulativeDistance)
                {
                    table[i, j] = new TimewrapTableElement();
                    table[i, j].consecutiveColumnIncreaseTimes = 0;
                    table[i, j].consecutiveRowIncreaseTimes = 0;
                    table[i, j].previousColumnIndex = j - 1;
                    table[i, j].previousRowIndex = i - 1;
                    table[i, j].cumulatedDistance = leftUpCumulativeDistance + distanceMap[i, j];
                    lastI = i;
                    lastJ = j;
                }
                else if (leftCumulativeDistance < upCumulativeDistance)
                {
                    table[i, j] = new TimewrapTableElement();
                    table[i, j].consecutiveColumnIncreaseTimes = left.consecutiveColumnIncreaseTimes + 1;
                    table[i, j].consecutiveRowIncreaseTimes = 0;
                    table[i, j].previousColumnIndex = j - 1;
                    table[i, j].previousRowIndex = i;
                    table[i, j].cumulatedDistance += leftCumulativeDistance + distanceMap[i, j];
                    lastI = i;
                    lastJ = j;
                }
                else if (upCumulativeDistance < Mathf.Infinity)
                {
                    table[i, j] = new TimewrapTableElement();
                    table[i, j].consecutiveColumnIncreaseTimes = 0;
                    table[i, j].consecutiveRowIncreaseTimes = up.consecutiveRowIncreaseTimes + 1;
                    table[i, j].previousColumnIndex = j;
                    table[i, j].previousRowIndex = i - 1;
                    table[i, j].cumulatedDistance += upCumulativeDistance + distanceMap[i, j];
                    lastI = i;
                    lastJ = j;
                }
                else if (i == 0 && j == 0)
                {
                    table[i, j] = new TimewrapTableElement();
                    table[i, j].consecutiveColumnIncreaseTimes = 0;
                    table[i, j].consecutiveRowIncreaseTimes = 0;
                    table[i, j].previousColumnIndex = -1;
                    table[i, j].previousRowIndex = -1;
                    table[i, j].cumulatedDistance = distanceMap[i, j];
                    lastI = i;
                    lastJ = j;
                }
            }
        }

        while (lastI >= 0 && lastJ >= 0)
        {
            TimewrapTableElement temp = table[lastI, lastJ];
            this.timewrapCurve.Add(new Tuple<int, int>(lastI, lastJ));
            lastI = temp.previousRowIndex;
            lastJ = temp.previousColumnIndex;
        }
        this.timewrapCurve.Reverse();
    }

    private void CreateAlignmentCurve()
    {
        for (int i = 0; i < this.timewrapCurve.Count; i++)
        {
            int frameIndexA = this.timewrapCurve[i].Item1;
            int frameIndexB = this.timewrapCurve[i].Item2;
            this.alignmentCurve.Add(new Tuple<Vector3, Vector3>(new Vector3(), this.transformationMap[frameIndexA, frameIndexB]));
        }

        // handle situation where the difference of delta of currentFrame and previosFrame more than 180 degree 
        for (int i = 1; i < this.alignmentCurve.Count; i++)
        {
            if (Mathf.Abs(this.alignmentCurve[i].Item2.x - this.alignmentCurve[i - 1].Item2.x) > 57 * 0.7)
            {
                float originTheta = this.alignmentCurve[i].Item2.x;
                float newTheta = originTheta + (this.alignmentCurve[i].Item2.x > this.alignmentCurve[i - 1].Item2.x ? -180 : 180);

                Vector3 originTranslation = new Vector3(this.alignmentCurve[i].Item2.y, 0, this.alignmentCurve[i].Item2.z);
                int frameIndexB = this.timewrapCurve[i].Item2;
                Vector3 motionBRootPos = this.motionB.root.GetLocalPosition(frameIndexB); ;
                Vector3 newTranslation = originTranslation + Quaternion.Euler(0, originTheta, 0) * motionBRootPos - Quaternion.Euler(0, newTheta, 0) * motionBRootPos;

                this.alignmentCurve[i] = new Tuple<Vector3, Vector3>(new Vector3(), new Vector3(newTheta, newTranslation.x, newTranslation.z));
            }
        }
    }

    private static Vector3 GetAlignmentTransformation(Vector3[,] sequenceA, Vector3[,] sequenceB)
    {
        float theta, x0, z0;
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

        theta = Mathf.Atan((tanA1 - tanA2) / (tanB1 - tanB2));
        x0 = xA - xB * Mathf.Cos(theta) - zB * Mathf.Sin(theta);
        z0 = zA - xB * Mathf.Sin(theta) - zB * Mathf.Cos(theta);
        return new Vector3(theta, x0, z0);
    }
}
