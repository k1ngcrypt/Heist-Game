using System.Collections.Generic;
using UnityEngine;
using HeistGame.Door;

namespace HeistGame.Interactions {
    public class TransportInteraction : MonoBehaviour, IInteractionContributor {
        private DoorController door;
        private IDoorOpenBehavior openBehavior;
        private InteractionStateManager stateManager;
 
        [SerializeField] private string nameOfTransport;
        [SerializeField] private int ticksUsedToTransport = 1;

        private void Awake() {
            FetchDependencies();
        }

        private void FetchDependencies() {
            door = GetComponentInParent<DoorController>();
            openBehavior = door.GetComponent<IDoorOpenBehavior>();
            stateManager = GetComponent<InteractionStateManager>();
        }

        public List<InteractBtnTemplate> GetContextButtons() {
            List<InteractBtnTemplate> myButtons = new List<InteractBtnTemplate>();

            if (openBehavior != null) {
                string openText = nameOfTransport != "" ? $"Use {nameOfTransport}" : "Use Transport";
                
                InteractBtnTemplate openBtn = new InteractBtnTemplate();
                openBtn.text = openText;
                openBtn.onClick.AddListener(Transport);
                myButtons.Add(openBtn);
            }
            return myButtons;
        }

        private async void Transport() {
            bool success;
            success = door.TryOpenDoor();
            if (success) {
                stateManager.RebuildActiveMenu();
                await TurnManager.Instance.ProcessTicks(ticksUsedToTransport);
            } else {
                if (nameOfTransport == "Stair") {
                    openBehavior.OpenDoor();
                    stateManager.RebuildActiveMenu();
                    await TurnManager.Instance.ProcessTicks(ticksUsedToTransport);
                }
                else Debug.LogWarning($"Failed to transport using {nameOfTransport}. Check if it's locked or destroyed.");
            }
            await Awaitable.EndOfFrameAsync(); 
        }
    }
}