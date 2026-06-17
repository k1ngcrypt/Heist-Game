using UnityEngine;
using TMPro;

public class TimerUI : MonoBehaviour, ITurnActor
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private RectTransform image;
    [SerializeField] private RectTransform timerArea;
    [SerializeField] private int turnsLeft = 120;
    [SerializeField] private TurnManager turnManager;

    private float paddingX = 20f;

    public int TickDebt { get; set; }

    public static TimerUI Instance { get; private set;}

    public void Start()
    {
        Instance = this;
        UpdateTimerUI();
    }

    public void OnValidate() {
        if (turnManager == null) turnManager = TurnManager.Instance;
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
    }

    public void OnEnable()
    {
        turnManager.Register(this);
        timerText.text = turnsLeft+"";
        Resize();
    }

    public async Awaitable OnTick()
    {
        TickDebt = 0;
        if (SettingManager.Instance.godMode)
        {
            timerText.text = "∞";
        } else
        {
            turnsLeft--;
            timerText.text = turnsLeft+"";
            if (turnsLeft == 0)
            {
                SceneUIManager.Instance.MissionFailed("Ran Out of Moves");
            }
        }
        Resize();
    }

    public void OnDestroy()
    {
        Instance = null;
    }

    public void UpdateTimerUI()
    {
        if (SettingManager.Instance.godMode)
        {
            timerText.text = "∞";
        } else
        {
            timerText.text = turnsLeft+"";
        }
        Resize();
    }

    public void Resize()
    {
        float pref = timerText.preferredWidth;
        if (pref < 53.59) //standard 4 character size
        {
            pref = 53.59f;
        }
        timerText.GetComponent<RectTransform>().sizeDelta = new Vector2(pref, timerText.preferredHeight);
        timerArea.sizeDelta = new Vector2(image.sizeDelta.x + pref + paddingX, timerArea.sizeDelta.y);
    }
}
