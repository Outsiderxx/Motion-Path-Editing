using UnityEngine;
using System.IO;

public class AppManager : MonoBehaviour
{
    [SerializeField] private GameObject bvhMotionPlayerPrefab = null;
    [SerializeField] private Transform playground;

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

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
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

    public void DeleteSelectedMotion()
    {
        if (this.selectedMotion != null)
        {
            print($"delete {this.selectedMotion.gameObject.name}");
            MyUtils.DestroyRecursively(this.selectedMotion.transform.parent);
            this.selectedMotion = null;
        }
    }

    private void OnSeletedStateChanged()
    {
        if (this.selectedMotion != null)
        {
            print($"select {this.selectedMotion.gameObject.name}");
        }
        else
        {
            print("unselected");
        }
    }
}
