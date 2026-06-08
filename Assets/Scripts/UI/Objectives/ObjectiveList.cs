using UnityEngine;
using HeistGame.Objectives;
using UnityEngine.UI;
using System.Collections.Generic;
public class ObjectiveList : MonoBehaviour {
    [SerializeField] private Transform containerParent;
    [SerializeField] private ObjectiveItem objectiveItemPrefab;
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private Transform titleArea;
    
    private bool isOpen = true;
    private float pBottom = 0;
    private RectTransform rect;

    private void OnEnable() { 
        ObjectiveManager.OnObjectivesChanged += RefreshObjectiveList;
        pBottom = containerParent.GetComponent<VerticalLayoutGroup>().padding.bottom;
        rect = containerParent.GetComponent<RectTransform>(); 
    }
    private void OnDisable(){ ObjectiveManager.OnObjectivesChanged -= RefreshObjectiveList; }
    private void Start() { RefreshObjectiveList(); }

    private void RefreshObjectiveList() {
        if (objectiveManager == null || containerParent == null || objectiveItemPrefab == null || titleArea == null || !isOpen) return;

        // 1. Wipe old text UI elements cleanly
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in containerParent) {
            if (child != titleArea) toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy) {
            DestroyImmediate(go);
        }

        // 2. Ask the manager for ONLY the currently visible objectives
        var visibleObjectives = objectiveManager.GetVisibleObjectives();

        // 3. Populate the UI layout with updated rows
        foreach (var objective in visibleObjectives) {
            ObjectiveItem newItem = Instantiate(objectiveItemPrefab, containerParent);
            
            newItem.Initialize(objective.Data.title, objective.CurrentAmount,
            objective.Data.requiredAmount, objective.IsCompleted, objective.IsFailed);
        }

        ResizeArea();
    }

    public void ToggleObjectivesVisibility()
    {
        isOpen = !isOpen;

        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in containerParent) {
            if (child != titleArea) toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy) {
            DestroyImmediate(go);
        }

        if (isOpen)
        {
            var visibleObjectives = objectiveManager.GetVisibleObjectives();
            foreach (var objective in visibleObjectives) {
                ObjectiveItem newItem = Instantiate(objectiveItemPrefab, containerParent);
                newItem.Initialize(objective.Data.title, objective.CurrentAmount,
                    objective.Data.requiredAmount, objective.IsCompleted, objective.IsFailed);
            }
        }

        ResizeArea();
    }

    private void ResizeArea()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        float lowestY = 0f;
        float ySize = 0f;

        foreach (Transform child in containerParent)
        {
            lowestY = child.GetComponent<RectTransform>().anchoredPosition.y;
            ySize = child.GetComponent<RectTransform>().sizeDelta.y;
        }

        rect.sizeDelta = new Vector2(rect.sizeDelta.x, (-lowestY) + (ySize / 2f) + pBottom);
    }
}