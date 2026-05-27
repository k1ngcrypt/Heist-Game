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

    private List<Button> allButtons = new List<Button>();

    private VerticalLayoutGroup layout;
    private RectTransform textRect;
    private RectTransform menuRect;

    public void Initialize(string title, List<InteractBtnTemplate> actions, Vector3 worldPosition)
    {
        layout = menuPanel.GetComponent<VerticalLayoutGroup>();
        textRect = titleText.GetComponent<RectTransform>();
        menuRect = menuPanel.GetComponent<RectTransform>();

        menuPanel.gameObject.SetActive(true); //make sure layout is correct and then will close

        titleText.text = title;
        canvas.GetComponent<Transform>().position = worldPosition;
        menuPanel.GetComponent<Transform>().position = worldPosition - new Vector3(0, 0.5f, 0); // One tile down;
        canvas.worldCamera = Camera.main;

        foreach (var btnData in actions)
        {
            Button btn = Instantiate(btnPrefab, menuPanel);
            btn.GetComponentInChildren<TMP_Text>().text = btnData.text;

            btn.onClick.AddListener(() => btnData.onClick.Invoke());
            allButtons.Add(btn);
        }

        ResizeMenu();        

        UpdateButtons();

        ToggleMenuStatus(); //close it for start
    }

    private void ResizeMenu()
    {
        float paddingBottom = layout.padding.bottom;
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


        menuRect.sizeDelta = new Vector2(menuRect.sizeDelta.x, -(pos - (height*0.5f) - paddingBottom));
    }

    public void UpdateButtons()
    {
        //do smth please
    }

    public void ToggleMenuStatus()
    {
        if (menuPanel.gameObject.activeSelf)
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