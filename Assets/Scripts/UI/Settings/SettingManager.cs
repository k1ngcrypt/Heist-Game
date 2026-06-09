using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown graphicDropdown;
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private Toggle godModeToggle;
    [SerializeField] private Scrollbar volumeBar;
    [SerializeField] private AudioMixer audioMixer;
    public static SettingManager Instance { get; private set; }
    private Resolution[] resolutions;
    private List<string> options;

    private int currentResIdx = 0;
    private int currentGraphicIdx = 0;
    private bool fullScreen = false;
    private float volumeSlider = 0;


    public static bool godMode;

    public void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        } else
        {
            Instance.Reinitialize(resolutionDropdown, graphicDropdown, fullScreenToggle, godModeToggle, volumeBar);
            Destroy(this);
            return;
        }
        
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();
        options = new List<string>();
        for(int i = 0; i < resolutions.Length; i++)
        {
            options.Add($"{resolutions[i].width} x {resolutions[i].height}");
            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                Debug.Log($"Resolution found at {resolutions[i].width} x {resolutions[i].height}");
                currentResIdx = i;
            }
        }
        
        Reinitialize(resolutionDropdown, graphicDropdown, fullScreenToggle, godModeToggle, volumeBar);
    }

    public void Reinitialize(TMP_Dropdown resolutionDropdown, TMP_Dropdown graphicDropdown, Toggle fullScreenToggle, Toggle godModeToggle, Scrollbar volumeBar)
    {
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIdx;
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        resolutionDropdown.RefreshShownValue();


        graphicDropdown.value = currentGraphicIdx;
        graphicDropdown.onValueChanged.RemoveAllListeners();
        graphicDropdown.onValueChanged.AddListener(SetQuality);
        graphicDropdown.RefreshShownValue();


        fullScreenToggle.isOn = fullScreen;
        fullScreenToggle.onValueChanged.RemoveAllListeners();
        fullScreenToggle.onValueChanged.AddListener(setFullScreen);
        

        godModeToggle.isOn = godMode;
        godModeToggle.onValueChanged.RemoveAllListeners();
        godModeToggle.onValueChanged.AddListener(SetGodMode);


        if (volumeBar == null) return;
        volumeBar.value = volumeSlider;
        volumeBar.onValueChanged.RemoveAllListeners();
        volumeBar.onValueChanged.AddListener(SetVolume);

        Debug.Log("Everything initialized");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void SetQuality(int qualityIdx)
    {
        QualitySettings.SetQualityLevel(qualityIdx);
        currentGraphicIdx = qualityIdx;
        Debug.Log($"Quality set to {qualityIdx}");
    }

    public void setFullScreen(bool value)
    {
        Screen.fullScreen = value;
        fullScreen = value;
        Debug.Log($"Fullscreen set to {value}");
    }

    public void SetResolution(int resolutionIdx)
    {
        Resolution res = resolutions[resolutionIdx];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        currentResIdx = resolutionIdx;
        Debug.Log($"Resolution set to {res.width} x {res.height}");
    }

    public void SetVolume(float value) //value inbetween 0 and 1, with 1 being on the left, and 0 being on the right
    {
        Debug.Log($"Volume set to {value * -80f}");
        if (audioMixer == null || volumeBar == null) return;
        audioMixer.SetFloat("volume", value * -80f); //at the left, it becomes -80, and at the right, it becomes 0
        volumeSlider = value;
    }

    public void SetGodMode(bool value)
    {
        godMode = value;
        Debug.Log($"GodMode set to {value}");
    }
}
