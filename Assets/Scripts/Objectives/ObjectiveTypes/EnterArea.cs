using UnityEngine;

namespace HeistGame.Objectives
{
    public class EnterArea : ObjectiveTrigger, ITurnActor
    {
        public int TickDebt { get; set; }

        [Header("Area Entering Objective Settings")]
        [SerializeField] private Vector2 topRight;
        [SerializeField] private Vector2 bottomLeft;
        [SerializeField] private Transform playerPos;
        [SerializeField] private TurnManager turnManager;

        private readonly Vector2 buffer = new(0.5f, 0.5f);

        private void OnEnable() { turnManager.Register(this); }
        private void OnDisable() { turnManager.Unregister(this); }

        public async Awaitable OnTick()
        {
            if (prerequisiteID != -1 && !ObjectiveManager.Instance.IsComplete(prerequisiteID))
            { return; }

            TickDebt = 0;
            Vector3 currentPosition = playerPos.position;
            if (currentPosition.x > bottomLeft.x - buffer.x && currentPosition.x < topRight.x + buffer.x &&
                currentPosition.y > bottomLeft.y - buffer.y && currentPosition.y < topRight.y + buffer.y)
            {
                TriggerProgress();
            }
            return;
        }

        public override void TriggerProgress()
        {
            base.TriggerProgress();

            Debug.Log("Area was entered, objective progress triggered.");
            gameObject.transform.SetParent(null);
            Destroy(gameObject);
        }
    }
}