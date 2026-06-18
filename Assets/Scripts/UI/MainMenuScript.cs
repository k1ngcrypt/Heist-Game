using UnityEngine;

public class MainMenuScript : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject creditsCanvas;
    [SerializeField] private GameObject loadoutCanvas;
    [SerializeField] private GameObject lvlSelectCanvas;
    [SerializeField] private GameObject settingCanvas;
    [SerializeField] private GameObject movingBGCanvas;

    void Start()
    {
        mainMenuCanvas.SetActive(true);
        mainMenuCanvas.GetComponent<Canvas>().sortingOrder = 0;

        creditsCanvas.SetActive(false);
        creditsCanvas.GetComponent<Canvas>().sortingOrder = 0;

        loadoutCanvas.SetActive(false);
        loadoutCanvas.GetComponent<Canvas>().sortingOrder = 0;

        lvlSelectCanvas.SetActive(false);
        lvlSelectCanvas.GetComponent<Canvas>().sortingOrder = 0;

        settingCanvas.SetActive(false);
        settingCanvas.GetComponent<Canvas>().sortingOrder = 0;

        movingBGCanvas.SetActive(true);
        movingBGCanvas.GetComponent<Canvas>().sortingOrder = -1;
    }

    public void QuitGame()
    {
        SettingManager.Instance.QuitGame();
    }
}
