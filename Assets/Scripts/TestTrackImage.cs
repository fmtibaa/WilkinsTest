using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class TestTrackImage : MonoBehaviour
{
    // object to display when we track all the images in the scene
    public GameObject objPrefab;

    // Cache ARRaycastManager GameObject from XROrigin
    private ARRaycastManager _raycastManager;

    // Cache AR tracked images manager from ARCoreSession
    private ARTrackedImageManager _trackedImagesManager;

    // List for raycast hits is re-used by raycast manager
    private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

    // number of refrenced images in the renfrenced image library
    private int imagelibraryCount;

    // start position
    private Vector3 startPosition;

    // tracked image chosen
    private GameObject TrackedImagechosen;

    // list of tracked image prefab
    private List<GameObject> trackedImagePrefabs = new List<GameObject>();

    //list of ordered position witch corresponds to the minimum of distance when parcouring images
    private List<Vector3> orderedPositionlist = new List<Vector3>();

    //list of tracked image positions
    private List<Vector3> positionList = new List<Vector3>();

    // the line renderer to show shortest route
    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _raycastManager = GetComponent<ARRaycastManager>();
        _trackedImagesManager = GetComponent<ARTrackedImageManager>();
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.enabled = false;
        imagelibraryCount = _trackedImagesManager.referenceLibrary.count;
        startPosition = Vector3.zero;
        TrackedImagechosen = null;
    }

    void OnEnable()
    {
        _trackedImagesManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        _trackedImagesManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        GameObject obj = new GameObject();

        foreach (var trackedImage in eventArgs.added)
        {
            trackedImagePrefabs.Add(trackedImage.gameObject);
            if (trackedImagePrefabs.Count == imagelibraryCount)
            {
                // to see if we tracked all images or not
                foreach(GameObject go in trackedImagePrefabs)
                {
                   obj = Instantiate(objPrefab, go.transform);
                   obj.SetActive(true);
                }
            }
        }
    }

    private void Update()
    {
        positionList.Clear();

        Touch touch = Input.GetTouch(0);

        //if ((touch = Input.GetTouch(0)).phase != TouchPhase.Began) { return; }
        //ShowAndroidToastMessage("Dans Update");
        // Perform AR raycast to Image trackable when we track all images
        if (trackedImagePrefabs.Count == imagelibraryCount)
        {
            if (_raycastManager.Raycast(touch.position, Hits, TrackableType.Image))
            {
                //ShowAndroidToastMessage("Ecran Touche");
                orderedPositionlist.Clear();
                Vector3 hitPosition = Hits[0].pose.position;

                float distance = (hitPosition - trackedImagePrefabs[0].transform.position).magnitude;

                startPosition = trackedImagePrefabs[0].transform.position;
                TrackedImagechosen = trackedImagePrefabs[0];

                // searching for the position of the trackble image we've selected
                for (int i = 1; i < trackedImagePrefabs.Count; i++)
                {
                    if ((hitPosition - trackedImagePrefabs[i].transform.position).magnitude <= distance)
                    {
                        distance = (hitPosition - trackedImagePrefabs[i].transform.position).magnitude;
                        startPosition = trackedImagePrefabs[i].transform.position;
                        TrackedImagechosen = trackedImagePrefabs[i];
                    }
                }
            }

            if (startPosition != Vector3.zero)
            {
                orderedPositionlist.Add(startPosition);

                foreach (GameObject go in trackedImagePrefabs)
                {
                    if (go.transform.position != startPosition)
                    {
                        positionList.Add(go.transform.position);
                    }
                }

                SearchForMinimumDistance(startPosition, positionList);

                ShowShortestRoute(orderedPositionlist);
            }
        }
    }

    private void SearchForMinimumDistance(Vector3 initialPosition, List<Vector3> initialpositionList)
    {
        if (initialpositionList.Count == 1)
        {
            orderedPositionlist.Add(initialpositionList[0]);
        }
        else
        {
            int index = 0;
            float distance = (initialPosition - initialpositionList[0]).magnitude;
            // searching for the point that has the minimal distance with the start position in the list of positions
            for (int i = 0; i< initialpositionList.Count; i++)
            {
                if ((initialPosition - initialpositionList[i]).magnitude < distance)
                {
                    distance = (initialPosition - initialpositionList[i]).magnitude;
                    index = i;
                }
            }
            orderedPositionlist.Add(initialpositionList[index]);
            initialpositionList.RemoveAt(index);
            SearchForMinimumDistance(initialpositionList[index], initialpositionList);
        }
    }

    private void ShowShortestRoute(List<Vector3> listofOrderedPosition)
    {
        // drawing line that link all the points in the ordered list of positions
        _lineRenderer.enabled = true;
        _lineRenderer.startWidth = 0.025f;
        _lineRenderer.endWidth = 0.0025f;
        _lineRenderer.startColor = Color.red;
        _lineRenderer.endColor = Color.red;
        _lineRenderer.positionCount = listofOrderedPosition.Count;
        _lineRenderer.SetPositions(listofOrderedPosition.ToArray());
    }

    /// <summary>
    /// Show an Android toast message.
    /// </summary>
    /// <param name="message">Message string to show in the toast.</param>
    private static void ShowAndroidToastMessage(string message)
    {
#if UNITY_ANDROID
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        if (unityActivity == null) return;
        var toastClass = new AndroidJavaClass("android.widget.Toast");
        unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            // Last parameter = length. Toast.LENGTH_LONG = 1
            using var toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText",
                unityActivity, message, 1);
            toastObject.Call("show");
        }));
#endif
    }
}