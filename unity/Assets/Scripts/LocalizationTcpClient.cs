using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

//For accessing the MRTK mesh observer.
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;

//For UI access.
using UnityEngine.UI;

//For the conversion of transformation matrices to quaternions
using Assets.Scripts;

//For the output status text.
using TMPro;

//For the stopwatch and time logging.
using System.Diagnostics;

#if !UNITY_EDITOR
using System.Threading.Tasks;
#endif

//TCP client modified from https://foxypanda.me/tcp-client-in-a-uwp-unity-app-on-hololens/

public class LocalizationTcpClient : MonoBehaviour
{
#if !UNITY_EDITOR
    private bool _useUWP = true;
    private Windows.Networking.Sockets.StreamSocket socket;
    private Task exchangeTask;
#endif

#if UNITY_EDITOR
    private bool _useUWP = false;
    System.Net.Sockets.TcpClient client;
    System.Net.Sockets.NetworkStream stream;
    private Thread exchangeThread;
#endif

    private Byte[] bytes = new Byte[256];
    private StreamWriter writer;
    private StreamReader reader;

    //Strings to hold vertices, triangle (faces) and their numbers. To be sent to the server.
    private string verticesString;
    private string trianglesString;
    private string verticesNumString;
    private string trianglesNumString;
    private string estimatedPositionString;

    private StringBuilder sbVertices = new StringBuilder();
    private StringBuilder sbTriangles = new StringBuilder();

    private List<Vector3> listVertices = new List<Vector3>();
    private List<int> listTriangles = new List<int>();

    int totalNumberOfMeshParts = 0;

    //Vector to hold the resulting transformation matrix. To be received from the server.
    private Matrix4x4 transformationMatrix;

    //IP address and port of the server.
    public string ipAddress = "10.233.60.193";
    public string port = "8013";

    //Flag that is true if the mesh has been sent, one that is true if the transformation need to be applied,
    //and one to indicate relocalisation is requested instead of initial (global) localisation.
    private bool meshSent = false;
    private bool applyTransform = false;
    private bool relocalisation = false;

    //Button to start the localization
    public Button startLocalizationButton;

    //Button to start the relocalization
    public Button startRelocalizationButton;

    public GameObject statusTextGO;
    private TextMeshProUGUI statusText;
    private String newStatusText;
    private bool updateStatus = false;

    //Game object World that contains all the models and game objects to be aligned with the reality (with the HL mesh),
    //and a game object that serves as a boundary for the cutting of the pieces of the spatial mesh observer mesh.
    GameObject world;
    GameObject boundary;

    //The size of the boundary box in metres, by default 3m, for initial localisation and for the relocalisation.
    public float boundaryBoxSize = 5;
    public float relocBoundaryBoxSize = 3;
    

    public GameObject cameraGO;

    private Stopwatch timer;

    //remove this
    //private int k = 0;
    private IMixedRealitySpatialAwarenessMeshObserver observer;

    private void Start()
    {
        world = GameObject.FindGameObjectWithTag("World");

        statusText = statusTextGO.GetComponent<TextMeshProUGUI>();

        startLocalizationButton.onClick.AddListener(() => StartLocalization());

        startRelocalizationButton.onClick.AddListener(() => StartRelocalisation());

        observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();
    }

    public void Update()
    {
        //If the applyTransform flag is raised, apply the transformation and lower the flag.
        //Setting the transforma of a game object can only be done from the main thread.
        if (applyTransform)
        {
            meshSent = false;

            ApplyTransformation(transformationMatrix);

            applyTransform = false;

            newStatusText = "Registration successful! Localization finished.";
            updateStatus = true;

            UnityEngine.Debug.Log("Transformation applied");

            transformationMatrix = new Matrix4x4();
        }

        //If the updateStatus flag is raised, apply the status change and lower the flag.
        if (updateStatus)
        {
            statusText.text = newStatusText;
            updateStatus = false;
        }

    }

