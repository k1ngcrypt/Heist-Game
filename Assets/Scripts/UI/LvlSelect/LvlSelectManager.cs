using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEditor.Experimental.GraphView;

public class LvlSelectManager : MonoBehaviour
{
    [Header("Levels")]
    public List<LvlOrganizer> allLvls = new();
    public List<LvlOrganizer> startingUnlockedLvls = new();

    [Header("References")]
    public LvlBtnUI btnPrefab;
    public Canvas canvas;
    public Transform contents;
    public Transform scrollbar;
    public Transform sltLvl;
    public Transform unsltLvl;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    private LvlOrganizer currentLvl = null;

    List<LvlBtnUI> allBtns = new();

    private void Start()
    {
        GenerateLvlSelect();
    }
    public void GenerateLvlSelect()
    {
        if (canvas == null || btnPrefab == null || contents == null || sltLvl == null || unsltLvl == null || titleText == null || descriptionText == null)
        {
            Debug.LogWarning("LvlSelectManager is missing references (Canvas, BtnPrefab, Contents, SelectedLevel, UnselectedLevel, TitleText, DescriptionText).", this);
            return;
        }

        foreach (var lvl in allLvls)
        {
            var btn = Instantiate(btnPrefab, contents);
            btn.name = $"LvlBtn_{lvl.name}";
            btn.Initialize(lvl, this);
            btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            allBtns.Add(btn);
        }
        ResizeContent();
    }
    
    private void ResizeContent()
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        float width = btnPrefab.GetComponent<RectTransform>().rect.width;
        float height = btnPrefab.GetComponent<RectTransform>().rect.height;
        Vector2 corner = new Vector2(width/2f, height/2f);
        foreach (var btn in allBtns)
        {
            Vector2 pos = btn.GetComponent<RectTransform>().anchoredPosition;
            min = Vector2.Min(min, pos - corner);
            max = Vector2.Max(max, pos + corner);
        }
        min -= new Vector2(20, 0); // Padding
        max += new Vector2(20, 0); // Padding
        contents.GetComponent<RectTransform>().sizeDelta = max - min;
    }

    public void SelectedLvl(LvlOrganizer lvl)
    {
        if (!sltLvl.gameObject.activeSelf || unsltLvl.gameObject.activeSelf)
        {
            sltLvl.gameObject.SetActive(true);
            unsltLvl.gameObject.SetActive(false);
        }
        currentLvl = lvl;
        titleText.text = string.IsNullOrWhiteSpace(currentLvl.lvlTitle) ? currentLvl.sceneName : currentLvl.lvlTitle;
        descriptionText.text = currentLvl.lvlDescription;
    }

    public void PlayLvl()
    {
        // Show loadout canvas, then load correct scene when player clicks the button
    }
}
