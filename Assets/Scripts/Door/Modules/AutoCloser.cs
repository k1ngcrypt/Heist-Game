using UnityEngine;

namespace HeistGame.Door
{
    [RequireComponent(typeof(DoorController))]
    public class AutoCloser : MonoBehaviour, ITurnActor
    {
        [SerializeField, Min(0)] private int closeDelayTicks = 3;
        [SerializeField] TurnManager turnManager;

        private DoorController doorController;
        public int TickDebt { get; set; }

        private void Awake()
        {
            doorController = GetComponent<DoorController>();
        }

        private void OnEnable() {
            if (turnManager == null) turnManager = TurnManager.Instance;
            if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>().GetComponent<TurnManager>();
        }

        public void NotifyDoorOpened()
        {
            TickDebt = -closeDelayTicks;
            turnManager.Register(this);
            
        }

        public void NotifyDoorClosed()
        {
            turnManager.Unregister(this);
            TickDebt = 0;
        }

        public async Awaitable OnTick()
        {
            TickDebt = 0;
            doorController.TryCloseDoor();
            turnManager.Unregister(this);
            return;
        }
    }
}
