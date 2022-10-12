using UnityEngine;

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
}
