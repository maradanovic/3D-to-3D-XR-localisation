using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using UnityEngine;
using UnityEngine.UI;


public class ExportObj : MonoBehaviour
{
    public Button button;

    //public GameObject objMeshToExport;

    int i;

    string folderPath;
    string path;

    // Start is called before the first frame update
    void OnEnable()
    {
        Debug.Log("ExportObj OnEnable started.");

        i = 1;

        folderPath = Application.persistentDataPath + "/MyObj/";

        Debug.Log("Export OBJ folder path:");

        Debug.Log(folderPath);

        //Create Directory if it does not exist
        if (!Directory.Exists(Path.GetDirectoryName(folderPath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(folderPath));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(folderPath));

        button.onClick.AddListener(() => exportMesh());
        
        Debug.Log("Export Obj OnEnable ended.");
    }

    private void exportMesh()
    {
        Debug.Log("Export obj command started.");

        var observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        //objMeshToExport = GameObject.Find("Spatial Object Mesh Observer");

        //Debug.Log(objMeshToExport.name.ToString());

        foreach (SpatialAwarenessMeshObject meshObject in observer.Meshes.Values)
        {
            MeshFilter mf = meshObject.Filter;

            path = Path.Combine(folderPath, "mesh_" + i + ".obj");

            ObjExporter.MeshToFile(mf, path);

            i++;
        }

        Debug.Log("Export obj command ended.");
    }


}