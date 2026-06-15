using HeistGame.Objectives;
using System.Collections.Generic;
using UnityEngine;

namespace HeistGame.Interactions
{
    public class InteractPrompt : MonoBehaviour, IInteractionContributor
    {
        private InteractionStateManager stateManager;

        [SerializeField] private string nameOfInteract;
        [SerializeField] private string InteractText;
        [SerializeField] protected int targetObjectiveID;

        private void Awake()
        {
            FetchDependencies();
        }

        private void FetchDependencies()
        {
            stateManager = GetComponent<InteractionStateManager>();
        }

        public List<InteractBtnTemplate> GetContextButtons()
        {
            List<InteractBtnTemplate> myButtons = new();

            InteractBtnTemplate openBtn = new InteractBtnTemplate();
            openBtn.text = InteractText;
            openBtn.onClick.AddListener(UpdateObj);
            myButtons.Add(openBtn);
            return myButtons;
        }

        private async void UpdateObj()
        {
            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.UpdateObjectiveProgress(targetObjectiveID, 1);
            Destroy(gameObject);
        }
    }
}