    public void Connect(string host, string port)
    {
        UnityEngine.Debug.Log("Connect started");

        if (_useUWP)
        {
            ConnectUWP(host, port);
        }
        else
        {
            ConnectUnity(host, port);
        }
    }

#if UNITY_EDITOR
    private void ConnectUWP(string host, string port)
#else
    private async void ConnectUWP(string host, string port)
#endif
    {
#if UNITY_EDITOR
        errorStatus = "UWP TCP client used in Unity!";
#else
        try
        {
            if (exchangeTask != null) StopExchange();
        
            socket = new Windows.Networking.Sockets.StreamSocket();
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(host);
            await socket.ConnectAsync(serverHost, port);
        
            Stream streamOut = socket.OutputStream.AsStreamForWrite();
            writer = new StreamWriter(streamOut) { AutoFlush = true };
        
            Stream streamIn = socket.InputStream.AsStreamForRead();
            reader = new StreamReader(streamIn);

            RestartExchange();
            //successStatus = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private void ConnectUnity(string host, string port)
    {
#if !UNITY_EDITOR
        errorStatus = "Unity TCP client used in UWP!";
#else
        UnityEngine.Debug.Log("UnityEngine connect started");


        try
        {
            if (exchangeThread != null) StopExchange();

            client = new System.Net.Sockets.TcpClient(host, Int32.Parse(port));
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = true };

            RestartExchange();
            //successStatus = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private bool exchangeStopRequested = false;

    private string errorStatus = null;
    //private string successStatus = null;

    public void RestartExchange()
    {
#if UNITY_EDITOR
        if (exchangeThread != null) StopExchange();
        exchangeStopRequested = false;
        exchangeThread = new System.Threading.Thread(ExchangePackets);
        exchangeThread.Start();
#else
        if (exchangeTask != null) StopExchange();
        exchangeStopRequested = false;
        exchangeTask = Task.Run(() => ExchangePackets());
#endif
    }

    public void ExchangePackets()
    {
        while (!exchangeStopRequested)
        {
            string received = null;

            if (!meshSent)
            {
                //Send the vertices, triangles, and their numbers to the server.
                writer.Write(verticesString);
                writer.Write(trianglesString);
                writer.Write(verticesNumString + "\n");
                writer.Write(trianglesNumString);

                //Notify the server that the mesh is sent, log this and lower the flag.
                if (!relocalisation)
                    writer.Write("Mesh sent!");
                else
                {
                    writer.Write("\n" + estimatedPositionString);
                    writer.Write("Reloc sent!");
                }

                UnityEngine.Debug.Log("Mesh sent!");
                
                newStatusText = "Mesh sent to server. Registering...";
                updateStatus = true;

                //Clear all the variables related to mesh reading and sending.
                verticesString = null;
                trianglesString = null;
                verticesNumString = null;
                trianglesNumString = null;
                writer.Flush();

                meshSent = true;
            }


#if UNITY_EDITOR
            byte[] bytes = new byte[client.SendBufferSize];
            int recv = 0;
            while (true)
            {
                recv = stream.Read(bytes, 0, client.SendBufferSize);
                received += Encoding.UTF8.GetString(bytes, 0, recv);
                if (received.EndsWith("\n")) break;
            }

            if (received.EndsWith("Transformation matrix sent!\n"))
            {
                UnityEngine.Debug.Log("Transformation matrix received!");

                received = received.Remove(received.Length - 28, 28);

                transformationMatrix = Localization.DeserializeVector4Array(received);

                UnityEngine.Debug.Log(transformationMatrix);

                applyTransform = true;

                exchangeStopRequested = true;

                received = null;

                StopExchange();
            }
            else if (received.EndsWith("Reloc matrix sent!\n"))
            {
                UnityEngine.Debug.Log("Reloc matrix received!");

                received = received.Remove(received.Length - 18, 18);

                transformationMatrix = Localization.DeserializeVector4Array(received);

                UnityEngine.Debug.Log(transformationMatrix);

                applyTransform = true;

                exchangeStopRequested = true;

                received = null;

                StopExchange();
            }
            else if (received.EndsWith("Unsuccessful!\n"))
            {
                UnityEngine.Debug.Log("Registration unsuccessful.");

                //newStatusText = "Registration unsuccessful.";
                //updateStatus = true;

                exchangeStopRequested = true;

                received = null;

                StopExchange();
            }
            else
                UnityEngine.Debug.Log("Read message from server: " + received);
#else

            received = reader.ReadLine();

            if (received.EndsWith("Transformation matrix sent!"))
            {
                received = received.Remove(received.Length - 27, 27);

                transformationMatrix = Localization.DeserializeVector4Array(received);

                UnityEngine.Debug.Log(transformationMatrix);

                applyTransform = true;

                exchangeStopRequested = true;

                received = null;

                StopExchange();
            }
            else if (received.EndsWith("Reloc matrix sent!"))
            {
                received = received.Remove(received.Length - 17, 17);

                transformationMatrix = Localization.DeserializeVector4Array(received);

                UnityEngine.Debug.Log(transformationMatrix);

                applyTransform = true;

                exchangeStopRequested = true;

                received = null;

                StopExchange();
            }
            else if (received.EndsWith("Unsuccessful!"))
            {
                UnityEngine.Debug.Log("Registration unsuccessful.");

                //newStatusText = "Registration unsuccessful.";
                //updateStatus = true;

                exchangeStopRequested = true;

                received = null;

                StopExchange();
            }
            else
                UnityEngine.Debug.Log("Read message from server: " + received);
#endif

        }
    }

    public void StopExchange()
    {
        exchangeStopRequested = true;

#if UNITY_EDITOR
        if (exchangeThread != null)
        {
            UnityEngine.Debug.Log("StopExchange started Unity.");

            //newStatusText = "StopExchange started Unity.";
            //updateStatus = true;

            //exchangeThread.Abort();

            //UnityEngine.Debug.Log("Thread Aborted Unity.");

            //newStatusText = "Thread Aborted Unity.";
            //updateStatus = true;

            stream.Close();
            client.Close();
            writer.Close();
            reader.Close();

            stream = null;
            exchangeThread = null;

            UnityEngine.Debug.Log("StopExchange ended Unity.");

            //newStatusText = "StopExchange ended Unity.";
            //updateStatus = true;
        }
#else
        if (exchangeTask != null) {
            UnityEngine.Debug.Log("StopExchange started UWP.");

            //newStatusText = "StopExchange started UWP.";
            //updateStatus = true;

            //exchangeTask.Wait();

            socket.Dispose();
            writer.Dispose();
            reader.Dispose();
  
            //UnityEngine.Debug.Log("Test UWP");

            //newStatusText = "Stuffe disposed off UWP.";
            //updateStatus = true;

            socket = null;
            exchangeTask = null;

            UnityEngine.Debug.Log("StopExchange ended UWP.");

            //newStatusText = "StopExchange ended UWP.";
            //updateStatus = true;
        }
#endif
        writer = null;
        reader = null;
    }

    public void OnDestroy()
    {
        StopExchange();
    }

    public void RetrieveAndSerializeMesh()
    {
        UnityEngine.Debug.Log("RetrieveAndSerialize started.");

        timer = new Stopwatch();
        timer.Start();

        int verticesNum = 0;
        int trianglesNum = 0;

        totalNumberOfMeshParts = 0;

        sbTriangles.Clear();
        sbVertices.Clear();

        estimatedPositionString = Localization.SerializeVector3Position(cameraGO.transform.position);

        Bounds boundaryBounds = boundary.GetComponent<MeshRenderer>().bounds;

        foreach (SpatialAwarenessMeshObject meshObject in observer.Meshes.Values)
        {
            if (boundaryBounds.Intersects(meshObject.Renderer.bounds))
            {
                totalNumberOfMeshParts++;

                Mesh mesh = meshObject.Filter.mesh;

                Transform tf = meshObject.Filter.GetComponent<Transform>();

                listVertices.Clear();
                listTriangles.Clear();

                mesh.GetVertices(listVertices);
                mesh.GetTriangles(listTriangles, 0);

                Localization.SerializeVector3Array(sbVertices, listVertices, tf);
                Localization.SerializeIntArray(sbTriangles, listTriangles, verticesNum);

                verticesNum += listVertices.Count;
                trianglesNum += (listTriangles.Count / 3);
            }
        }

        verticesNumString = verticesNum.ToString();
        trianglesNumString = trianglesNum.ToString();

        timer.Stop();
        UnityEngine.Debug.Log("Time elapsed for serialization: " + timer.Elapsed);

        UnityEngine.Debug.Log("Total number of mesh parts serialized: " + totalNumberOfMeshParts);
    }

    //Retrieves the meshes of all children of this GameObject. Serializes their vertices and triangles (faces) in strings to be sent to the server.

    private void StartLocalization()
    {
        CreateBoundary(boundaryBoxSize);

        Stopwatch slTimer = new Stopwatch();
        slTimer.Start();

        exchangeStopRequested = false;

        newStatusText = "Mesh serialisation and localisation started.";
        updateStatus = true;

        RetrieveAndSerializeMesh();

        verticesString = sbVertices.ToString();
        trianglesString = sbTriangles.ToString();

        slTimer.Stop();
        UnityEngine.Debug.Log("slTimer elapsed: " + slTimer.Elapsed);

        UnityEngine.Debug.Log("Mesh serialized");

        newStatusText = "Mesh serialized in " + slTimer.Elapsed + " s for " + totalNumberOfMeshParts + " parts. Sending mesh...";
        updateStatus = true;

        Connect(ipAddress, port);
    }

    private void StartRelocalisation()
    {
        CreateBoundary(relocBoundaryBoxSize);

        newStatusText = "Mesh serialisation and relocalisation started.";
        updateStatus = true;

        UnityEngine.Debug.Log("Mesh relocalisation started.");

        relocalisation = true;
        exchangeStopRequested = false;
        meshSent = false;

        RetrieveAndSerializeMesh();

        verticesString = sbVertices.ToString();
        trianglesString = sbTriangles.ToString();

        UnityEngine.Debug.Log("Mesh serialized");

        Connect(ipAddress, port);
    }

    private void ApplyTransformation(Matrix4x4 tMatrix)
    {
        Quaternion quat = new Quaternion();
        quat = Localization.GetRotation(tMatrix);

        Vector3 position = new Vector3(tMatrix.GetColumn(0)[3], tMatrix.GetColumn(1)[3], tMatrix.GetColumn(2)[3]);

        world.transform.position = position;

        world.transform.rotation = Quaternion.Inverse(quat);
    }

    private void CreateBoundary(float size)
    {
        Destroy(boundary);
        boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);

        boundary.transform.position = cameraGO.transform.position;

        boundary.GetComponent<MeshRenderer>().enabled = false;
        boundary.transform.localScale = new Vector3(size, size, size);
    }
}