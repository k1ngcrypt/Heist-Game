using UnityEngine;
using UnityEngine.UI;

public class MainMenuBackdropMove : MonoBehaviour
{
    [SerializeField] private RectTransform imageRt;
    [SerializeField] private RectTransform baseFitImage;
    [SerializeField] private RectTransform canvasRt;

    [SerializeField] private float timeToEndInSec = 100f;
    private float distance;
    private float startPos;
    private float endPos;
    private float lastWidth;
    private float lastHeight;

    void Start()
    {
        UpdateMovement();
        imageRt.anchoredPosition = new Vector2(startPos, imageRt.anchoredPosition.y);
        //imageRt.anchoredPosition = new Vector2(endPos+100f, imageRt.anchoredPosition.y);
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            Debug.Log("New Dimensions Adjusted to: "+lastWidth+" x "+lastHeight);

            UpdateMovement();
        }

        if (imageRt.anchoredPosition.x <= endPos)
        {
            imageRt.anchoredPosition = new Vector2(startPos, imageRt.anchoredPosition.y); 
            //in coding, it might seem like a very harsh movement, but visually, i made it line up perfectly, so you don't notice the transition. test for yourself
        } else
        {
            imageRt.anchoredPosition -= new Vector2(Time.deltaTime * distance / timeToEndInSec, 0); //moves from left to right
        }
    }

    private void UpdateMovement()
    {
        imageRt.sizeDelta = new Vector2(baseFitImage.sizeDelta.x+canvasRt.sizeDelta.x, imageRt.sizeDelta.y);
        distance = imageRt.sizeDelta.x - canvasRt.sizeDelta.x;
        startPos = distance/2f;
        endPos = -distance/2f;
    }
}
