using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraManager : MonoBehaviour, ITurnActor
{
    [Header("Locations")]
    [SerializeField] private List<Vector2> lLocations;
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private GameObject cam;
    [SerializeField] private TurnManager turnManager;
    private Vector3 velocity = Vector3.zero;
    private Camera mainCamera;
    private List<Camera> cameras = new List<Camera>();
    private const float camSpeed = 0.9f;
    [HideInInspector] public Vector3 pos = Vector3.zero;
    public int TickDebt { get; set; }
    void Awake() {
        if (this == null) return;
        mainCamera = GetComponent<Camera>();
        Map.layerLocations = lLocations;
        if (lLocations.Count==0||cam==null||mainCamera == null) return;
        var cameraData = mainCamera.GetUniversalAdditionalCameraData();
        if (cameraData == null) return;
        cameraData.cameraStack.Clear();
        cameras.Clear();
        transform.position = new Vector3(0,0,-10);
        //List<Transform> children = new List<Transform>();
        //foreach (Transform child in transform) children.Add(child);
        //foreach (Transform child in children) DestroyImmediate(child.gameObject);
        //for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        foreach (Vector2 v in lLocations) {
            GameObject camm = Instantiate(cam,transform);
            camm.transform.parent = transform;
            camm.transform.position = new Vector3(v.x, v.y, -10);
            Camera c = camm.GetComponent<Camera>();
            cameras.Add(c);
            cameraData.cameraStack.Add(c);
        }
        Debug.Log("[CameraManager] Camera stack updated with " + cameras.Count + " cameras.");
    }

    private void OnEnable()
    {
        if (turnManager == null) turnManager = TurnManager.Instance;
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>().GetComponent<TurnManager>();
        if (turnManager != null) turnManager.Register(this);
    }

    private void OnDisable()
    {
        if (turnManager != null) turnManager.Unregister(this);
    }
    
    public async Awaitable OnTick()
    {
        Debug.Log("[CameraManager] OnTick called.");
        if (!enabled) return;
        int l = Map.currentLayer();
        pos = Map.Player.transform.position+(l==0?new Vector3(0,0,-10) : new Vector3(lLocations[l-1].x, lLocations[l-1].y, -10));
        for (int i = 0; i < cameras.Count; i++) 
            cameras[i].enabled = i < l;
        return;
    }
    void LateUpdate()
    {
       transform.position = Vector3.SmoothDamp(transform.position, pos, ref velocity, smoothTime);
    }
}
