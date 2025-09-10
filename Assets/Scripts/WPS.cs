using Niantic.Experimental.Lightship.AR.WorldPositioning;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class WPS : MonoBehaviour { 
    
    public static WPS Instance { get; private set; }

    [SerializeField] private LatLong targetLatlong;

    [Space]
    [SerializeField] private ARCameraManager cameraMaganer;
    [SerializeField] private ARWorldPositioningManager positioningManager;
    [SerializeField] private ARWorldPositioningObjectHelper objectHelper;
    [SerializeField] private GameObject cameraHelperGameObject;

    private ARWorldPositioningCameraHelper cameraHelper;

    private void Awake() {
        Instance = this;
    }

    private void Start()
    {
        cameraHelper = cameraHelperGameObject.GetComponent<ARWorldPositioningCameraHelper>();
    }

    private void Update() {
        if (SceneManager.GetActiveScene().name.ToString() == "Main Menu") {
            if (cameraHelper.Latitude == targetLatlong.latitude && cameraHelper.Longitude == targetLatlong.longitude) {
                UIManager.Instance.PlayGame();
            }
        }

        Debug.Log("Lat : " + cameraHelper.Latitude);
        Debug.Log("Long : " + cameraHelper.Longitude);
    }

    public GameObject SpawnObjectAtPositionFromCamera(GameObject objectPrefab, Vector3 position, double altitude = 0.0) {
        Transform camTransform = cameraMaganer.transform;
        (double latOffsetCam, double longOffsetCam) = GetGeographicOffsetfromCameraPosition(camTransform.position);
        (double latOffsetObj, double longOffsetObj) = GetGeographicOffsetfromCameraPosition(position);

        double latitude = positioningManager.WorldTransform.OriginLatitude + latOffsetCam + latOffsetObj;
        double longitude = positioningManager.WorldTransform.OriginLatitude + longOffsetCam + longOffsetObj;

        GameObject spawnedObject = Instantiate(objectPrefab);

        objectHelper.AddOrUpdateObject(spawnedObject, latitude, longitude, altitude, Quaternion.identity);

        return spawnedObject;
    }

    public (double, double) GetGeographicOffsetfromCameraPosition(Vector3 position) { 
        double latOffset = position.z / 111000;
        double longOffset = position.x / (111000 * Mathf.Cos((float)positioningManager.WorldTransform.OriginLatitude * Mathf.Deg2Rad));

        return (latOffset, longOffset);
    }


}

[Serializable]
public struct LatLong {
    public double latitude;
    public double longitude;
}