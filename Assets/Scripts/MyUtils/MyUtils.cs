using UnityEngine;

public static class MyUtils
{
    public static void DestroyAllChild(Transform root)
    {
        foreach (Transform child in root.transform)
        {
            DestroyAllChild(child);
            GameObject.Destroy(child.gameObject);
        }
    }
}
