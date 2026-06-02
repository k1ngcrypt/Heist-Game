using System.Collections.Generic;
using UnityEngine;
using HeistGame.Objectives;

namespace HeistGame.Interactions {
    public class CollectionInteraction : MonoBehaviour, IInteractionContributor {
        private CollectionObjective collectionObjective;
        private InteractionStateManager stateManager;
 
        [SerializeField] private string nameOfInteracted;
        [SerializeField] private int ticksUsedToCollect = 1;

        private void Awake() {
            FetchDependencies();
        }

        private void FetchDependencies() {
            collectionObjective = GetComponentInParent<CollectionObjective>();
            stateManager = GetComponent<InteractionStateManager>();
        }

        public List<InteractBtnTemplate> GetContextButtons() {
            List<InteractBtnTemplate> myButtons = new List<InteractBtnTemplate>();

            if (collectionObjective != null) {
                string collectText = nameOfInteracted != "" ? $"Collect {nameOfInteracted}" : "Collect Item";
                
                InteractBtnTemplate collectBtn = new InteractBtnTemplate();
                collectBtn.text = collectText;
                collectBtn.onClick.AddListener(Collect);

                myButtons.Add(collectBtn);
            }
            return myButtons;
        }

        private async void Collect() {
            await TurnManager.Instance.ProcessTicks(ticksUsedToCollect);
            collectionObjective.TriggerProgress();
        }
    }
}