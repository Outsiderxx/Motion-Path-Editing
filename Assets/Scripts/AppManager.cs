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
    [SerializeField] private Text motionNameText;
    [SerializeField] private RuntimeGizmos.TransformGizmo gizmo;

    private RectTransform controlPanelBG;
    private BVHMotionPlayer _selectedMotion = null;

    private BVHMotionPlayer selectedMotion
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
            BVHMotionPlayer target = null;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                target = hit.transform.GetComponentInParent<BVHMotionPlayer>();
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
                    BVHParser bvhData = new BVHParser(File.ReadAllText(selectedFilePath));
                    print($"Load {fileName} successfully");

                    BVHMotionPlayer player = Instantiate(this.bvhMotionPlayerPrefab, this.playground).GetComponentInChildren<BVHMotionPlayer>();
                    player.gameObject.name = fileName;
                    player.Play(bvhData);
                    this.selectedMotion = player;
                }
                catch (System.Exception exception)
                {
                    print($"Load {fileName} failed");
                    Debug.LogException(exception);
                }
            }
        }
        else
        {
            print("User cancel action");
        }
    }

    public void AddConcateMotion()
    {
        string[] selectedFilePathes = DialogShow.ShowOpenFileDialog("Choose bvh files", Application.dataPath, "bvh files\0*.bvh\0All Files\0*.*\0\0", false, true);
        if (selectedFilePathes == null)
        {
            print("User cancel action");
        }
        else if (selectedFilePathes.Length != 2)
        {
            print("Please choose only two files");
        }
        else
        {
            string fileNameA = Path.GetFileNameWithoutExtension(selectedFilePathes[0]);
            string fileNameB = Path.GetFileNameWithoutExtension(selectedFilePathes[1]);
            try
            {
                BVHParser bvhDataA = new BVHParser(File.ReadAllText(selectedFilePathes[0]));
                BVHParser bvhDataB = new BVHParser(File.ReadAllText(selectedFilePathes[1]));
                ConcatMotion.Concat(bvhDataA, bvhDataB);
                print($"Load {fileNameA}, {fileNameB} successfully");

                BVHMotionPlayer player = Instantiate(this.bvhMotionPlayerPrefab, this.playground).GetComponentInChildren<BVHMotionPlayer>();
                player.gameObject.name = $"{fileNameA} + {fileNameB}";
                player.Play(bvhDataA);
                this.selectedMotion = player;
            }
            catch (System.Exception exception)
            {
                print($"Load  {fileNameA}, {fileNameB}  failed");
                Debug.LogException(exception);
            }
        }
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
            this.selectedMotion.splineController.useArcLength = isOn;
            print($"arcLenth mode: {isOn}");
        }
    }

    public void ChangeMotionSpeed()
    {
        if (this.selectedMotion != null)
        {
            this.selectedMotion.speed = this.speedSlider.value * 2;
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
            this.speedSlider.value = this.selectedMotion.speed / 2;
            this.useArcLengthToggle.isOn = this.selectedMotion.splineController.useArcLength;
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
