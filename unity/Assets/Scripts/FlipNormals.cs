using UnityEngine;
using System.Linq;

public class FlipNormals : MonoBehaviour
{
    void Start()
    {
        foreach (MeshRenderer mr in this.gameObject.GetComponentsInChildren<MeshRenderer>())
        {
            Mesh m = mr.GetComponent<MeshFilter>().mesh;

            m.triangles = m.triangles.Reverse().ToArray();
        }
    }
}
