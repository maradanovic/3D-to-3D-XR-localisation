using UnityEngine;

public class AddColliders : MonoBehaviour
{
    void Start()
    {
        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
        {
            mf.gameObject.AddComponent<MeshCollider>();
        }
    }
}
