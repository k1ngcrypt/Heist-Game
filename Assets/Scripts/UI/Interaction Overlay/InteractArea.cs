using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class InteractBtnTemplate
{
    [SerializeField] public string text;
    [SerializeField] public UnityEvent onClick = new UnityEvent();
}

public class InteractArea : MonoBehaviour, ITurnActor
{
    [Header("Overlay Settings")]
    [SerializeField] private string title;
    [SerializeField] private int radius;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private Transform player;
    [SerializeField] private InteractionOverlay overlayPrefab;
    
    [Header("Item Info")]
    [SerializeField] private List<InteractBtnTemplate> buttons;
    
    
    public int TickDebt { get; set; }
    private InteractionOverlay currentOverlay;
    private Vector3 worldPosition;
    private Vector3 topRight;
    private Vector3 bottomLeft;
    private Vector3 buffer = new Vector3(0.25f, 0.25f, 0); //incase to make sure it will still call player

    public void OnEnable()
    {
        
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject obj = new GameObject("EventSystem");

            obj.AddComponent<EventSystem>();
            obj.AddComponent<InputSystemUIInputModule>();
        }
        turnManager.Register(this);
        worldPosition = this.transform.position;
        topRight = worldPosition + new Vector3(radius, radius, 0) + buffer;
        bottomLeft = worldPosition - new Vector3(radius, radius, 0) - buffer;
        
    }
    public void OnDisable() {
        turnManager.Unregister(this);
    }

    public async Awaitable OnTick()
    {
        TickDebt = 0;
        Vector3 playerPosition = player.position;
        if (playerPosition.x <= topRight.x && playerPosition.x >= bottomLeft.x && playerPosition.y <= topRight.y && playerPosition.y >= bottomLeft.y)
        {
            //in area
            if (currentOverlay != null) {
                //already active, update
                currentOverlay.UpdateButtons();
            } else
            {
                //not active, create
                currentOverlay = Instantiate(overlayPrefab, this.transform);
                currentOverlay.Initialize(title, buttons, GetComponent<Transform>().position);
            }
        } else if (currentOverlay != null)
        {
            //not in area but overlay is active, so destroy it
            Destroy(currentOverlay.gameObject);
        }
        await Awaitable.EndOfFrameAsync();
    }

    //for interaction scripts to call to update the buttons when something changes
    public void RegisterButtons(List<InteractBtnTemplate> newButtons) { buttons.AddRange(newButtons); }
    public void ClearAllButtons() { buttons.Clear(); }
    public void RefreshActiveOverlayUI() {
        if (currentOverlay == null) return;
        currentOverlay.Initialize(title, buttons, transform.position);
    }
    public List<InteractBtnTemplate> GetActiveButtons() { return buttons; }
}
