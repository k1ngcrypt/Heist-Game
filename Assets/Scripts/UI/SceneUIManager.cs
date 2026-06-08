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

    public static SceneUIManager Instance;
    
    private bool paused = false;
    private bool gameEnd = false;
    private AwaitableCompletionSource<bool> _pauseSignal;

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
        SetPause(!paused);
    }

    public bool IsPaused()
    {
        return paused;
    }

    public AwaitableCompletionSource<bool> GetPauseSignal()
    {
        return _pauseSignal;
    }

    public void SetPause(bool pauseSignal)
    {
        paused = pauseSignal;
        if (!paused) {
            if (_pauseSignal != null) {
                _pauseSignal.SetResult(true);
                _pauseSignal = null;
            }
        } else {
            _pauseSignal = new AwaitableCompletionSource<bool>();
        }
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
        SetPause(true);
        inGameOverlays.SetActive(false);
        allPauseOverlays.SetActive(false);
        victoryScreen.SetActive(true);
        gameEnd = true;
    }

    public void MissionFailed(string cause)
    {
        SetPause(true);
        inGameOverlays.SetActive(false);
        allPauseOverlays.SetActive(false);
        defeatScreen.SetActive(true);
        causeDefeatTxt.text = cause;
        gameEnd = true;
    }
}
