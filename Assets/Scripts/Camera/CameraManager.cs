using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using HeistGame.Door;
using System;

[RequireComponent(typeof(Camera))]
public class CameraManager : MonoBehaviour, ITurnActor
{
    [Header("Locations")]
    [SerializeField] private List<Vector2> layerLocations;
    [Header("Vents")]
    [SerializeField] private bool isTopLocationVents = true;
    [SerializeField] private GameObject vents;
    [Header("Cameras")]
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private int shadowRenderTextureScale = 15;
    [Header("The Sun")]
    [SerializeField] private bool hasSun = true;
    [SerializeField] private GameObject sunLight;
    [SerializeField] private GameObject sunLightLocations;
    [SerializeField] [Range(0,10)] private int sunAmount = 3;
    [SerializeField] [Range(0f,360f)] private float sunDirection = 0f;
    [SerializeField] [Range(0f,1f)] private float sunLowering = 0.6f;
    [SerializeField] [Range(0f,100f)] private float sunExtraDistanceFromTilemap = 14;
    [Header("Light Baking")]
    [SerializeField] private bool bakeLights = true;
    [SerializeField] private bool alwaysBake = false;
    [SerializeField] [Range(0, 50)] private int lightTextureResolution = 30;
    [SerializeField] private GameObject lightFloored;
    [SerializeField] private GameObject bakedLightLocations;
    [SerializeField] private GameObject bakedLightLocationsLocationns;
    [Header("Backround Removal")]
    [SerializeField] private GameObject sinRadiance;
    [SerializeField] private GameObject playerLight;
    [SerializeField] private GameObject BaseWall;
    [Header("References")]
    [SerializeField] private GameObject shadowCam;
    [SerializeField] private GameObject cam;
    [SerializeField] private GameObject shadowSpotlight;
    [SerializeField] private GameObject globalRadiance;
    [SerializeField] private GameObject tinyCarrotLight;
    [SerializeField] private Volume volume;
    [SerializeField] private TurnManager turnManager;
    [HideInInspector] [SerializeField] private List<BoundsInt> layerBounds;
    private readonly List<RenderTexture> inputRT = new();
    private readonly List<RenderTexture> outputRT = new();
    private Material addingMaterial;
    private readonly List<Material> shadowMaterials = new();
    private Vector3 velocity = Vector3.zero;
    private Camera mainCamera;
    private Shader shader;
    private readonly List<RenderTexture> textures = new();
    private const int SHADOWEXTRASIDESIZES = 4;
    private const float AC = Mathf.PI/180f;
    private readonly List<Camera> cameras = new(), coolCameras = new();
    private const float camFixer = 0.7f;
    private bool queued = false;
    [HideInInspector] public Vector3 pos = Vector3.zero;
    public int TickDebt { get; set; }
    void OnValidate() {
        //Setup Locations
        if (!Map.IsInitialized||Application.isPlaying||queued) return;
        queued = true;
        EditorApplication.delayCall += () => {
            queued = false;
            List<Vector2> locations = new() { Vector2.zero};
            foreach (Vector2 i in layerLocations) locations.Add(i);
            Map.layerLocations = locations;
            Map.ReloadLayerBounds();
            layerBounds = Map.LayerBounds;
            mainCamera ??= GetComponent<Camera>();
            if (Application.isPlaying) return;
            
            //Setup Vents if needed
            if (isTopLocationVents||vents!=null) {
                for (int i = 0; i<vents.transform.childCount; i++) {
                    Transform t = vents.transform.GetChild(i), vent = t.GetChild(0);
                    vent.parent = t;
                    vent.localPosition = (Vector3)(layerLocations[^1] - Map.layerLocations[Map.LayerByPos(t.position)]+Vector2.up);
                    vent.gameObject.GetComponent<Vent>().otherVent = t;
                    t.gameObject.GetComponent<Vent>().otherVent = vent;
                }
            }

            //Bake Lights
            if ((bakeLights||alwaysBake)&&lightFloored!=null&&lightTextureResolution>0&&bakedLightLocations!=null&&bakedLightLocationsLocationns!=null) {
                bakeLights = false;

                //Get Formats
                List<GraphicsFormat> supportedFormats = new() {
                    GraphicsFormat.R8G8B8_UNorm,
                    GraphicsFormat.R8G8B8_SNorm,
                    GraphicsFormat.R8G8B8_SInt,
                    GraphicsFormat.R8G8B8_SRGB,
                    GraphicsFormat.R8G8B8A8_UNorm,
                    GraphicsFormat.R8G8B8A8_SNorm,
                    GraphicsFormat.R8G8B8A8_SNorm,
                    GraphicsFormat.R8G8B8A8_SInt,
                    GraphicsFormat.R8G8B8A8_SRGB
                }, depthFormats = new() {
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
                    Debug.LogError("[CameraManager] No suitable baking RenderTexture format found!");
                    return;
                }
                for (int i = 0; i < depthFormats.Count && !SystemInfo.IsFormatSupported(dFormat, GraphicsFormatUsage.Render); i++)
                    dFormat = depthFormats[i];
                if (!SystemInfo.IsFormatSupported(dFormat, GraphicsFormatUsage.Render)) {
                    Debug.LogError("[CameraManager] No suitable baking DepthStencilFormat found!");
                    return;
                }

                //Setup GameObjects
                for (int i = bakedLightLocationsLocationns.transform.childCount-1; i >= 0; i--) {
                    Light2D l = bakedLightLocationsLocationns.transform.GetChild(i).GetComponent<Light2D>();
                    if (l != null) {
                        DestroyImmediate(l.lightCookieSprite);
                        l.lightCookieSprite = null;
                    }
                    DestroyImmediate(bakedLightLocationsLocationns.transform.GetChild(i).gameObject);
                }
                if (playerLight) playerLight.SetActive(false);
                if (mainCamera) mainCamera.enabled = false;
                DepthOfField blur = null;
                float coolblur = 1.2f;
                if (volume) if (volume.profile.TryGet<DepthOfField>(out blur)) {
                    coolblur = blur.gaussianMaxRadius.value;
                    blur.gaussianMaxRadius.value = 0.1f;
                }
                for (int i = 0; i<Map.layerLocations.Count; i++) {
                    var bounds = Map.LayerBounds[i];

                    //Create render Texture
                    RenderTexture rt = new((bounds.size.x+SHADOWEXTRASIDESIZES)*lightTextureResolution,(bounds.size.y+SHADOWEXTRASIDESIZES)*lightTextureResolution, 0, format);
                    rt.depthStencilFormat = dFormat;
                    rt.Create();
                    
                    //Create Block
                    GameObject block = Instantiate(lightFloored, new Vector3(bounds.center.x+0.5f, bounds.center.y+0.5f, 0), Quaternion.identity);
                    block.transform.localScale = new Vector3(bounds.size.x+SHADOWEXTRASIDESIZES+0.21f, bounds.size.y+SHADOWEXTRASIDESIZES+0.21f, 1);

                    //Create Camera
                    GameObject coolCamera = Instantiate(shadowCam, Vector3.zero, Quaternion.identity);
                    coolCamera.transform.position = new Vector3(bounds.center.x+0.5f, bounds.center.y+0.5f, -10);
                    Camera cool = coolCamera.GetComponent<Camera>();
                    cool.orthographicSize = (bounds.size.y+SHADOWEXTRASIDESIZES)*0.5f;
                    cool.targetTexture = rt;
                    cool.cullingMask = ~((1 << LayerMask.NameToLayer("ShadowLayer"))|(1 << LayerMask.NameToLayer("UI")));
                    var cameraData = cool.GetUniversalAdditionalCameraData();
                    cameraData.renderPostProcessing = true;

                    //RENDER!!!!
                    //SceneView sceneView = SceneView.lastActiveSceneView;
                    //sceneView.pivot = Map.layerLocations[i];
                    //sceneView.size = 20f;
                    //sceneView.Repaint();

                    cool.cameraType = CameraType.Game;
                    UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
                    request.destination = rt;
                    if (RenderPipeline.SupportsRenderRequest(cool, request))
                        RenderPipeline.SubmitRenderRequest(cool, request);

                    //Create Texture2D
                    Texture2D tex = new(rt.width, rt.height, TextureFormat.RGBA32, false);
                    RenderTexture.active = rt;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;

                    //Create Light
                    GameObject light = Instantiate(bakedLightLocations, new Vector3(bounds.center.x+0.5f, bounds.center.y+0.5f, 0), Quaternion.identity);
                    light.transform.parent = bakedLightLocationsLocationns.transform;
                    light.GetComponent<Light2D>().lightCookieSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one*0.5f, lightTextureResolution);

                    //Cleanup
                    rt.Release();
                    DestroyImmediate(coolCamera);
                    DestroyImmediate(block);
                }
                if (playerLight) playerLight.SetActive(true);
                if (mainCamera) mainCamera.enabled = true;
                if (blur) blur.gaussianMaxRadius.Override(coolblur);
            }
            
            //The Sun
            if (hasSun&&sunAmount>0&&sunLight!=null&&sunLightLocations!=null) {
                for (int i = sunLightLocations.transform.childCount-1; i >= 0; i--) 
                    DestroyImmediate(sunLightLocations.transform.GetChild(i).gameObject);
                int suns = 0;
                for (int i = 0; i<Map.layerLocations.Count; i++) {
                    if (isTopLocationVents&&i==Map.layerLocations.Count-1) continue;
                    float distance = Map.LayerBounds[i].size.magnitude*0.5f+sunExtraDistanceFromTilemap, dir = 2*Mathf.PI/sunAmount;
                    Vector2 center = Map.LayerBounds[i].center;
                    for (int ii = 0; ii < sunAmount; ii++) {
                        float direction = ii*dir+sunDirection*AC;
                        GameObject sun = Instantiate(sunLight, center+new Vector2(Mathf.Sin(direction)*distance,Mathf.Cos(ii*dir+sunDirection*AC)*distance), Quaternion.identity);
                        sun.transform.parent = sunLightLocations.transform;
                        sun.GetComponent<Light2D>().intensity*=sunLowering+Mathf.Abs((1-2*((float)ii)/sunAmount)*(1-sunLowering));
                        sun.transform.eulerAngles = new Vector3(0,0,180f -ii*360f/sunAmount-sunDirection);
                        suns++;
                    }
                }
                //Debug.Log(Map.layerLocations.Count);
                Debug.Log("[CameraManager] Created "+suns+" Suns in Scene");
            }
        };
    }
    void Awake() {
        if (this == null) return;
        mainCamera = GetComponent<Camera>();
        List<Vector2> locations = new() { Vector2.zero};
        foreach (Vector2 v in layerLocations) locations.Add(v);
        Map.layerLocations = locations;
        Map.SetLayerBounds(layerBounds);
        if (layerLocations.Count==0||cam==null||mainCamera == null) return;
        var cameraData = mainCamera.GetUniversalAdditionalCameraData();
        if (cameraData == null) return;
        cameraData.cameraStack.Clear();
        cameras.Clear();
        transform.position = new Vector3(0,0,-10);

        //Setup Map Layer Cameras
        for (int i = 0; i<layerLocations.Count; i++) {
            GameObject camObject = Instantiate(cam,transform);
            camObject.transform.parent = transform;
            camObject.transform.position = new Vector3(layerLocations[i].x, layerLocations[i].y-i-1, -10);
            Camera c = camObject.GetComponent<Camera>();
            c.orthographicSize=mainCamera.orthographicSize;
            cameras.Add(c);
            cameraData.cameraStack.Add(c);
        }
        Debug.Log("[CameraManager] Camera stack updated with " + cameras.Count + " cameras.");
        shader = Shader.Find("Custom/ShadowChanger");
        if (shader == null) {
            Debug.LogError("[CameraManager] Failed to find shader");
            return;
        }

        //Setup Formats
        List<GraphicsFormat> supportedFormats = new() {
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
        }, depthFormats = new() {
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

            //Create render Textures
            inputRT.Add(new RenderTexture((bounds.size.x+SHADOWEXTRASIDESIZES)*shadowRenderTextureScale,(bounds.size.y+SHADOWEXTRASIDESIZES)*shadowRenderTextureScale, 0, format));
            inputRT[i].depthStencilFormat = dFormat;
            inputRT[i].Create();
            outputRT.Add(new RenderTexture((bounds.size.x+SHADOWEXTRASIDESIZES)*shadowRenderTextureScale,(bounds.size.y+SHADOWEXTRASIDESIZES)*shadowRenderTextureScale, 0, format));
            outputRT[i].depthStencilFormat = dFormat;
            outputRT[i].Create();

            //Create the shadow input
            GameObject coolCamera = Instantiate(shadowCam, Vector3.zero, Quaternion.identity);
            coolCamera.transform.position = new Vector3(bounds.center.x+0.5f, bounds.center.y-0.19f, -10);
            Camera cool = coolCamera.GetComponent<Camera>();
            cool.orthographicSize = (bounds.size.y+SHADOWEXTRASIDESIZES)*0.5f;
            cool.targetTexture = inputRT[i];
            cool.cullingMask = ~((1 << LayerMask.NameToLayer("ShadowLayer"))|(1 << LayerMask.NameToLayer("UI")));
            Graphics.Blit(Texture2D.blackTexture, outputRT[i]);
            Graphics.Blit(Texture2D.blackTexture, inputRT[i]);

            //Create the shadow output
            shadowMaterials.Add(new Material(shader));
            shadowMaterials[i].SetTexture("_OtherTex", inputRT[i]);
            GameObject permalightOutput = Instantiate(shadowSpotlight, transform.position, Quaternion.identity);
            permalightOutput.transform.position = new Vector3(bounds.center.x+0.5f, bounds.center.y+0.5f, 0);
            permalightOutput.transform.GetChild(0).GetComponent<RawImage>().texture = outputRT[i];
            permalightOutput.transform.GetChild(0).GetComponent<RawImage>().material = shadowMaterials[i];
            permalightOutput.GetComponent<RectTransform>().localScale = new Vector3(bounds.size.x+SHADOWEXTRASIDESIZES, bounds.size.y+SHADOWEXTRASIDESIZES, 1);
            coolCameras.Add(cool);
        }
        Debug.Log("[CameraManager] Shadow Cameras Created.");
    }

    private void OnEnable() {
        //Setup Turn Manager
        if (turnManager == null) turnManager = TurnManager.Instance;
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>().GetComponent<TurnManager>();
        if (turnManager != null) turnManager.Register(this);
    }

    void Start() {
        addingMaterial = new Material(Shader.Find("Custom/AddingShader"));
        int l = Map.CurrentLayer();
        pos = Map.Player.transform.position+(l==0?new Vector3(0,0,-10) : new Vector3(-layerLocations[l-1].x, l-layerLocations[l-1].y, -10));
        for (int i = 0; i < cameras.Count; i++) 
            cameras[i].enabled = i < l;

        //Have Camera Blit Alpha of the Floor Onto shadow render texture, then set culling layers to ShadowLayer
        Material m = new(Shader.Find("Custom/allAlpha"));
        if (sinRadiance) {
            sinRadiance.SetActive(false);
            Destroy(sinRadiance); //The reason for both of these is because Destroy isn't immediate, and DestroyImmediate isn't safe to use during runtime. Let it be.
        }
        if (vents) for (int i = 0; i<vents.transform.childCount; i++) {
            Light2D vent = vents.GetComponentInChildren<Light2D>();
            if (vent) Destroy(vent.gameObject);
        }
        if (playerLight) playerLight.SetActive(false);
        if (BaseWall) BaseWall.SetActive(false);
        for (int i = 0; i < inputRT.Count; i++) {
            coolCameras[i].Render();
            if (!isTopLocationVents||i!=inputRT.Count-1) Graphics.Blit(inputRT[i], outputRT[i]);

            coolCameras[i].gameObject.transform.position += new Vector3(0,0.69f,0);
            if (!isTopLocationVents||i!=inputRT.Count-1) {
                RenderTexture tempp = new(outputRT[i].width, outputRT[i].height, 1, outputRT[i].graphicsFormat);
                tempp.Create();
                RenderTexture temppp = new(outputRT[i].width, outputRT[i].height, 1, outputRT[i].graphicsFormat);
                Graphics.CopyTexture(outputRT[i], temppp);
                coolCameras[i].Render();
                Graphics.Blit(inputRT[i], tempp);
                addingMaterial.SetTexture("_OtherTex", temppp);
                Graphics.Blit(tempp, outputRT[i], addingMaterial);
                tempp.Release();
                temppp.Release();
            }

            coolCameras[i].cullingMask = 1 << LayerMask.NameToLayer("ShadowLayer");
            var cameraData = coolCameras[i].GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = enabled;
        }
        if (playerLight) playerLight.SetActive(true);
        if (BaseWall) BaseWall.SetActive(true);

        //Setup Blit
        if (globalRadiance) Destroy(globalRadiance);
        transform.position = pos;
        coolCameras[l].Render();
        coolCameras.Clear();

        //First Blit
        RenderTexture temp = new(outputRT[l].width, outputRT[l].height, 1, outputRT[l].graphicsFormat);
        Graphics.CopyTexture(outputRT[l], temp);
        addingMaterial.SetTexture("_OtherTex", temp);
        Graphics.Blit(inputRT[l], outputRT[l], addingMaterial);
        temp.Release();
    }

    private void OnDisable() {
        //No more Turn Manager
        if (turnManager != null) turnManager.Unregister(this);
    }
    
    public async Awaitable OnTick() {
        TickDebt = 0;
        if (!enabled) return;

        //Set Locations
        int l = Map.CurrentLayer();
        pos = Map.Player.transform.position+(l==0?new Vector3(0,0,-10) : new Vector3(-layerLocations[l-1].x, l-layerLocations[l-1].y, -10));
        tinyCarrotLight.transform.localPosition = l==0?new Vector3(0,0,10) : new Vector3(layerLocations[l-1].x, layerLocations[l-1].y-l, 10);
        for (int i = 0; i < cameras.Count; i++) 
            cameras[i].enabled = i < l;
        
        //Blit Shadows
        RenderTexture temp = new(outputRT[l].width, outputRT[l].height, 1, outputRT[l].graphicsFormat);
        Graphics.CopyTexture(outputRT[l], temp);
        addingMaterial.SetTexture("_OtherTex", temp);
        Graphics.Blit(inputRT[l], outputRT[l], addingMaterial);
        temp.Release();
        return;
    }

    void LateUpdate()
    {
        //Set Camera Position
        float camFix = Mathf.Pow(camFixer, Time.deltaTime);
        if (Vector3.Distance(transform.position, pos) > 100) transform.position = pos;
        else transform.position = Vector3.SmoothDamp(transform.position, pos, ref velocity, smoothTime)*camFix + pos*(1-camFix);
    }

    void OnDestroy() {
        //Destroy All of the Render Textures
        while (outputRT.Count>0) {
            outputRT[0].Release();
            outputRT.RemoveAt(0);
        }
        while (inputRT.Count>0) {
            inputRT[0].Release();
            inputRT.RemoveAt(0);
        }
    }
}
