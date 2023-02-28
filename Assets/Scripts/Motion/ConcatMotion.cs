using UnityEngine;
using System;

public static class ConcatMotion
{
    private static int interpolateFrameCount = 10;
    public static void Concat(BVHMotion motionA, BVHMotion motionB)
    {
        Vector3 motionALastPosition = motionA.root.GetLocalPosition(motionA.frames - 1);
        Vector3 motionBFirstPosition = motionB.root.GetLocalPosition(0);
        Vector3 positionDiff = motionBFirstPosition - motionALastPosition;
        for (int i = 0; i < motionB.frames; i++)
        {
            motionB.root.channels[0].values[i] -= positionDiff.x;
            motionB.root.channels[1].values[i] -= positionDiff.y;
            motionB.root.channels[2].values[i] -= positionDiff.z;
        }

        foreach (var bone in motionA.allBones)
        {
            BVHMotion.BVHBone boneB = motionB.allBones.Find(ele => ele.name == bone.name);
            if (boneB == null)
            {
                throw new Exception("Skeleton match failed");
            }
            // add interpolate frames
            for (int i = 0; i < ConcatMotion.interpolateFrameCount; i++)
            {
                // position
                if (bone.isRoot)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Array.Resize(ref bone.channels[j].values, motionA.frames + motionB.frames + ConcatMotion.interpolateFrameCount);
                        bone.channels[j].values[motionA.frames + i] = Mathf.Lerp(bone.channels[j].values[motionA.frames - 1], boneB.channels[j].values[0], (float)(i) / ConcatMotion.interpolateFrameCount);
                    }
                }
                // quaternion
                Array.Resize(ref bone.quaternions, motionA.frames + motionB.frames + ConcatMotion.interpolateFrameCount);
                bone.quaternions[motionA.frames + i] = Quaternion.Lerp(bone.quaternions[motionA.frames - 1], boneB.quaternions[0], (float)(i) / ConcatMotion.interpolateFrameCount);
            }

            // add data of motionB to motionA
            for (int i = 0; i < motionB.frames; i++)
            {
                // position
                if (bone.isRoot)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        bone.channels[j].values[motionA.frames + ConcatMotion.interpolateFrameCount + i] = boneB.channels[j].values[i];
                    }
                }
                // quaternion
                bone.quaternions[motionA.frames + ConcatMotion.interpolateFrameCount + i] = boneB.quaternions[i];
            }
        }

        motionA.frames = motionA.frames + motionB.frames + ConcatMotion.interpolateFrameCount;
    }
}
