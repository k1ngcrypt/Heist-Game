using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

public class SceneUIManager : MonoBehaviour
{
    [SerializeField] private GameObject inGameOverlays;
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private GameObject allPauseOverlays;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject defeatScreen;
    [SerializeField] private TextMeshProUGUI causeDefeatTxt;
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
            _pauseSignal?.SetResult(true);
            _pauseSignal = null;
        } else {
            _pauseSignal = new AwaitableCompletionSource<bool>();
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(0);
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
        if (SettingManager.Instance.godMode) return;
        SetPause(true);
        inGameOverlays.SetActive(false);
        allPauseOverlays.SetActive(false);
        defeatScreen.SetActive(true);
        causeDefeatTxt.text = cause;
        gameEnd = true;
    }

    public void QuitGame()
    {
        SettingManager.Instance.QuitGame();
    }
}
