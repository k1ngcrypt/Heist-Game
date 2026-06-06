using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LvlSelectManager : MonoBehaviour
{
    [Header("Levels")]
    [SerializeField] private List<LvlOrganizer> allLvls = new();
    [SerializeField] private List<LvlOrganizer> startingUnlockedLvls = new();

    [Header("References")]
    [SerializeField] private LvlBtnUI btnPrefab;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Transform lvlScroll;
    [SerializeField] private Transform contents;
    [SerializeField] private Transform scrollbar;
    [SerializeField] private Transform sltLvl;
    [SerializeField] private Transform unsltLvl;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private LoadoutManager loadoutManager;

    private LvlOrganizer currentLvl = null;

    private List<LvlBtnUI> allBtns = new();

    private void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        GenerateLvlSelect();
    }
    public void GenerateLvlSelect()
    {
        if (!canvas || !btnPrefab || !lvlScroll || !contents || !scrollbar || !sltLvl || !unsltLvl || !titleText || !descriptionText || !objectiveText)
        {
            Debug.LogWarning("LvlSelectManager is missing references (Canvas, BtnPrefab, LvlScroll, Contents, Scrollbar, SelectedLevel, UnselectedLevel, TitleText, DescriptionText, ObjectiveText).", this);
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
        Vector2 corner = new Vector2(width*0.5f, height*0.5f);
        foreach (var btn in allBtns)
        {
            Vector2 pos = btn.GetComponent<RectTransform>().anchoredPosition;
            min = Vector2.Min(min, pos - corner);
            max = Vector2.Max(max, pos + corner);
        }
        min -= new Vector2(contents.GetComponent<HorizontalLayoutGroup>().padding.left, 0);
        max += new Vector2(contents.GetComponent<HorizontalLayoutGroup>().padding.right, 0);
        contents.GetComponent<RectTransform>().sizeDelta = max - min;
        scrollbar.GetComponent<Scrollbar>().numberOfSteps = allLvls.Count;
    }

    public void SelectedLvl(LvlOrganizer lvl)
    {
        //implement lock/unlock logic here

        if (!sltLvl.gameObject.activeSelf || unsltLvl.gameObject.activeSelf)
        {
            sltLvl.gameObject.SetActive(true);
            unsltLvl.gameObject.SetActive(false);
        }
        currentLvl = lvl;
        titleText.text = string.IsNullOrWhiteSpace(currentLvl.lvlTitle) ? currentLvl.sceneName : currentLvl.lvlTitle;
        descriptionText.text = currentLvl.lvlDescription;
        string objList = "";
        if (currentLvl.objectiveText == null || currentLvl.objectiveText.Count == 0)
        {
            objList = "    - Objectives Unknown. Good Luck Soldier.";
        } else
        {
            foreach (string s in currentLvl.objectiveText)
            {
                objList += "    - " + s + "\n";
            }
        }
        objectiveText.text = objList;
    }

    public void OpenLvlSelect()
    {
        canvas.gameObject.SetActive(true);
        sltLvl.gameObject.SetActive(false);
        unsltLvl.gameObject.SetActive(true);
        lvlScroll.GetComponent<ScrollRect>().horizontalNormalizedPosition = 0; //set to the leftmost position
    }

    public void CloseLvlSelect()
    {
        sltLvl.gameObject.SetActive(false);
        unsltLvl.gameObject.SetActive(true);
        canvas.gameObject.SetActive(false);
        lvlScroll.GetComponent<ScrollRect>().horizontalNormalizedPosition = 0; //set to the leftmost position
    }

    public void PlayLvl()
    {
        // Show loadout canvas, then load correct scene when player clicks the button
        inventoryManager.InitializeInventory(loadoutManager.GetCurrentLoadout());
        SceneManager.LoadScene(currentLvl.sceneName);
    }
}
