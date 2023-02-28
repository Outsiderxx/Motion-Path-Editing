using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class AppManager : MonoBehaviour
{
    [SerializeField] private GameObject bvhMotionPlayerPrefab = null;
    [SerializeField] private Transform playground;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private Toggle useArcLengthToggle;
    [SerializeField] private Toggle displayModelToggle;
    [SerializeField] private GameObject controlPanel;
    [SerializeField] private Slider desiredKnotPointCountSlider;
    [SerializeField] private Text motionNameText;
    [SerializeField] private Text speedText;
    [SerializeField] private Text knotPointCountText;
    [SerializeField] private Text message;
    [SerializeField] private RuntimeGizmos.TransformGizmo gizmo;

    private RectTransform controlPanelBG;
    private MotionPlayer _selectedMotion = null;

    private MotionPlayer selectedMotion
    {
        get
        {
            return this._selectedMotion;
        }
        set
        {
            if (this._selectedMotion == value)
            {
                return;
            }
            this._selectedMotion = value;
            this.OnSeletedStateChanged();
        }
    }

    private void Awake()
    {
        this.controlPanelBG = this.controlPanel.transform.Find("Background").GetComponent<RectTransform>();
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(this.controlPanelBG, Input.mousePosition))
            {
                return;
            }
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            MotionPlayer target = null;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                target = hit.transform.GetComponentInParent<MotionPlayer>();
                if (target != null)
                {
                    break;
                }
            }
            this.selectedMotion = target;
        }

        if (Input.GetKeyUp(KeyCode.Delete))
        {
            this.DeleteSelectedMotion();
        }
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            this.controlPanel.SetActive(!this.controlPanel.activeSelf);
        }
    }

    public void AddMotions()
    {
        string[] selectedFilePathes = DialogShow.ShowOpenFileDialog("Choose bvh files", Application.dataPath, "bvh files\0*.bvh\0All Files\0*.*\0\0", false, true);
        if (selectedFilePathes != null)
        {
            foreach (string selectedFilePath in selectedFilePathes)
            {
                string fileName = Path.GetFileNameWithoutExtension(selectedFilePath);
                try
                {
                    BVHMotion bvhData = new BVHMotion(File.ReadAllText(selectedFilePath));
                    print($"Load {fileName} successfully");

                    MotionPlayer player = Instantiate(this.bvhMotionPlayerPrefab, this.playground).GetComponentInChildren<MotionPlayer>();
                    player.gameObject.name = fileName;
                    player.desiredKnotPointCount = (int)this.desiredKnotPointCountSlider.value;
                    player.Play(bvhData);
                    this.selectedMotion = player;
                    this.message.text = "";
                }
                catch (System.Exception exception)
                {
                    print($"Load {fileName} failed");
                    this.message.text = exception.Message;
                    Debug.LogException(exception);
                }
            }
        }
        else
        {
            print("User cancel action");
            this.message.text = "";
        }
    }

    public void AddConcateMotion()
    {
        string[] selectedFilePathes = DialogShow.ShowOpenFileDialog("Choose bvh files", Application.dataPath, "bvh files\0*.bvh\0All Files\0*.*\0\0", false, true);
        if (selectedFilePathes == null)
        {
            print("User cancel action");
            this.message.text = "";
        }
        else if (selectedFilePathes.Length != 2)
        {
            print("Please choose two files");
            this.message.text = "Please choose two files";
        }
        else
        {
            string fileNameA = Path.GetFileNameWithoutExtension(selectedFilePathes[0]);
            string fileNameB = Path.GetFileNameWithoutExtension(selectedFilePathes[1]);
            try
            {
                BVHMotion bvhDataA = new BVHMotion(File.ReadAllText(selectedFilePathes[0]));
                BVHMotion bvhDataB = new BVHMotion(File.ReadAllText(selectedFilePathes[1]));
                ConcatMotion.Concat(bvhDataA, bvhDataB);
                print($"Concat {fileNameA}, {fileNameB} successfully");

                MotionPlayer player = Instantiate(this.bvhMotionPlayerPrefab, this.playground).GetComponentInChildren<MotionPlayer>();
                player.gameObject.name = $"{fileNameA} + {fileNameB}";
                player.desiredKnotPointCount = (int)this.desiredKnotPointCountSlider.value;
                player.Play(bvhDataA);
                this.selectedMotion = player;
                this.message.text = "";
            }
            catch (System.Exception exception)
            {
                print($"Concat  {fileNameA}, {fileNameB}  failed");
                this.message.text = exception.Message;
                Debug.LogException(exception);
            }
        }
    }

    public void AddBlendMotion()
    {
        string[] selectedFilePathes = DialogShow.ShowOpenFileDialog("Choose bvh files", Application.dataPath, "bvh files\0*.bvh\0All Files\0*.*\0\0", false, true);
        if (selectedFilePathes == null)
        {
            print("User cancel action");
            this.message.text = "";
        }
        else if (selectedFilePathes.Length != 2)
        {
            print("Please choose two files");
            this.message.text = "Please choose two files";
        }
        else
        {
            string fileNameA = Path.GetFileNameWithoutExtension(selectedFilePathes[0]);
            string fileNameB = Path.GetFileNameWithoutExtension(selectedFilePathes[1]);
            try
            {
                BVHMotion bvhDataA = new BVHMotion(File.ReadAllText(selectedFilePathes[0]));
                BVHMotion bvhDataB = new BVHMotion(File.ReadAllText(selectedFilePathes[1]));
                float[] weights = new float[bvhDataA.frames];
                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] = 0.5f;
                }
                BVHMotion blendMotion = BlendMotion.Blend(bvhDataA, bvhDataB, weights);
                MotionPlayer player = Instantiate(this.bvhMotionPlayerPrefab, this.playground).GetComponentInChildren<MotionPlayer>();
                player.gameObject.name = $"{fileNameA} + {fileNameB}";
                player.desiredKnotPointCount = (int)this.desiredKnotPointCountSlider.value;
                player.Play(blendMotion);
                this.selectedMotion = player;
                this.message.text = "";
                print($"Blend {fileNameA}, {fileNameB} successfully");
            }
            catch (System.Exception exception)
            {
                print($"Blend  {fileNameA}, {fileNameB}  failed");
                this.message.text = exception.Message;
                Debug.LogException(exception);
            }
        }
    }

    public void OnDesiredKnotPointCountChanged(float count)
    {
        this.knotPointCountText.text = $"{((int)(count))}";
    }

    public void DeleteSelectedMotion()
    {
        if (this.selectedMotion != null)
        {
            print($"delete {this.selectedMotion.gameObject.name}");
            MyUtils.DestroyRecursively(this.selectedMotion.transform.parent);
            this.selectedMotion = null;
        }
    }

    public void ToggleDisplayMode(bool isOn)
    {
        if (this.selectedMotion != null)
        {
            this.selectedMotion.displayModel = isOn;
            print($"display model: {isOn}");
        }
    }

    public void ToggleArcLength(bool isOn)
    {
        if (this.selectedMotion != null)
        {
            this.selectedMotion.splineController.spline.useArcLength = isOn;
            print($"arcLenth mode: {isOn}");
        }
    }

    public void ChangeMotionSpeed(float speed)
    {
        if (this.selectedMotion != null)
        {
            this.selectedMotion.speed = speed;
            this.speedText.text = $"{speed.ToString("F2")}";
        }
    }

    public void DeleteAllMotion()
    {
        foreach (Transform motion in this.playground)
        {
            MyUtils.DestroyRecursively(motion);
        }
    }

    private void OnSeletedStateChanged()
    {
        if (this.selectedMotion != null)
        {
            print($"select {this.selectedMotion.gameObject.name}");
            this.speedSlider.value = this.selectedMotion.speed;
            this.useArcLengthToggle.isOn = this.selectedMotion.splineController.spline.useArcLength;
            this.displayModelToggle.isOn = this.selectedMotion.displayModel;
            this.motionNameText.text = this.selectedMotion.gameObject.name;
            gizmo.AddTarget(this.selectedMotion.transform.parent, true);
        }
        else
        {
            print("unselected");
        }
        this.controlPanel.SetActive(this.selectedMotion != null);
    }
}
