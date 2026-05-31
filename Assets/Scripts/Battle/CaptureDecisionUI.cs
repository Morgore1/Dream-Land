using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CaptureDecisionUI : MonoBehaviour
{
    [SerializeField] TMP_Text monsterNameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Button addButton;
    [SerializeField] TMP_Text addButtonLabel;
    [SerializeField] Button combineButton;
    [SerializeField] TMP_Text combineButtonLabel;
    [SerializeField] Button discardButton;
    [SerializeField] TMP_Text discardButtonLabel;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color highlightColor = Color.yellow;

    private Action addAction;
    private Action combineAction;
    private Action discardAction;
    private int selectedIndex;
    private bool canCombine;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (addButton != null)
            addButton.onClick.AddListener(OnAddClicked);

        if (combineButton != null)
            combineButton.onClick.AddListener(OnCombineClicked);

        if (discardButton != null)
            discardButton.onClick.AddListener(OnDiscardClicked);

        Close();
    }

    public void Show(Monster monster, bool canCombine, Action onAdd, Action onCombine, Action onDiscard)
    {
        if (monsterNameText != null)
            monsterNameText.text = monster.Base.Name;

        if (descriptionText != null)
            descriptionText.text = $"What should you do with {monster.Base.Name}?";

        addButton.interactable = true;
        combineButton.interactable = canCombine;
        discardButton.interactable = true;

        this.canCombine = canCombine;
        addAction = onAdd;
        combineAction = onCombine;
        discardAction = onDiscard;

        gameObject.SetActive(true);
        IsOpen = true;

        selectedIndex = 0;
        SelectFirstAvailableOption();
        UpdateSelectionVisual();
    }

    public void HandleInput()
    {
        if (!IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelection(-1);
            UpdateSelectionVisual();
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelection(1);
            UpdateSelectionVisual();
        }

        if (Input.GetKeyDown(KeyCode.Z))
            ActivateSelection();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        IsOpen = false;
    }

    void OnAddClicked()
    {
        addAction?.Invoke();
    }

    void OnCombineClicked()
    {
        combineAction?.Invoke();
    }

    void OnDiscardClicked()
    {
        discardAction?.Invoke();
    }

    void MoveSelection(int direction)
    {
        int startIndex = selectedIndex;
        do
        {
            selectedIndex = (selectedIndex + direction + 3) % 3;
        }
        while (!IsOptionInteractable(selectedIndex) && selectedIndex != startIndex);
    }

    void ActivateSelection()
    {
        if (selectedIndex == 0)
            addAction?.Invoke();
        else if (selectedIndex == 1)
            combineAction?.Invoke();
        else if (selectedIndex == 2)
            discardAction?.Invoke();
    }

    bool IsOptionInteractable(int index)
    {
        if (index == 0)
            return addButton != null && addButton.interactable;
        if (index == 1)
            return combineButton != null && combineButton.interactable;
        if (index == 2)
            return discardButton != null && discardButton.interactable;
        return false;
    }

    void SelectFirstAvailableOption()
    {
        selectedIndex = 0;
        if (!IsOptionInteractable(selectedIndex))
            MoveSelection(1);
    }

    void UpdateSelectionVisual()
    {
        if (addButtonLabel != null)
            addButtonLabel.color = (selectedIndex == 0 ? highlightColor : normalColor);

        if (combineButtonLabel != null)
            combineButtonLabel.color = (selectedIndex == 1 ? highlightColor : normalColor);

        if (discardButtonLabel != null)
            discardButtonLabel.color = (selectedIndex == 2 ? highlightColor : normalColor);

        if (addButton != null)
            addButton.interactable = addButton.interactable;
        if (combineButton != null)
            combineButton.interactable = combineButton.interactable;
        if (discardButton != null)
            discardButton.interactable = discardButton.interactable;
    }
}
