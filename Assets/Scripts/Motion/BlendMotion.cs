using System;
using System.Collections.Generic;
using UnityEngine;

public static class BlendMotion
{
    public static BVHMotion Blend(BVHMotion motionA, BVHMotion motionB, float[] weights)
    {
        BVHMotion blendMotion = motionA.Clone();
        RegistrationCurve registrationCurve = new RegistrationCurve(motionA, motionB);
        List<Vector3> blendedRootPositions = new List<Vector3>();
        List<List<Quaternion>> blendedJointRotations = new List<List<Quaternion>>();

        int t = 0, deltaT = 1;
        float u = 0, deltaU = 0;
        float du = 1.0f / registrationCurve.timewrapCurveLength, dA = 1.0f / motionA.frames, dB = 1.0f / motionB.frames;
        List<Vector3> T = new List<Vector3>() { new Vector3() };
        Tuple<float, float> w = MyUtils.W(0, weights);

        while (true)
        {
            List<Quaternion> oneFrameBlendedJointRotations = new List<Quaternion>();
            Vector3 oneFrameBlendedRootPosition;
            Matrix4x4 transformationA = MyUtils.MakeTransformationMatrix(registrationCurve.A(u).Item1);
            Matrix4x4 transformationB = MyUtils.MakeTransformationMatrix(registrationCurve.A(u).Item2);
            Tuple<float, float> frameIndex = registrationCurve.S(u);

            // root position
            oneFrameBlendedRootPosition = w.Item1 * (transformationA.MultiplyPoint(motionA.root.GetLocalPosition(frameIndex.Item1))) + w.Item2 * (transformationB.MultiplyPoint(motionB.root.GetLocalPosition(frameIndex.Item2)));
            oneFrameBlendedRootPosition = MyUtils.MakeTransformationMatrix(T[t]).MultiplyPoint(oneFrameBlendedRootPosition);

            // joint rotation
            for (int i = 0; i < motionA.allBones.Count; i++)
            {
                oneFrameBlendedJointRotations.Add(Quaternion.Lerp(motionA.allBones[i].GetLocalQuaternion(frameIndex.Item1), motionB.allBones[i].GetLocalQuaternion(frameIndex.Item2), w.Item1));
            }

            blendedRootPositions.Add(oneFrameBlendedRootPosition);
            blendedJointRotations.Add(oneFrameBlendedJointRotations);

            // progress
            deltaU = w.Item1 * (du / dA) + w.Item2 * (du / dB);
            t += deltaT;
            u += deltaU;

            if (u > registrationCurve.timewrapCurveLength - 1)
            {
                break;
            }

            // calculate for new T
            float[] weightList = new float[] { w.Item1, w.Item2 };
            Vector3 newT = new Vector3();
            for (int i = 0; i < 2; i++)
            {
                Matrix4x4 previousTMatrix = MyUtils.MakeTransformationMatrix(T[t - deltaT]);
                Tuple<Vector3, Vector3> previousAlignment = registrationCurve.A(u - deltaU);
                Tuple<Vector3, Vector3> newAlignment = registrationCurve.A(u);
                Matrix4x4 previousAlignmentMatrix = MyUtils.MakeTransformationMatrix(i == 0 ? previousAlignment.Item1 : previousAlignment.Item2);
                Matrix4x4 newAlignmentMatrix = MyUtils.MakeTransformationMatrix(i == 0 ? newAlignment.Item1 : newAlignment.Item2);
                newT += weightList[i] * MyUtils.MakeTransformationVector(previousTMatrix * previousAlignmentMatrix * newAlignmentMatrix.inverse);
            }
            T.Add(newT);

            // update weight
            w = MyUtils.W(registrationCurve.S(u).Item1, weights);
        }

        // reset motion frame count
        int newFrameCount = blendedRootPositions.Count;
        blendMotion.frames = newFrameCount;
        blendMotion.root.channels[0].values = new float[newFrameCount];
        blendMotion.root.channels[1].values = new float[newFrameCount];
        blendMotion.root.channels[2].values = new float[newFrameCount];
        for (int i = 0; i < motionA.allBones.Count; i++)
        {
            blendMotion.allBones[i].quaternions = new Quaternion[newFrameCount];
        }

        // fill new blend data into blend motion
        for (int i = 0; i < blendedRootPositions.Count; i++)
        {
            blendMotion.root.channels[0].values[i] = blendedRootPositions[i].x;
            blendMotion.root.channels[1].values[i] = blendedRootPositions[i].y;
            blendMotion.root.channels[2].values[i] = blendedRootPositions[i].z;
            for (int j = 0; j < motionA.allBones.Count; j++)
            {
                blendMotion.allBones[j].quaternions[i] = blendedJointRotations[i][j];
            }
        }

        return blendMotion;
    }
}
