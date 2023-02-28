using UnityEngine;
using System;

public static class MyUtils
{
    public static void DestroyRecursively(Transform root)
    {
        foreach (Transform child in root.transform)
        {
            DestroyRecursively(child);
            GameObject.Destroy(child.gameObject);
        }
        GameObject.Destroy(root.gameObject);
    }

    public static Tuple<float, float> W(float f, float[] weights)
    {
        int low = Mathf.FloorToInt(f);
        int high = Mathf.CeilToInt(f);
        float alpha = f - low;
        float result = Mathf.Lerp(weights[low], weights[high], alpha);
        return new Tuple<float, float>(result, 1 - result);
    }

    public static Matrix4x4 MakeTransformationMatrix(Vector3 transformation)
    {
        return Matrix4x4.Translate(new Vector3(transformation.y, 0, transformation.z)) * Matrix4x4.Rotate(Quaternion.Euler(0, transformation.x, 0));
    }

    public static Vector3 MakeTransformationVector(Matrix4x4 matrix)
    {
        Tuple<Vector3, Quaternion, Vector3> transformation = MyUtils.DecomposeMatrix4x4(matrix);
        float theta = transformation.Item2.eulerAngles.y;
        if (Mathf.Abs(theta) > 180)
        {
            theta = theta + (theta > 0 ? -360 : 360);
        }
        return new Vector3(theta, transformation.Item1.x, transformation.Item1.z);
    }

    public static Tuple<Vector3, Quaternion, Vector3> DecomposeMatrix4x4(Matrix4x4 matrix)
    {
        Vector3 translation = matrix.GetColumn(3);
        Quaternion rotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
        Vector3 scale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
        return new Tuple<Vector3, Quaternion, Vector3>(translation, rotation, scale);
    }
}
