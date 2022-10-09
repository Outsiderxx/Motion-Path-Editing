using UnityEngine;

public class BVHMotionPlayer : MonoBehaviour
{
    public bool isLoop = false;
    [Range(0, 2)]
    public float speed = 1;

    private BVHParser bvhData;
    private float _currentTime = 0;
    private int _currentFrameIndex = -1;

    public bool isPlaying
    {
        get
        {
            return this.bvhData != null && this.isLoop || (this.bvhData.frames > this.currentFrameIndex);
        }
    }

    private int currentFrameIndex
    {
        get
        {
            return this._currentFrameIndex;
        }
        set
        {
            if (value == this._currentFrameIndex)
            {
                return;
            }
            if (value >= this.bvhData.frames)
            {
                if (!this.isLoop)
                {
                    return;
                }
                this._currentTime = 0;
                this._currentFrameIndex = 0;
            }
            else
            {
                this._currentFrameIndex = value;
            }
            this.NextFrame();
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        if (this.bvhData == null)
        {
            return;
        }
        this.UpdateAnimationTime();
        this.DrawBone(this.bvhData.root.translform);
    }

    public void Play(BVHParser bvhData)
    {
        if (this.bvhData != null)
        {
            this.ResetState();
        }
        this.bvhData = bvhData;
        this.CreateSkeleton();
    }

    public void Stop()
    {
        this.ResetState();
    }

    private void CreateSkeleton()
    {
        GameObject root = this.AddJoint(this.bvhData.root, this.transform);
    }

    private GameObject AddJoint(BVHParser.BVHBone jointData, Transform parent)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.name = jointData.name;
        gameObject.transform.parent = parent;
        gameObject.transform.localPosition = new Vector3(-jointData.offsetX, jointData.offsetY, jointData.offsetZ);
        jointData.translform = gameObject.transform;
        foreach (var childJoint in jointData.children)
        {
            GameObject child = this.AddJoint(childJoint, gameObject.transform);
        }
        if (jointData.children.Count == 0)
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            child.name = jointData.name + "_EndEffector";
            child.transform.parent = gameObject.transform;
            child.transform.localPosition = new Vector3(-jointData.endSiteOffsetX, jointData.endSiteOffsetY, jointData.endSiteOffsetZ);
            jointData.endSiteTransform = child.transform;
        }
        return gameObject;
    }

    private void NextFrame()
    {
        this.UpdateRootPosition();
        this.UpdateJointRotation(this.bvhData.root);
        print($"currentFrameIndex: {this.currentFrameIndex}");
    }

    private void UpdateAnimationTime()
    {
        this._currentTime += Time.deltaTime * this.speed;
        this.currentFrameIndex = (int)(this._currentTime / this.bvhData.frameTime);
    }

    private void UpdateRootPosition()
    {
        this.bvhData.root.translform.localPosition = new Vector3(-this.bvhData.root.channels[0].values[this.currentFrameIndex], this.bvhData.root.channels[1].values[this.currentFrameIndex], this.bvhData.root.channels[2].values[this.currentFrameIndex]);
    }

    private void UpdateJointRotation(BVHParser.BVHBone jointData)
    {
        Transform jointTransform = jointData.translform;
        float rotationX = jointData.channels[3].values[this.currentFrameIndex];
        float rotationY = -jointData.channels[4].values[this.currentFrameIndex];
        float rotationZ = -jointData.channels[5].values[this.currentFrameIndex];
        jointTransform.localRotation = Quaternion.AngleAxis(rotationZ, Vector3.forward);
        jointTransform.localRotation *= Quaternion.AngleAxis(rotationX, Vector3.right);
        jointTransform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.up);
        foreach (var childJointData in jointData.children)
        {
            this.UpdateJointRotation(childJointData);
        }
    }

    private void DrawBone(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Debug.DrawLine(parent.position, child.position, Color.red);
            this.DrawBone(child);
        }
    }

    private void ResetState()
    {
        this.bvhData = null;
        this._currentTime = 0;
        this._currentFrameIndex = 0;
        MyUtils.DestroyAllChild(this.transform);
    }
}
