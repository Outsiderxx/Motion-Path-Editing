using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private BVHParser _bvhData;
    private Dictionary<HumanBodyBones, Quaternion> initBoneQuaternion = new Dictionary<HumanBodyBones, Quaternion>();
    private Dictionary<string, Quaternion> initSkeletonQuaternion = new Dictionary<string, Quaternion>();
    private Vector3 initSkeletonRootPosition = Vector3.zero;
    private float skeletonModelScale;
    public CubicBSpline spline = null;

    private static Dictionary<string, HumanBodyBones> humanBodyBoneIDMap = new Dictionary<string, HumanBodyBones>()
    {
        ["Hips"] = HumanBodyBones.Hips,
        ["Abdomen"] = HumanBodyBones.Spine,
        ["LeftHip"] = HumanBodyBones.LeftUpperLeg,
        ["LeftUpLeg"] = HumanBodyBones.LeftUpperLeg,
        ["LeftKnee"] = HumanBodyBones.LeftLowerLeg,
        ["LeftLowLeg"] = HumanBodyBones.LeftLowerLeg,
        ["LeftFoot"] = HumanBodyBones.LeftFoot,
        ["LeftAnkle"] = HumanBodyBones.LeftFoot,
        ["RightHip"] = HumanBodyBones.RightUpperLeg,
        ["RightUpLeg"] = HumanBodyBones.RightUpperLeg,
        ["RightKnee"] = HumanBodyBones.RightLowerLeg,
        ["RightLowLeg"] = HumanBodyBones.RightLowerLeg,
        ["RightFoot"] = HumanBodyBones.RightFoot,
        ["RightAnkle"] = HumanBodyBones.RightFoot,
        ["Chest"] = HumanBodyBones.Chest,
        ["LeftCollar"] = HumanBodyBones.LeftShoulder,
        ["LeftShoulder"] = HumanBodyBones.LeftUpperArm,
        ["LeftUpArm"] = HumanBodyBones.LeftUpperArm,
        ["LeftLowArm"] = HumanBodyBones.LeftLowerArm,
        ["LeftElbow"] = HumanBodyBones.LeftLowerArm,
        ["LeftWrist"] = HumanBodyBones.LeftHand,
        ["LeftHand"] = HumanBodyBones.LeftHand,
        ["RightCollar"] = HumanBodyBones.RightShoulder,
        ["RightShoulder"] = HumanBodyBones.RightUpperArm,
        ["RightUpArm"] = HumanBodyBones.RightUpperArm,
        ["RightLowArm"] = HumanBodyBones.RightLowerArm,
        ["RightElbow"] = HumanBodyBones.RightLowerArm,
        ["RightWrist"] = HumanBodyBones.RightHand,
        ["RightHand"] = HumanBodyBones.RightHand,
        ["Neck"] = HumanBodyBones.Neck,
        ["Head"] = HumanBodyBones.Head
    };

    public BVHParser bvhData
    {
        get
        {
            return this._bvhData;
        }
        set
        {
            this._bvhData = value;
            this.Init();
        }
    }

    private void Init()
    {
        Transform upArm = this.animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Transform lowArm = this.animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        // find quaternion to convert bvh to t pose ( left shoulder and right shoulder )
        foreach (BVHParser.BVHBone bone in this.bvhData.allBones)
        {
            Quaternion result = Quaternion.identity;
            if (bone.name == "LeftShoulder" || bone.name == "LeftUpArm")
            {
                result = Quaternion.AngleAxis(-90, Vector3.forward);
            }
            else if (bone.name == "RightShoulder" || bone.name == "RightUpArm")
            {
                result = Quaternion.AngleAxis(90, Vector3.forward);
            }
            else if (bone.name == "LeftLowArm" || bone.name == "LeftElbow")
            {
                result = Quaternion.AngleAxis(-90, Vector3.forward);
            }
            else if (bone.name == "RightLowArm" || bone.name == "RightElbow")
            {
                result = Quaternion.AngleAxis(90, Vector3.forward);
            }
            else if (bone.name == "LeftWrist" || bone.name == "LeftHand")
            {
                result = Quaternion.AngleAxis(-90, Vector3.forward);
            }
            else if (bone.name == "RightWrist" || bone.name == "RightHand")
            {
                result = Quaternion.AngleAxis(90, Vector3.forward);
            }
            this.initSkeletonQuaternion.Add(bone.name, result);
        }

        // save bone t pose rotation
        foreach (BVHParser.BVHBone bone in this.bvhData.allBones)
        {
            try
            {
                HumanBodyBones id = ModelController.humanBodyBoneIDMap[bone.name];
                Transform modelBone = this.animator.GetBoneTransform(id);
                this.initBoneQuaternion.Add(id, modelBone.rotation);
            }
            catch (System.Exception exception)
            {
                exception.ToString();
            }
        }

        // adjust scale
        float modelBoneLength = Vector3.Distance(this.animator.GetBoneTransform(HumanBodyBones.Head).position, this.animator.GetBoneTransform(HumanBodyBones.Neck).position);
        BVHParser.BVHBone nick = bvhData.GetBone("Neck");
        BVHParser.BVHBone head = bvhData.GetBone("Head");
        float skeletonBoneLength = Vector3.Distance(nick.transform.position, head.transform.position);
        this.skeletonModelScale = skeletonBoneLength / modelBoneLength;
        this.animator.transform.localScale = Vector3.one * skeletonModelScale;

        // set offset
        foreach (BVHParser.BVHBone bone in this.bvhData.allBones)
        {
            try
            {
                HumanBodyBones id = ModelController.humanBodyBoneIDMap[bone.name];
                Transform modelBone = this.animator.GetBoneTransform(id);
                modelBone.Translate(new Vector3(bone.offsetX, bone.offsetY, bone.endSiteOffsetZ) / this.skeletonModelScale * 2);
            }
            catch (System.Exception exception)
            {
                exception.ToString();
            }
        }

        BVHParser.BVHBone ankle = bvhData.GetBone("RightFoot");
        if (ankle == null)
        {
            ankle = bvhData.GetBone("RightAnkle");
        }
        this.initSkeletonRootPosition = -BVHParser.FindTotalOffset(ankle);
        this.initSkeletonRootPosition.x = 0;
        this.initSkeletonRootPosition.z = 0;
    }

    public void SetToFrameIndex(int frameIndex)
    {
        this.UpdateJointRotation(this._bvhData.root, frameIndex);
        this.UpdateRootWorldRotation(frameIndex);
        this.UpdateRootPosition(frameIndex);
    }

    private void UpdateRootPosition(int frameIndex)
    {
        Vector3 detail = new Vector3(this.bvhData.root.channels[0].values[frameIndex], this.bvhData.root.channels[1].values[frameIndex], this.bvhData.root.channels[2].values[frameIndex]) - this.initSkeletonRootPosition; ;
        this.transform.localPosition = this.spline.GetTranslationMatrix(frameIndex) * new Vector4(detail.x, detail.y, detail.z, 1);
    }

    private void UpdateRootWorldRotation(int frameIndex)
    {
        Quaternion rotation = this.spline.GetQuaternion(frameIndex);
        this.transform.rotation = rotation;
    }

    private void UpdateJointRotation(BVHParser.BVHBone joint, int frameIndex)
    {
        try
        {
            HumanBodyBones id = ModelController.humanBodyBoneIDMap[joint.name];
            Transform modelBone = this.animator.GetBoneTransform(id);
            modelBone.rotation = joint.transform.rotation * Quaternion.Inverse(this.initSkeletonQuaternion[joint.name]) * this.initBoneQuaternion[id];
        }
        catch (System.Exception exception)
        {
            exception.ToString();
        }

        foreach (BVHParser.BVHBone bone in joint.children)
        {
            this.UpdateJointRotation(bone, frameIndex);
        }
    }
}
