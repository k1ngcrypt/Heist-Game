using System.Collections.Generic;
using System.Xml.Schema;
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
    [Header("Cameras")]
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private int shadowRenderTextureScale = 15;
    [Header("References")]
    [SerializeField] private GameObject shadowCam;
    [SerializeField] private GameObject cam;
    [SerializeField] private GameObject SPOTLIGHT;
    [SerializeField] private GameObject radiance;
    [SerializeField] private GameObject otherRadiance;
    [SerializeField] private GameObject tinyCarrotLight;
    [SerializeField] private TurnManager turnManager;
    [HideInInspector] [SerializeField] private List<BoundsInt> layerBounds;
    private List<RenderTexture> vRT = new List<RenderTexture>();
    private List<RenderTexture> eRT = new List<RenderTexture>();
    private Material addingMaterial;
    private List<Material> shadowMaterials = new List<Material>();
    private Vector3 velocity = Vector3.zero;
    private Camera mainCamera;
    private Shader shader;
    private const int SHADOWEXTRA = 4;
    private List<Camera> cameras = new List<Camera>(), coolCameras = new List<Camera>();
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
        foreach (Vector2 ii in lLocations) locations.Add(ii);
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
        shader = Shader.Find("Custom/ShadowChanger");
        if (shader == null) {
            Debug.LogError("[CameraManager] Failed to find shader");
            return;
        }
        List<GraphicsFormat> supportedFormats = new List<GraphicsFormat>() {
            GraphicsFormat.R8_UNorm,
            GraphicsFormat.R8_SNorm,
            GraphicsFormat.R8_UInt,
            GraphicsFormat.R8_SInt,
            GraphicsFormat.R8_SRGB,
            GraphicsFormat.R16_UNorm,
            GraphicsFormat.R16_UInt,
            GraphicsFormat.R16_SNorm,
            GraphicsFormat.R16_SInt,
            GraphicsFormat.R16_SFloat,
            GraphicsFormat.R4G4B4A4_UNormPack16,
            GraphicsFormat.R8G8_UNorm,
            GraphicsFormat.R8G8B8_UNorm,
            GraphicsFormat.R8G8B8_SNorm,
            GraphicsFormat.R8G8B8_SInt,
            GraphicsFormat.R8G8B8_SRGB,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.R8G8B8A8_SNorm,
            GraphicsFormat.R8G8B8A8_SNorm,
            GraphicsFormat.R8G8B8A8_SInt,
            GraphicsFormat.R8G8B8A8_SRGB
        }, depthFormats = new List<GraphicsFormat> {
            GraphicsFormat.D16_UNorm,
            GraphicsFormat.D16_UNorm_S8_UInt,
            GraphicsFormat.D24_UNorm,
            GraphicsFormat.D24_UNorm_S8_UInt,
            GraphicsFormat.D32_SFloat,
            GraphicsFormat.D32_SFloat_S8_UInt
        };
        GraphicsFormat format = supportedFormats[0], dFormat = depthFormats[0];
        for (int i = 0; i < supportedFormats.Count && !SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render); i++)
            format = supportedFormats[i];
        if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)) {
            Debug.LogError("[CameraManager] No suitable RenderTexture format found!");
            return;
        }
        for (int i = 0; i < depthFormats.Count && !SystemInfo.IsFormatSupported(dFormat, GraphicsFormatUsage.Render); i++)
            dFormat = depthFormats[i];
        if (!SystemInfo.IsFormatSupported(dFormat, GraphicsFormatUsage.Render)) {
            Debug.LogError("[CameraManager] No suitable DepthStencilFormat found!");
            return;
        }
        Debug.Log("[CameraManager] Using RenderTexture format: " + format);
        for (int i = 0; i<Map.layerLocations.Count; i++) {
            var bounds = Map.LayerBounds[i];

            vRT.Add(new RenderTexture((bounds.size.x+SHADOWEXTRA)*shadowRenderTextureScale,(bounds.size.y+SHADOWEXTRA)*shadowRenderTextureScale, 0, format));
            vRT[i].depthStencilFormat = dFormat;
            vRT[i].Create();
            eRT.Add(new RenderTexture((bounds.size.x+SHADOWEXTRA)*shadowRenderTextureScale,(bounds.size.y+SHADOWEXTRA)*shadowRenderTextureScale, 0, format));
            eRT[i].depthStencilFormat = dFormat;
            eRT[i].Create();

            GameObject coolCamera = Instantiate(shadowCam, Vector3.zero, Quaternion.identity);
            coolCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y-0.69f, -10);
            Camera cool = coolCamera.GetComponent<Camera>();
            cool.orthographicSize = (bounds.size.y+SHADOWEXTRA)/2.0f;
            cool.targetTexture = vRT[i];
            cool.cullingMask = (~(1 << LayerMask.NameToLayer("ShadowLayer")))&(~(1 << LayerMask.NameToLayer("UI")));
            Graphics.Blit(Texture2D.blackTexture, eRT[i]);
            Graphics.Blit(Texture2D.blackTexture, vRT[i]);

            shadowMaterials.Add(new Material(shader));
            shadowMaterials[i].SetTexture("_OtherTex", vRT[i]);
            GameObject permalight = Instantiate(SPOTLIGHT, transform.position, Quaternion.identity);
            permalight.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0);
            permalight.transform.GetChild(0).GetComponent<RawImage>().texture = eRT[i];
            permalight.transform.GetChild(0).GetComponent<RawImage>().material = shadowMaterials[i];
            permalight.GetComponent<RectTransform>().localScale = new Vector3(bounds.size.x+SHADOWEXTRA, bounds.size.y+SHADOWEXTRA, 1);
            coolCameras.Add(cool);
        }
        Debug.Log("[CameraManager] Shadow Cameras Created.");
    }

    private void OnEnable() {
        if (turnManager == null) turnManager = TurnManager.Instance;
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>().GetComponent<TurnManager>();
        if (turnManager != null) turnManager.Register(this);
    }

    void Start() {
        addingMaterial = new Material(Shader.Find("Custom/SHADERRRRR"));
        int l = Map.currentLayer();
        pos = Map.Player.transform.position+(l==0?new Vector3(0,0,-10) : new Vector3(-lLocations[l-1].x, -lLocations[l-1].y, -10));
        for (int i = 0; i < cameras.Count; i++) 
            cameras[i].enabled = i < l;

        Material m = new Material(Shader.Find("Custom/allAlpha"));
        for (int i = 0; i < vRT.Count; i++) {
            coolCameras[i].Render();
            Graphics.Blit(vRT[i], eRT[i], m);

            coolCameras[i].gameObject.transform.position += new Vector3(0,0.69f,0);
            RenderTexture tempp = new RenderTexture(eRT[i].width, eRT[i].height, 1, eRT[i].graphicsFormat);
            tempp.Create();
            RenderTexture temppp = new RenderTexture(eRT[i].width, eRT[i].height, 1, eRT[i].graphicsFormat);
            Graphics.CopyTexture(eRT[i], temppp);
            coolCameras[i].Render();
            Graphics.Blit(vRT[i], tempp, m);
            addingMaterial.SetTexture("_OtherTex", temppp);
            Graphics.Blit(tempp, eRT[i], addingMaterial);

            //Have Camera Blit Alpha of floor Onto eRT, then set culling layers to Shadow
            coolCameras[i].cullingMask = 1 << LayerMask.NameToLayer("ShadowLayer");
            var cameraData = coolCameras[i].GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = enabled;
        }
        Destroy(radiance);
        
        transform.position = pos;
        coolCameras[l].Render();
        coolCameras.Clear();
        RenderTexture temp = new RenderTexture(eRT[l].width, eRT[l].height, 1, eRT[l].graphicsFormat);
        Graphics.CopyTexture(eRT[l], temp);
        addingMaterial.SetTexture("_OtherTex", temp);
        Graphics.Blit(vRT[l], eRT[l], addingMaterial);
        temp.Release();
        Destroy(otherRadiance);
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
        tinyCarrotLight.transform.localPosition = l==0?new Vector3(0,0,10) : new Vector3(lLocations[l-1].x, lLocations[l-1].y, 10);
        for (int i = 0; i < cameras.Count; i++) 
            cameras[i].enabled = i < l;
        
        RenderTexture temp = new RenderTexture(eRT[l].width, eRT[l].height, 1, eRT[l].graphicsFormat);
        Graphics.CopyTexture(eRT[l], temp);
        addingMaterial.SetTexture("_OtherTex", temp);
        Graphics.Blit(vRT[l], eRT[l], addingMaterial);
        temp.Release();
        return;
    }
    void LateUpdate()
    {
        float camFix = Mathf.Pow(camFixer, Time.deltaTime);
        if (Vector3.Distance(transform.position, pos) > 100) transform.position = pos;
        else transform.position = Vector3.SmoothDamp(transform.position, pos, ref velocity, smoothTime)*camFix + pos*(1-camFix);
    }

    void OnDestroy() {
        while (eRT.Count>0) {
            eRT[0].Release();
            eRT.RemoveAt(0);
        }
        while (vRT.Count>0) {
            vRT[0].Release();
            vRT.RemoveAt(0);
        }
    }
}
