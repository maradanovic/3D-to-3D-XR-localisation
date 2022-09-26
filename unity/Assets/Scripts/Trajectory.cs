//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.Windows.WebCam;

//public class Trajectory : MonoBehaviour
//{
//    PhotoCapture photoCaptureObject = null;

//    private Stopwatch timer;

//    System.Linq.IOrderedEnumerable<Resolution> cameraResolutions;

//    // Start is called before the first frame update
//    void Start()
//    {
//        cameraResolutions = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height);

//        timer = new Stopwatch();

//        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
//    }

//#if !UNITY_EDITOR
//    private void Update()
//    {

//    }
//#endif

//    void OnPhotoCaptureCreated(PhotoCapture captureObject)
//    {
//        photoCaptureObject = captureObject;

//        Resolution cameraResolution = cameraResolutions.ElementAt(1);

//        CameraParameters c = new CameraParameters();
//        c.hologramOpacity = 0.0f;
//        c.cameraResolutionWidth = cameraResolution.width;
//        c.cameraResolutionHeight = cameraResolution.height;
//        c.pixelFormat = CapturePixelFormat.BGRA32;

//        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
//    }

//    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
//    {
//        if (result.success)
//        {
//            string filename = string.Format(@"CapturedImage{0}_n.png", Time.time);
//            timer.Start();

//#if !UNITY_EDITOR
//            string filePath = System.IO.Path.Combine(Windows.Storage.KnownFolders.PicturesLibrary.Path, filename);
//#else
//            string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
//#endif



//            //photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.PNG, OnCapturedPhotoToDisk);

//            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);

//            UnityEngine.Debug.Log(filePath);
//        }
//        else
//        {
//            UnityEngine.Debug.LogError("Unable to start photo mode!");
//        }
//    }

//    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
//    {
//        UnityEngine.Debug.Log("OnCapturedPhotoToMemory");

//        if (result.success)
//        {
//            UnityEngine.Debug.Log("OnCapturedPhotoToMemory result_success");

//            //// Create our Texture2D for use and set the correct resolution
//            //Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
//            //Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
//            //// Copy the raw image data into our target texture
//            //photoCaptureFrame.UploadImageDataToTexture(targetTexture);
//            //// Do as we wish with the texture such as apply it to a material, etc.

//            if (photoCaptureFrame.hasLocationData)
//            {

//                UnityEngine.Debug.Log("Has location data");

//            }


//            photoCaptureFrame.TryGetCameraToWorldMatrix(out Matrix4x4 cameraToWorldMatrix);

//            Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
//            Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

//            UnityEngine.Debug.Log(cameraToWorldMatrix);
//            UnityEngine.Debug.Log(position);
//            UnityEngine.Debug.Log(rotation);

//            //photoCaptureFrame.TryGetProjectionMatrix(Camera.main.nearClipPlane, Camera.main.farClipPlane, out Matrix4x4 projectionMatrix);
//        }
//        // Clean up
//        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
//    }

//    void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
//    {
//        if (result.success)
//        {
//            UnityEngine.Debug.Log("Saved Photo to disk!");

//            timer.Stop();

//            UnityEngine.Debug.Log("Time elapsed to export photo: " + timer.Elapsed);

//            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
//        }
//        else
//        {
//            UnityEngine.Debug.Log("Failed to save Photo to disk");
//        }
//    }

//    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
//    {
//        photoCaptureObject.Dispose();
//        photoCaptureObject = null;
//    }
//}


using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Windows.WebCam;

public class Trajectory : MonoBehaviour
{
    PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;

    // Use this for initialization
    void Start()
    {
        UnityEngine.Debug.Log("Start");

        StartCoroutine(TakePhotoCoroutine());

        UnityEngine.Debug.Log("Start ended");
    }

    IEnumerator TakePhotoCoroutine()
    {
        UnityEngine.Debug.Log("Coroutine start");

        yield return new WaitForSeconds(2);

        UnityEngine.Debug.Log("Coroutine after 10 seconds");

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

        // Create a PhotoCapture object
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject) {
            photoCaptureObject = captureObject;
            CameraParameters cameraParameters = new CameraParameters();
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.cameraResolutionWidth = cameraResolution.width;
            cameraParameters.cameraResolutionHeight = cameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;


            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result) {
                // Take a picture
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });
    }    

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        // Copy the raw image data into our target texture
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);

        // Create a gameobject that we can apply our texture to
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;
        quadRenderer.material = new Material(Shader.Find("Unlit/Texture"));

        quad.transform.parent = this.transform;
        quad.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);

        quadRenderer.material.SetTexture("_MainTex", targetTexture);

        Matrix4x4 m1 = new Matrix4x4();
        Matrix4x4 m2 = new Matrix4x4();

        Debug.Log(photoCaptureFrame.TryGetCameraToWorldMatrix(out m1)); // Display False
        Debug.Log(photoCaptureFrame.TryGetProjectionMatrix(out m2)); // Display False

        if (photoCaptureFrame.hasLocationData)
        {
            UnityEngine.Debug.Log("Has location data");

            photoCaptureFrame.TryGetCameraToWorldMatrix(out Matrix4x4 cameraToWorldMatrix);

            Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
            Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

            UnityEngine.Debug.Log(cameraToWorldMatrix);
            UnityEngine.Debug.Log(position);
            UnityEngine.Debug.Log(rotation);

        }

        // Deactivate our camera
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown our photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
}