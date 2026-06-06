using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class GuardIcon : MonoBehaviour
{
    [Header("Icon Textures")]
    [SerializeField] private Sprite questionMarkSprite;
    [SerializeField] private Sprite exclamationMarkSprite;

    private SpriteRenderer spriteRenderer => GetComponent<SpriteRenderer>();

    public void TriggerSuspicious() => ShowIcon(questionMarkSprite);
    public void TriggerAlerted() => ShowIcon(exclamationMarkSprite);

    private void ShowIcon(Sprite nextSprite)
    {
        spriteRenderer.sprite = nextSprite;
    }

    public void HideIcon()
    {
        spriteRenderer.sprite = null;
    }
}