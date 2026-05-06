using System.Collections;
using UnityEngine;

namespace HeistGame.Door
{
    public class AutoCloser : MonoBehaviour
    {
        [SerializeField] private float closeDelaySeconds = 3f;

        private DoorController doorController;
        private Coroutine closeRoutine;

        private void Awake()
        {
            doorController = GetComponent<DoorController>();
        }

        public void NotifyDoorOpened()
        {
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
            }

            closeRoutine = StartCoroutine(CloseAfterDelay());
        }

        public void NotifyDoorClosed()
        {
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSeconds(closeDelaySeconds);
            doorController?.TryCloseDoor();
            closeRoutine = null;
        }
    }
}
