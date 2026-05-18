using UnityEngine;

namespace HeistGame.Objectives {
    public class EnterArea : ObjectiveTrigger {
        [Header("Vault Special Settings")]
        [SerializeField] private Sprite openedVaultSprite;
        private bool isTriggered = false;

        public override void TriggerProgress() {
            if (isTriggered) return;

            base.TriggerProgress();

            isTriggered = true;
            
            if (TryGetComponent<SpriteRenderer>(out var renderer)) renderer.sprite = openedVaultSprite;

            Debug.Log("The vault sealing mechanism has been disengaged!");
        }
    }
}