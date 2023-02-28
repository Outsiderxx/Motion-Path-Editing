using UnityEngine;

public class MotionPlayer : MonoBehaviour
{
    public bool isLoop = false;
    [Range(0, 2)]
    public float speed = 1;
    public int desiredKnotPointCount = 2;
    public SplineController splineController;
    [SerializeField] private ModelController modelController;
    [SerializeField] private LineRenderer motionLine;

    public BVHMotion bvhData { get; private set; }
    private float _currentTime = 0;
    private float _currentFrameIndex = -1;
    private bool _displayModel = false;

    public bool displayModel
    {
        get
        {
            return this._displayModel;
        }
        set
        {
            if (this._displayModel == value)
            {
                return;
            }
            this._displayModel = value;
            this.bvhData.root.transform.gameObject.SetActive(!value);
            this.modelController.gameObject.SetActive(value);
        }
    }

    public bool isPlaying
    {
        get
        {
            return this.bvhData != null && this.isLoop || (this.bvhData.frames > this.currentFrameIndex);
        }
    }

    public float currentFrameIndex
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
            if (value > this.bvhData.frames - 1)
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

    void Update()
    {
        if (this.bvhData == null)
        {
            return;
        }
        this.UpdateAnimationTime();
        this.DrawMotion();
    }

    public void Play(BVHMotion bvhData)
    {
        if (this.bvhData != null)
        {
            this.ResetState();
        }
        this.bvhData = bvhData;
        this.motionLine.positionCount = bvhData.frames;
        this.CreateSpline();
        this.CreateSkeleton();
        this.TransformToLocalCoordinate();
        this.modelController.bvhData = bvhData;
        this.modelController.spline = this.splineController.spline;
        this.modelController.gameObject.SetActive(false);
    }

    public void Stop()
    {
        this.ResetState();
    }

    public void Restart()
    {
        this._currentTime = 0;
    }

    private void CreateSkeleton()
    {
        this.AddJoint(this.bvhData.root, this.transform);
        this.AddBone(this.bvhData.root);
    }

    private void CreateSpline()
    {
        Vector3[] points = new Vector3[bvhData.frames];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = bvhData.root.GetLocalPosition(i);
        }
        this.splineController.spline = new CubicBezierSpline(points, this.desiredKnotPointCount);
    }

    private void TransformToLocalCoordinate()
    {
        for (int i = 0; i < this.bvhData.frames; i++)
        {
            Vector3 worldPosition = this.bvhData.root.GetLocalPosition(i);
            Vector3 localPosition = this.splineController.spline.GetTranslationMatrix(i).inverse.MultiplyPoint(worldPosition);
            this.bvhData.root.channels[0].values[i] = localPosition.x;
            this.bvhData.root.channels[1].values[i] = localPosition.y;
            this.bvhData.root.channels[2].values[i] = localPosition.z;
            this.bvhData.root.quaternions[i] = Quaternion.Inverse(this.splineController.spline.GetQuaternion(i)) * this.bvhData.root.quaternions[i];
        }
    }

    private GameObject AddJoint(BVHMotion.BVHBone jointData, Transform parent)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.name = jointData.name;
        gameObject.transform.parent = parent;
        gameObject.transform.localPosition = new Vector3(jointData.offsetX, jointData.offsetY, jointData.offsetZ);
        jointData.transform = gameObject.transform;
        foreach (var childJoint in jointData.children)
        {
            GameObject child = this.AddJoint(childJoint, gameObject.transform);
        }
        if (jointData.children.Count == 0)
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            child.name = jointData.name + "_EndEffector";
            child.transform.parent = gameObject.transform;
            child.transform.localPosition = new Vector3(jointData.endSiteOffsetX, jointData.endSiteOffsetY, jointData.endSiteOffsetZ);
            jointData.endSiteTransform = child.transform;
        }
        return gameObject;
    }

    private void AddBone(BVHMotion.BVHBone jointData)
    {
        if (jointData.parent != null)
        {
            Transform parent = jointData.parent.transform;
            GameObject bone = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bone.name = $"Bone({parent.name},{jointData.transform.name})";
            bone.transform.parent = parent;
            bone.transform.up = (jointData.transform.position - parent.position).normalized;
            bone.transform.localScale = new Vector3(1, Vector3.Distance(jointData.transform.position, parent.position) * 0.5f, 1);
            bone.transform.position = Vector3.Lerp(jointData.transform.position, parent.position, 0.5f);
        }
        foreach (BVHMotion.BVHBone child in jointData.children)
        {
            this.AddBone(child);
        }
        if (jointData.children.Count == 0)
        {
            Vector3 endSitePosition = jointData.transform.position + new Vector3(jointData.endSiteOffsetX, jointData.endSiteOffsetY, jointData.endSiteOffsetZ);
            GameObject bone = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bone.name = $"Bone({jointData.transform.name},endSite)";
            bone.transform.parent = jointData.transform;
            bone.transform.up = (endSitePosition - jointData.transform.position).normalized;
            bone.transform.localScale = new Vector3(1, Vector3.Distance(jointData.transform.position, endSitePosition) * 0.5f, 1);
            bone.transform.position = Vector3.Lerp(jointData.transform.position, endSitePosition, 0.5f);
        }
    }

    private void NextFrame()
    {
        this.UpdateJointRotation(this.bvhData.root);
        this.UpdateRootPosition();
        this.UpdateRootWorldRotation();
        this.modelController.SetToFrameIndex(this.currentFrameIndex);
    }

    private void UpdateAnimationTime()
    {
        this._currentTime += Time.deltaTime * this.speed;
        this.currentFrameIndex = this._currentTime / this.bvhData.frameTime;
    }

    private void UpdateRootPosition()
    {
        Matrix4x4 translationMatrix = this.splineController.spline.GetTranslationMatrix(this.currentFrameIndex);
        this.bvhData.root.transform.localPosition = translationMatrix.MultiplyPoint(this.bvhData.root.GetLocalPosition(this.currentFrameIndex));
    }

    private void UpdateRootWorldRotation()
    {
        Quaternion rotation = this.splineController.spline.GetQuaternion(this.currentFrameIndex);
        this.bvhData.root.transform.localRotation = rotation * this.bvhData.root.transform.localRotation;
    }

    private void UpdateJointRotation(BVHMotion.BVHBone jointData)
    {
        jointData.transform.localRotation = jointData.GetLocalQuaternion(this.currentFrameIndex);
        foreach (var childJointData in jointData.children)
        {
            this.UpdateJointRotation(childJointData);
        }
    }

    private void DrawMotion()
    {
        for (int i = 0; i < this.bvhData.frames; i++)
        {
            Vector4 currentLocalPosition = this.bvhData.root.GetLocalPosition(i);
            Vector3 currentWorldPosition = this.splineController.spline.GetTranslationMatrix(i).MultiplyPoint(currentLocalPosition);
            this.motionLine.SetPosition(i, this.transform.TransformPoint(currentWorldPosition));
        }
    }

    private void ResetState()
    {
        this.bvhData = null;
        this._currentTime = 0;
        this._currentFrameIndex = 0;
        foreach (Transform child in this.transform)
        {
            MyUtils.DestroyRecursively(child);
        }
    }
}
