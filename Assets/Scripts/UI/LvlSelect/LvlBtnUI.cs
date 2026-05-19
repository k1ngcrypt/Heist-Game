using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class LvlBtnUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image iconImage;

    private LvlOrganizer myBtn;
    private LvlSelectManager lvlManager;
    
    public void Initialize(LvlOrganizer btn, LvlSelectManager lvlMgr)
    {
        myBtn = btn;
        lvlManager = lvlMgr;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(myBtn.lvlTitle) ? myBtn.sceneName : myBtn.lvlTitle;
        }

        if (iconImage != null)
        {
            iconImage.sprite = myBtn.lvlIcon;
        }
        GetComponent<Button>().onClick.AddListener(OnPressed);
    }

    public void OnPressed()
    {
        lvlManager.SelectedLvl(myBtn);
    }
}
