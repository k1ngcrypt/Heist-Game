using UnityEngine;
using System.Collections.Generic;

namespace HeistGame.Interactions {
    [RequireComponent(typeof(InteractArea))]
    public class InteractionStateManager : MonoBehaviour {
        private InteractArea masterArea;

        private void Awake() {
            masterArea = GetComponent<InteractArea>();
        }

        private void Start() { RebuildActiveMenu(); }
        public void RebuildActiveMenu(bool onlyIfVisible = false) {
            masterArea.ClearAllButtons();
            IInteractionContributor[] contributors = GetComponents<IInteractionContributor>();

            foreach (var contributor in contributors) {
                masterArea.RegisterButtons(contributor.GetContextButtons());
            }
            masterArea.RefreshActiveOverlayUI(onlyIfVisible);
        }
    }

    public interface IInteractionContributor{
        List<InteractBtnTemplate> GetContextButtons();
    }
}