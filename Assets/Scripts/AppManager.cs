using UnityEngine;
using System.IO;

public class AppManager : MonoBehaviour
{
    [SerializeField] private GameObject bvhMotionPlayerPrefab = null;
    [SerializeField] private Transform playground;

    private void Start()
    {
        Application.targetFrameRate = 60;
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
}
