using System.Collections.Generic;
using UnityEngine;

public class SplineController : MonoBehaviour
{
    [SerializeField] private LineRenderer pathLine;

    private static int lineSegmentCount = 1000;
    private CubicBezierSpline _spline;
    private List<Transform> knotPoints = new List<Transform>();

    public CubicBezierSpline spline
    {
        get
        {
            return this._spline;
        }
        set
        {
            this._spline = value;
            this.SpawnknotPoints();
        }
    }

    private void Start()
    {
        this.pathLine.positionCount = SplineController.lineSegmentCount;
    }

    private void Update()
    {
        for (int i = 0; i < this.knotPoints.Count; i++)
        {
            if (this.knotPoints[i].hasChanged)
            {
                this.knotPoints[i].hasChanged = false;
                this._spline.MoveKnotPoint(this.knotPoints[i].localPosition, i);
            }
        }
        this.DrawPath();
    }

    private void SpawnknotPoints()
    {
        foreach (Vector3 knotPoint in this._spline.knotPoints)
        {
            GameObject controlPointEntity = GameObject.CreatePrimitive(PrimitiveType.Cube);
            controlPointEntity.transform.localScale = Vector3.one * 7;
            controlPointEntity.transform.parent = this.transform;
            controlPointEntity.transform.localPosition = knotPoint;
            controlPointEntity.layer = 3;
            knotPoints.Add(controlPointEntity.transform);
        }
    }

    private void DrawPath()
    {
        for (int i = 0; i < SplineController.lineSegmentCount; i++)
        {
            Vector3 localPositionA = this._spline.Interpolate((float)i / SplineController.lineSegmentCount);
            localPositionA.y = 1;
            this.pathLine.SetPosition(i, this.transform.TransformPoint(localPositionA));
        }
    }

    private void Reset()
    {
        this._spline = null;
        this.knotPoints.Clear();
    }
}
