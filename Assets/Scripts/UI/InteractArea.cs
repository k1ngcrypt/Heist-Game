using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class InteractBtnTemplate
{
    [SerializeField] public string text;
    [SerializeField] public UnityEvent onClick;
}

public class InteractArea : MonoBehaviour
{
    [SerializeField] public string title;
    [SerializeField] private int radius;
    [SerializeField] private InteractionOverlay overlayPrefab;

    [SerializeField] private List<InteractBtnTemplate> interactBtnTemplate;

    private InteractionOverlay currentOverlay;

    private void OnValidate()
    {
        GetComponent<BoxCollider2D>().edgeRadius = radius;
    }

    private void Start()
    {
        GetComponent<Transform>().localPosition = new Vector3(0, 0, 0);
        OnTriggerEnter2D(GetComponent<Collider2D>());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Player entered interact area");
        //if (!collision.gameObject.CompareTag("Player")) return;

        if (currentOverlay != null)
            return;

        currentOverlay = Instantiate(overlayPrefab, this.transform);

        currentOverlay.Initialize(title, interactBtnTemplate, GetComponent<Transform>().position);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log("Player exited interact area");   
        //if (!collision.gameObject.CompareTag("Player")) return;

        if (currentOverlay != null)
        {
            Destroy(currentOverlay.gameObject);
        }
    }

    public void End()
    {
        OnTriggerExit2D(GetComponent<Collider2D>());
    }
}
