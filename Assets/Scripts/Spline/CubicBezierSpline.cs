using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CubicBezierSpline
{
    public List<Vector3> knotPoints = new List<Vector3>();
    public bool useArcLength = true;

    public List<CubicBezierCurve> curves = new List<CubicBezierCurve>();
    private List<float> tFactors;
    private List<float> cumulativeDistances;
    private List<float> currentCumulativeDistances;

    public CubicBezierSpline(Vector3[] points, int desiredKnotPointCount)
    {
        this.CalulateTFactorAtEachPoint(points);
        this.CalculateCumlativeDistanceAtEachPoint(points);

        // decide knot points
        List<int> knotPointIndices = this.DecideKnotPointIndices(points, desiredKnotPointCount);
        knotPointIndices.ForEach(index => this.knotPoints.Add(points[index]));

        // fitting curve
        for (int i = 0; i < knotPointIndices.Count - 1; i++)
        {
            int startIndex = knotPointIndices[i];
            int length = knotPointIndices[i + 1] - startIndex + 1;
            this.curves.Add(new CubicBezierCurve(points.Skip(startIndex).Take(length).ToArray(), this.tFactors.Skip(startIndex).Take(length).ToArray(), i == 0 ? null : this.curves[i - 1]));
        }

        this.cumulativeDistances = this.currentCumulativeDistances = this.CalculateCurrentCumlativeDistanceAtEachPoint();
    }

    public Vector3 Interpolate(float t)
    {
        CubicBezierCurve curve = this.curves[this.ChooseCurve(t)];
        t = curve.NormalizeTFactor(t);
        return curve.Interpolate(t);
    }

    public void MoveKnotPoint(Vector3 newPos, int index)
    {
        newPos.y = 0;
        if (index == this.curves.Count)
        {
            this.curves[this.curves.Count - 1].knotPointB = newPos;
        }
        else
        {
            this.curves[index].knotPointA = newPos;
            if (index != 0)
            {
                this.curves[index - 1].knotPointB = newPos;
            }
        }

        if (index != 0 && index != this.curves.Count)
        {
            this.curves[index].EnsureContinuity(this.curves[index - 1]);
        }
        this.OnKnotPointChanged();
    }

    public Vector3 GetPosition(float frameIndex)
    {
        return this.Interpolate(this.useArcLength ? this.FindTFactorithArcLength(this.GetCumulativeDistance(frameIndex)) : this.GetTFactor(frameIndex));
    }

    public Vector3 GetVelocity(float frameIndex)
    {
        float t = this.useArcLength ? this.FindTFactorithArcLength(this.GetCumulativeDistance(frameIndex)) : this.GetTFactor(frameIndex);
        if (t == 0)
        {
            return this.Interpolate(t + 0.001f) - this.Interpolate((float)0);
        }
        return this.Interpolate(t) - this.Interpolate(t - 0.001f);
    }

    public Vector3 GetDirection(float frameIndex)
    {
        return this.GetVelocity(frameIndex).normalized;
    }

    public Quaternion GetQuaternion(float frameIndex)
    {
        Vector3 direction = this.GetDirection(frameIndex);
        return Quaternion.FromToRotation(Vector3.forward, direction);
    }

    public Matrix4x4 GetTranslationMatrix(float frameIndex)
    {
        Vector3 position = this.GetPosition(frameIndex);
        return Matrix4x4.Translate(position);
        // Matrix4x4 result = Matrix4x4.identity;
        // result.m03 = position.x;
        // result.m13 = position.y;
        // result.m23 = position.z;
        // return result;
    }

    private float GetCumulativeDistance(float frameIndex)
    {
        int low = Mathf.FloorToInt(frameIndex);
        int high = Mathf.CeilToInt(frameIndex);
        float alpha = frameIndex - low;
        if (low == high)
        {
            return this.cumulativeDistances[low];
        }
        return Mathf.Lerp(this.cumulativeDistances[low], this.cumulativeDistances[high], alpha);
    }

    private float GetTFactor(float frameIndex)
    {
        int low = Mathf.FloorToInt(frameIndex);
        int high = Mathf.CeilToInt(frameIndex);
        float alpha = frameIndex - low;
        if (low == high)
        {
            return this.tFactors[low];
        }
        return Mathf.Lerp(this.tFactors[low], this.tFactors[high], alpha);
    }

    private int ChooseCurve(float t)
    {
        for (int i = 0; i < this.curves.Count; i++)
        {
            if (t <= this.curves[i].endTFactor)
            {
                return i;
            }
        }
        throw new System.Exception("Should Never Happened");
    }

    private List<int> DecideKnotPointIndices(Vector3[] points, int desiredKnotPointCount)
    {
        int currentKnotPointIndex = 0;
        int[] result = new int[desiredKnotPointCount];
        float totalDistance = this.cumulativeDistances.Last();
        result[currentKnotPointIndex] = 0;
        currentKnotPointIndex++;
        for (int i = 0; i < this.tFactors.Count; i++)
        {
            if (this.cumulativeDistances[i] / totalDistance >= 1.0f / (desiredKnotPointCount - 1) * currentKnotPointIndex)
            {
                result[currentKnotPointIndex] = i;
                currentKnotPointIndex++;
                if (currentKnotPointIndex == desiredKnotPointCount - 1)
                {
                    break;
                }
            }
        }
        result[desiredKnotPointCount - 1] = points.Length - 1;
        return result.ToList();
    }

    private void CalulateTFactorAtEachPoint(Vector3[] points)
    {
        this.tFactors = new List<float>() { 0 };

        for (int i = 1; i < points.Length; i++)
        {
            this.tFactors.Add(this.tFactors[i - 1] + 1.0f / points.Length);
        }

        this.tFactors[points.Length - 1] = 1;
    }

    private void CalculateCumlativeDistanceAtEachPoint(Vector3[] points)
    {
        this.cumulativeDistances = new List<float>() { 0 };

        for (int i = 1; i < points.Length; i++)
        {
            this.cumulativeDistances.Add(this.cumulativeDistances[i - 1] + Vector3.Distance(points[i], points[i - 1]));
        }
    }

    private List<float> CalculateCurrentCumlativeDistanceAtEachPoint()
    {
        List<float> result = new List<float>() { 0 };

        for (int i = 1; i < this.tFactors.Count; i++)
        {
            result.Add(result[i - 1] + Vector3.Distance(this.Interpolate(this.tFactors[i]), this.Interpolate(this.tFactors[i - 1])));
        }

        return result;
    }

    private float FindTFactorithArcLength(float arcLength)
    {
        int index = -1;
        for (int i = 0; i < this.currentCumulativeDistances.Count; i++)
        {
            if (this.currentCumulativeDistances[i] >= arcLength)
            {
                index = i;
                break;
            }
        }
        if (index == -1)
        {
            return 1;
        }
        if (index == 0)
        {
            return 0;
        }
        float factor = (arcLength - this.currentCumulativeDistances[index - 1]) / (this.currentCumulativeDistances[index] - this.currentCumulativeDistances[index - 1]);
        return Mathf.Lerp(this.tFactors[index - 1], this.tFactors[index], factor);
    }

    private void OnKnotPointChanged()
    {
        this.currentCumulativeDistances = this.CalculateCurrentCumlativeDistanceAtEachPoint();
    }
}
