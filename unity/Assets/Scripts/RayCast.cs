using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using TMPro;

public class RayCast : MonoBehaviour
{
    public TextMeshProUGUI coordinatesText;

    private void Start()
    {
        coordinatesText.text = "Uspjeh!";
    }

    public void Cast(MixedRealityPointerEventData eventData)
    {

        var result = eventData.Pointer.Result;
        Vector3 pointHit = result.Details.Point;

        GameObject go = result.CurrentPointerTarget;

        if (go != null)
        {
            //Transform tf = go.transform;

            //Vector3 worldPointHit = tf.TransformPoint(pointHit);

            coordinatesText.text = string.Format("X, Y, Z [m]: {0}, {1}, {2}", pointHit.x.ToString("0.000"), pointHit.z.ToString("0.000"), pointHit.y.ToString("0.000"));
        }
        else
        {
            coordinatesText.text = "No hit.";
        }

    }
}
