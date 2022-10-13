using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubicBSplineController : MonoBehaviour
{
    [SerializeField] private LineRenderer pathLine;
    public bool useArcLength = true;

    private static int lineSegmentCount = 1000;
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

    private void Start()
    {
        this.pathLine.positionCount = CubicBSplineController.lineSegmentCount;
    }

    private void Update()
    {
        for (int i = 0; i < this.controlPoints.Count; i++)
        {
            if (this.controlPoints[i].hasChanged)
            {
                this._spline.controlPoints[i] = this.controlPoints[i].localPosition;
                this._spline.OnControlPointsChanged();
            }
        }
        this.DrawPath();
        this._spline.useArcLength = this.useArcLength;
    }

    public void ToggleUseArcLengthMode()
    {
        this._spline.useArcLength = !this._spline.useArcLength;
        this.useArcLength = this._spline.useArcLength;
    }

    private void SpawnControlPoints()
    {
        foreach (Vector3 controlPoint in this._spline.controlPoints)
        {
            GameObject controlPointEntity = GameObject.CreatePrimitive(PrimitiveType.Cube);
            controlPointEntity.transform.localScale = Vector3.one * 7;
            controlPointEntity.transform.parent = this.transform;
            controlPointEntity.transform.localPosition = controlPoint;
            controlPointEntity.layer = 3;
            controlPoints.Add(controlPointEntity.transform);
        }
    }

    private void DrawPath()
    {
        for (int i = 0; i < CubicBSplineController.lineSegmentCount; i++)
        {
            Vector3 localPositionA = this._spline.GetPositionWithTime((float)i / CubicBSplineController.lineSegmentCount);
            localPositionA.y = 1;
            this.pathLine.SetPosition(i, this.transform.TransformPoint(localPositionA));
        }
    }

    private void Reset()
    {
        this._spline = null;
        this.controlPoints.Clear();
    }
}
