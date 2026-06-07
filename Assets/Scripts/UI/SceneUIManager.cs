using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneUIManager : MonoBehaviour
{
    [SerializeField] private GameObject inGameOverlays;
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private GameObject allPauseOverlays;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject defeatScreen;
    [SerializeField] private TextMeshProUGUI causeDefeatTxt;
    #if UNITY_EDITOR
    [SerializeField] private SceneAsset mainMenu;
    #endif
    [SerializeField] private PlayerController playerController;

    public static SceneUIManager Instance;
    
    private bool paused = false;
    private bool gameEnd = false;

    public void Start()
    {
        Instance = this;
        inGameOverlays.SetActive(true);
        pauseOverlay.SetActive(false);
        allPauseOverlays.SetActive(true);
        victoryScreen.SetActive(false);
        defeatScreen.SetActive(false);
    }

    public void TogglePause()
    {
        if (gameEnd) return;
        inGameOverlays.SetActive(paused);
        pauseOverlay.SetActive(!paused);
        playerController.SetPaused(!paused);
        paused = !paused;
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenu.name);
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MissionComplete()
    {
        playerController.SetPaused(true);
        inGameOverlays.SetActive(false);
        allPauseOverlays.SetActive(false);
        victoryScreen.SetActive(true);
        gameEnd = true;
    }

    public void MissionFailed(string cause)
    {
        playerController.SetPaused(true);
        inGameOverlays.SetActive(false);
        allPauseOverlays.SetActive(false);
        defeatScreen.SetActive(true);
        causeDefeatTxt.text = cause;
        gameEnd = true;
    }
}
