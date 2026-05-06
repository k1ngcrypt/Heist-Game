using UnityEngine;

namespace HeistGame.Door
{
    public class AutoCloser : MonoBehaviour, ITurnActor
    {
        [SerializeField, Min(0)] private int closeDelayTicks = 3;
        [SerializeField] TurnManager turnManager;

        private DoorController doorController;
        private int waitTicks = 0;
        public int TickDebt { get; set; }

        private void Awake()
        {
            doorController = GetComponent<DoorController>();
        }

        public void NotifyDoorOpened()
        {
            waitTicks = closeDelayTicks;
        }

        public void NotifyDoorClosed()
        {
            waitTicks = 0;
        }

        public Awaitable OnTick()
        {
            if (waitTicks > 0)
            {
                waitTicks--;
                TickDebt = 0;
                if (waitTicks == 0)
                {
                    doorController.TryCloseDoor();
                }
            }
            return default;
        }
    }
}
