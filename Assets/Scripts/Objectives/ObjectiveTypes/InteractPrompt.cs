using HeistGame.Objectives;
using System.Collections.Generic;
using UnityEngine;

namespace HeistGame.Interactions
{
    public class InteractPrompt : MonoBehaviour, IInteractionContributor
    {
        private InteractionStateManager stateManager;

        [SerializeField] private string InteractText;
        private ObjectiveTrigger objectiveTrigger;

        private void Awake()
        {
            FetchDependencies();
        }

        private void FetchDependencies()
        {
            stateManager = GetComponent<InteractionStateManager>();
            objectiveTrigger = GetComponent<ObjectiveTrigger>();
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

        private async void UpdateObj() {
            if (objectiveTrigger != null) if (!objectiveTrigger.TriggerProgress()) return;
            Destroy(gameObject);
        }
    }
}