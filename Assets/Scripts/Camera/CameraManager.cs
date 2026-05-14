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
    [Header("Property")]
    [SerializeField] private Material addingMaterial;
    [SerializeField] private List<RenderTexture> vRT;
    [SerializeField] private GameObject shadowCam;

    [SerializeField] private TurnManager turnManager;
    private List<RenderTexture> eRT = new List<RenderTexture>();
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
        if (vRT.Count<Map.LayerBounds.Count) Debug.LogError("[Fogs] Not enough render textures set!");
        for (int i = 0; i<vRT.Count; i++) {
            var bounds = Map.LayerBounds[i];
            if (vRT[i] != null) vRT[i].Release();
            vRT[i] = new RenderTexture(bounds.size.x*15+30, bounds.size.y*15+30, 0, RenderTextureFormat.a8);
            vRT[i].create();
            eRT.add(new RenderTexture(bounds.size.x*15+30, bounds.size.y*15+30, 0, RenderTextureFormat.a8));
            eRT[i].create();
            GameObject coolCamera = Instantiate(shadowCam, transform.parent);
            coolCamera.transform.position = bounds.center;
            Camera cool = coolCamera.GetComponent<Camera>();
            cool.size = bounds.size.y+2;
            cool.targetTexture = vRT[i];
            cool.cullingMask = -1;
            //Have Camera Blit Alpha of floor Onto eRT, then set culling layers to Shadow
            cool.cullingMask = 1 << LayerMask.NameToLayer("ShadowLayer");
        }
        Debug.Log("[CameraManager] Shadow Cameras Created.");
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
        Graphics.Blit(vRT[l], eRT[l], addingMaterial);
        return;
    }
    void LateUpdate()
    {
       transform.position = Vector3.SmoothDamp(transform.position, pos, ref velocity, smoothTime);
    }

    void OnDestroy() {
        while (eRT.Count>0) {
            eRT[0].Release();
            eRT.RemoveAt(0);
        }
    }

}
