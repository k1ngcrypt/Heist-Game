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

    public void Initialize(string title, List<InteractBtnTemplate> actions, Vector3 worldPosition)
    {
        menuPanel.gameObject.SetActive(true); //make sure layout is correct and then will close

        titleText.text = title;
        canvas.GetComponent<Transform>().position = worldPosition;
        menuPanel.GetComponent<Transform>().position = worldPosition - new Vector3(0, 0.5f, 0); // One tile down;
        canvas.worldCamera = Camera.main;

        List<Button> buttons = new List<Button>();
        foreach (var btnData in actions)
        {
            Button btn = Instantiate(btnPrefab, menuPanel);
            btn.GetComponentInChildren<TMP_Text>().text = btnData.text;

            btn.onClick.AddListener(() => btnData.onClick.Invoke());
            buttons.Add(btn);
        }

        //resize panel to fit buttons
        var layout = menuPanel.GetComponent<VerticalLayoutGroup>();
        float paddingBottom = layout.padding.bottom;

        var pnl = menuPanel.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(pnl);

        RectTransform lastElement;
        if (buttons.Count == 0)
        {
            lastElement = titleText.GetComponent<RectTransform>();
            Debug.Log("No buttons in interact area, resizing to fit title only");
        } else
        {
            lastElement = buttons[^1].GetComponent<RectTransform>();
            Debug.Log("Resizing interact area to fit " + buttons.Count + " buttons");
        }
         
        float pos = lastElement.anchoredPosition.y;
        float height = lastElement.rect.height;


        pnl.sizeDelta = new Vector2(pnl.sizeDelta.x, -(pos - (height*0.5f) - paddingBottom));

        EditMenu(); //close it for start
    }

    public void EditMenu()
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