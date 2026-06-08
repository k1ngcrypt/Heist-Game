using UnityEngine;
using TMPro;

public class TimerUI : MonoBehaviour, ITurnActor
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private int turnsLeft = 120;
    [SerializeField] private TurnManager turnManager;

    public int TickDebt { get; set; }

    public void OnValidate() {
        if (turnManager == null) turnManager = TurnManager.Instance;
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
    }

    public void OnEnable()
    {
        turnManager.Register(this);
        timerText.text = turnsLeft+"";
    }

    public async Awaitable OnTick()
    {
        TickDebt = 0;
        turnsLeft--;
        timerText.text = turnsLeft+"";
        if (turnsLeft == 0)
        {
            SceneUIManager.Instance.MissionFailed("Ran Out of Moves");
        }
    }
}
