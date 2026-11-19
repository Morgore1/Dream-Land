using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class OOBPartyMemberDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IDropHandler, IEndDragHandler
{
    private PartyScreenOOB partyScreen;
    private CanvasGroup canvasGroup;
    private int slotIndex;
    private float originalAlpha = 1f;
    private bool isDragging;

    public void Initialize(PartyScreenOOB screen, int index)
    {
        partyScreen = screen;
        slotIndex = index;

        Graphic slotGraphic = GetComponent<Graphic>();
        if (slotGraphic == null)
        {
            Image hitArea = gameObject.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            hitArea.raycastTarget = true;
        }
        else
        {
            slotGraphic.raycastTarget = true;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalAlpha = canvasGroup.alpha;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (partyScreen == null || !partyScreen.CanReorderMembers || partyScreen.IsDetailsOpen
            || !partyScreen.HasMonsterInSlot(slotIndex))
            return;

        isDragging = true;
        canvasGroup.alpha = originalAlpha * 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // The EventSystem drives this handler while the pointer is held.
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!partyScreen.CanReorderMembers || partyScreen.IsDetailsOpen)
            return;

        OOBPartyMemberDragHandler source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<OOBPartyMemberDragHandler>()
            : null;

        if (source == null || source.partyScreen != partyScreen || !source.isDragging)
            return;

        partyScreen.TrySwapMembers(source.slotIndex, slotIndex);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        RestoreDragVisual();
    }

    private void OnDisable()
    {
        RestoreDragVisual();
    }

    private void RestoreDragVisual()
    {
        isDragging = false;
        if (canvasGroup != null)
            canvasGroup.alpha = originalAlpha;
    }
}
