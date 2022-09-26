using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class RayCast : MonoBehaviour
{
    public TextMeshProUGUI coordinatesText;

    public TextMeshProUGUI BIMText;

    //Button to control the start of localization
    public Button exportButton;

    //String to hold all coordinates.
    string exportString;

    //File path of the file to export the coordinates to.
    string exportPath;

    private void Start()
    {
        exportButton.onClick.AddListener(() => ExportCoordinates());

        float rand = Random.Range(0.0f, 100.0f);

        string filename = "coord" + rand.ToString() + ".txt";

        exportPath = Path.Combine(Application.persistentDataPath, filename);

        Debug.Log(exportPath);
    }

    public void Cast(MixedRealityPointerEventData eventData)
    {

        var result = eventData.Pointer.Result;
        Vector3 pointHit = result.Details.Point;

        GameObject go = result.CurrentPointerTarget;

        string goTag = null;

        if (go != null)
            goTag = go.tag;

        if (go != null && goTag != "UI")
        {
            //Transform tf = go.transform;

            //Vector3 worldPointHit = tf.TransformPoint(pointHit);

            coordinatesText.text = string.Format("X, Y, Z [m]: {0}, {1}, {2}", pointHit.x.ToString("0.000"), pointHit.z.ToString("0.000"), pointHit.y.ToString("0.000"));

            exportString = exportString + string.Format("{0}, {1}, {2}", pointHit.x.ToString("0.000"), pointHit.z.ToString("0.000"), pointHit.y.ToString("0.000")) + "\n";

            if (go.GetComponent<BIM_element>() != null)
            {
                BIMText.text = go.GetComponent<BIM_element>().BIM_element_name;
            }
        }
        else if (goTag != "UI")
        {
            coordinatesText.text = "No hit.";

            exportString = exportString + "No hit. \n";

            BIMText.text = "No hit.";
        }

    }

    void ExportCoordinates()
    {
        StreamWriter writer = new StreamWriter(exportPath);
        writer.Write(exportString);
        writer.Close();

    }
}
