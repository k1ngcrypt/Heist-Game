using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour, ITurnActor
{
    [Header("Locations")]
    [SerializeField] private List<Vector2> lLocations;
    [Header("Property")]
    [SerializeField] private List<RenderTexture> vRT;
    [SerializeField] private List<RenderTexture> eRT;
    [Header("Cameras")]
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private Material addingMaterial;
    [Header("References")]
    [SerializeField] private GameObject shadowCam;
    [SerializeField] private GameObject cam;
    [SerializeField] private GameObject SPOTLIGHT;
    [SerializeField] private TurnManager turnManager;
    [HideInInspector] [SerializeField] private List<BoundsInt> layerBounds;
    private Vector3 velocity = Vector3.zero;
    private Camera mainCamera;
    private List<Camera> cameras = new List<Camera>();
    private const float camFixer = 0.7f;
    private bool queued = false;
    [HideInInspector] public Vector3 pos = Vector3.zero;
    public int TickDebt { get; set; }
    void OnValidate() {
        if (!Map.IsInitialized||queued) return;
        queued = true;
        EditorApplication.delayCall += () => {
            queued = false;
            List<Vector2> locations = new List<Vector2> {Vector2.zero};
            foreach (Vector2 i in lLocations) locations.Add(i);
            Map.layerLocations = locations;
            Map.ReloadLayerBounds();
            layerBounds = Map.LayerBounds;
        };
    }
    void Awake() {
        if (this == null) return;
        mainCamera = GetComponent<Camera>();
        List<Vector2> locations = new List<Vector2> {Vector2.zero};
        foreach (Vector2 i in lLocations) locations.Add(i);
        Map.layerLocations = locations;
        Map.SetLayerBounds(layerBounds);
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
        Debug.Log(vRT.Count);
        if (vRT.Count<Map.LayerBounds.Count||eRT.Count<Map.LayerBounds.Count) {
            Debug.LogError("[CameraManager] Not enough render textures set!");
            return;
        }
        GraphicsFormat format = GraphicsFormat.R8_UNorm;
        if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
            Debug.LogWarning("[CameraManager] R8_UNorm not supported, trying next format...");
            format = GraphicsFormat.R8_SNorm;
            if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
            format = GraphicsFormat.R8_UInt;
            if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
            format = GraphicsFormat.R8_SInt;
            if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
            format = GraphicsFormat.R8_SRGB;
            if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                Debug.LogWarning("[CameraManager] R8_SRGB not supported, trying next format...");
                format = GraphicsFormat.R16_UNorm;
                if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                format = GraphicsFormat.R16_UInt;
                if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                format = GraphicsFormat.R16_SNorm;
                if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                format = GraphicsFormat.R16_SInt;
                if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                format = GraphicsFormat.R16_SFloat;
                if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                    format = GraphicsFormat.R4G4B4A4_UNormPack16;
                    if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                    Debug.LogWarning("[CameraManager] R4G4B4A4_UNormPack16 not supported, trying next format...");
                    format = GraphicsFormat.R8G8_UNorm;
                    if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                    format = GraphicsFormat.R8G8B8_UNorm;
                    if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                    format = GraphicsFormat.R8G8B8_SNorm;
                    if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                    format = GraphicsFormat.R8G8B8_SInt;
                    if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                    format = GraphicsFormat.R8G8B8_SRGB;
                    if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                        Debug.LogWarning("[CameraManager] R8G8B8_SRGB not supported, trying next format...");
                        format = GraphicsFormat.R8G8B8A8_UNorm;
                        if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                        format = GraphicsFormat.R8G8B8A8_SNorm;
                        if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                        format = GraphicsFormat.R8G8B8A8_SNorm;
                        if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                        format = GraphicsFormat.R8G8B8A8_SInt;
                        if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                        format = GraphicsFormat.R8G8B8A8_SRGB;
                        if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
                            Debug.LogError("[CameraManager] No suitable RenderTexture format found!");
                            return;
        }}}}}}}}}}}}}}}}}}}}}
        Debug.Log("[CameraManager] Using RenderTexture format: " + format);
        for (int i = 0; i<vRT.Count; i++) {
            Debug.Log(Map.LayerBounds[i]);
            var bounds = Map.LayerBounds[i];
            if (vRT[i] != null) vRT[i].Release();
            vRT[i].width = bounds.size.x*15+30;
            vRT[i].height = bounds.size.y*15+30;
            vRT[i].depth = 1;
            vRT[i].graphicsFormat = format;
            vRT[i].Create();
            if (eRT[i] != null) eRT[i].Release();
            eRT[i].width = bounds.size.x*15+30;
            eRT[i].height = bounds.size.y*15+30;
            eRT[i].depth = 1;
            eRT[i].graphicsFormat = format;
            eRT[i].Create();
            GameObject coolCamera = Instantiate(shadowCam, Vector3.zero, Quaternion.identity);
            coolCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
            Camera cool = coolCamera.GetComponent<Camera>();
            cool.orthographicSize = (bounds.size.y+2)/2.0f;
            cool.targetTexture = vRT[i];
            cool.cullingMask = -1;
            Graphics.Blit(Texture2D.blackTexture, eRT[i]);
            RenderTexture.active = eRT[i];
            //GL.Clear(true, true, Color.black);
            //RenderTexture.active = null;
            //Have Camera Blit Alpha of floor Onto eRT, then set culling layers to Shadow
            cool.cullingMask = 1 << LayerMask.NameToLayer("ShadowLayer");
            GameObject light = Instantiate(SPOTLIGHT, transform.position, Quaternion.identity);
            light.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0);
            light.transform.GetChild(0).GetComponent<RawImage>().texture = eRT[i];
            light.GetComponent<RectTransform>().localScale = new Vector3(bounds.size.x+2, bounds.size.y+2, 1);
        }
        Debug.Log("[CameraManager] Shadow Cameras Created.");
    }

    private void OnEnable()
    {
        if (turnManager == null) turnManager = TurnManager.Instance;
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>().GetComponent<TurnManager>();
        if (turnManager != null) turnManager.Register(this);
    }

    void Start() {
        int l = Map.currentLayer();
        pos = Map.Player.transform.position+(l==0?new Vector3(0,0,-10) : new Vector3(-lLocations[l-1].x, -lLocations[l-1].y, -10));
        for (int i = 0; i < cameras.Count; i++) 
            cameras[i].enabled = i < l;
        //Graphics.Blit(vRT[l], eRT[l], addingMaterial);
        transform.position = pos;
    }

    private void OnDisable()
    {
        if (turnManager != null) turnManager.Unregister(this);
    }
    
    public async Awaitable OnTick()
    {
        TickDebt = 0;
        if (!enabled) return;
        int l = Map.currentLayer();
        pos = Map.Player.transform.position+(l==0?new Vector3(0,0,-10) : new Vector3(-lLocations[l-1].x, -lLocations[l-1].y, -10));
        for (int i = 0; i < cameras.Count; i++) 
            cameras[i].enabled = i < l;
        Graphics.Blit(vRT[l], eRT[l], addingMaterial);
        return;
    }
    void LateUpdate()
    {
        float camFix = Mathf.Pow(camFixer, Time.deltaTime);
        if (Vector3.Distance(transform.position, pos) > 100) {
            transform.position = pos;
            Debug.LogWarning("[CameraManager] Teleported camera to " + pos + " due to large distance.");
        }
        else transform.position = Vector3.SmoothDamp(transform.position, pos, ref velocity, smoothTime)*camFix + pos*(1-camFix);
        //Debug.Log("Camera position: " + transform.position + ", Target position: " + pos);
    }

    void OnDestroy() {
        while (eRT.Count>0) {
            eRT[0].Release();
            eRT.RemoveAt(0);
        }
    }

}
