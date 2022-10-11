using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubicBSplineController : MonoBehaviour
{
    private CubicBSpline _spline;
    private List<Transform> controlPoints = new List<Transform>();

    public CubicBSpline spline
    {
        get
        {
            return this._spline;
        }
        set
        {
            this._spline = value;
            this.SpawnControlPoints();
        }
    }

    private void Update()
    {
        this.DrawPath();
        for (int i = 0; i < this.controlPoints.Count; i++)
        {
            if (this.controlPoints[i].hasChanged)
            {
                this._spline.controlPoints[i] = this.controlPoints[i].localPosition;
            }
        }
    }

    private void SpawnControlPoints()
    {
        foreach (Vector3 controlPoint in this._spline.controlPoints)
        {
            GameObject controlPointEntity = GameObject.CreatePrimitive(PrimitiveType.Cube);
            controlPointEntity.transform.localScale = Vector3.one * 3;
            controlPointEntity.transform.parent = this.transform;
            controlPointEntity.transform.localPosition = controlPoint;
            controlPoints.Add(controlPointEntity.transform);
        }
    }

    private void DrawPath()
    {
        for (float i = 0; i < 1; i += 0.001f)
        {
            Vector3 localPositionA = this._spline.GetPosition(i);
            Vector3 localPositionB = this._spline.GetPosition(i + 0.001f);
            Debug.DrawLine(this.transform.TransformPoint(localPositionA), this.transform.TransformPoint(localPositionB), Color.yellow);
        }
    }

    private void Reset()
    {
        this._spline = null;
        this.controlPoints.Clear();
    }
}
