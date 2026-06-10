using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InteractionOverlay : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Transform menuPanel;
    [SerializeField] private RectTransform miniImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button btnPrefab;

    public bool isMenuOpen => menuPanel != null && menuPanel.gameObject.activeInHierarchy;

    private List<Button> allButtons = new List<Button>();

    private VerticalLayoutGroup layout;
    private RectTransform textRect;
    private RectTransform menuRect;

    public void Initialize(string title, List<InteractBtnTemplate> actions, Vector3 worldPosition, PlayerController player)
    {
        layout = menuPanel.GetComponent<VerticalLayoutGroup>();
        textRect = titleText.GetComponent<RectTransform>();
        menuRect = menuPanel.GetComponent<RectTransform>();

        bool isInitialSpawn = allButtons.Count == 0;

        foreach (Button oldBtn in allButtons) {
            if (oldBtn != null) {
                oldBtn.transform.SetParent(null); 
                Destroy(oldBtn.gameObject);
            }
        }
        allButtons.Clear();

        menuPanel.gameObject.SetActive(true); //make sure layout is correct and then will close

        titleText.text = title;
        var txtRt = titleText.GetComponent<RectTransform>();
        txtRt.sizeDelta = new Vector2(btnPrefab.GetComponent<RectTransform>().sizeDelta.x, txtRt.sizeDelta.y);
        canvas.GetComponent<Transform>().position = worldPosition;
        menuPanel.GetComponent<Transform>().position = worldPosition - new Vector3(0, 0.5f, 0); // One tile down;
        if (!player.inVent) canvas.worldCamera = Camera.main;
        else {
            Camera[] allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Camera targetCam = Camera.main;
            float closestDistance = float.MaxValue;

            foreach (Camera c in allCams) {
                if (c == Camera.main || c.targetTexture != null || c.name.Contains("shadow")) continue;
                float dist = Vector2.Distance(new Vector2(c.transform.position.x, c.transform.position.y), new Vector2(worldPosition.x, worldPosition.y));
                if (dist < closestDistance) {
                    closestDistance = dist;
                    targetCam = c;
                }
            }
            canvas.worldCamera = targetCam;
        }

        int hotkeyNumber = 1;
        float basisHeight = btnPrefab.GetComponent<RectTransform>().rect.height - btnPrefab.GetComponentInChildren<TMP_Text>().preferredHeight - 5;
        foreach (var btnData in actions)
        {
            Button btn = Instantiate(btnPrefab, menuPanel);
            btn.GetComponentInChildren<TMP_Text>().text = $"[{hotkeyNumber}] {btnData.text}";

            var btnRect = btn.GetComponent<RectTransform>();

            btnRect.sizeDelta = new Vector2(btnRect.sizeDelta.x, basisHeight + btnRect.GetComponentInChildren<TMP_Text>().preferredHeight);

            btn.onClick.AddListener(() => btnData.onClick.Invoke());
            allButtons.Add(btn);
            hotkeyNumber++;
        }

        ResizeMenu();        

        UpdateButtons();
        if (isInitialSpawn){
            miniImage.localRotation = Quaternion.Euler(0, 0, 0);
            menuPanel.gameObject.SetActive(false);
        }
    }

    private void ResizeMenu()
    {
        float paddingBottom = layout.padding.bottom;
        float paddingLeft = layout.padding.left;
        float paddingRight = layout.padding.right;
        LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);

        RectTransform lastElement;
        if (allButtons.Count == 0)
        {
            lastElement = textRect;
        } else
        {
            lastElement = allButtons[^1].GetComponent<RectTransform>();
        }
         
        float pos = lastElement.anchoredPosition.y;
        float height = lastElement.rect.height;


        menuRect.sizeDelta = new Vector2(lastElement.sizeDelta.x + paddingLeft + paddingRight, -(pos - (height*0.5f) - paddingBottom));
    }

    public void UpdateButtons()
    {
        //do smth please
    }

    public void ToggleMenuStatus()
    {
        if (isMenuOpen)
        {
            //close menu
            miniImage.rotation = new Quaternion(0, 0, 0, 1);
            menuPanel.gameObject.SetActive(false);
        } else
        {
            //open menu
            miniImage.rotation = new Quaternion(0, 0, 180, 1);
            menuPanel.gameObject.SetActive(true);
        }
    }
}