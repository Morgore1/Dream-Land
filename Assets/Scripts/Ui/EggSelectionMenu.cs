using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EggSelectionMenu : MonoBehaviour
{
    [System.Serializable]
    public class EggOption
    {
        public string eggName;
        [Tooltip("Optional permanent unlock ID. Leave blank for an egg that's always available.")]
        public string requiredUnlockId;
        public Button button;
        public TextMeshProUGUI buttonLabel;
        public List<MonsterBase> possibleMonsters = new List<MonsterBase>();
    }

    [Header("Canvas")]
    [SerializeField] private GameObject canvasRoot;

    [Header("Panels")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("Prompt UI")]
    [SerializeField] private TextMeshProUGUI selectionPromptText;
    [SerializeField] private string selectionPrompt = "Which egg would you like to choose?";
    [SerializeField] private float lettersPerSecond = 24f;

    [Header("Result UI")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Image resultMonsterSprite;
    [SerializeField] private Button continueButton;

    [Header("Party")]
    [SerializeField] private MonsterParty playerParty;

    [Header("Eggs")]
    [SerializeField] private List<EggOption> eggOptions = new List<EggOption>();

    [Header("Settings")]
    [SerializeField] private bool autoOpenOnStart = true;

    private Coroutine selectionPromptTypingCoroutine;
    private Coroutine resultTypingCoroutine;

    private void Start()
    {
        if (playerParty == null)
            playerParty = FindObjectOfType<MonsterParty>();

        SetupEggButtons();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(CloseResult);
        }

        if (autoOpenOnStart)
            OpenSelection();
        else
            CloseAll();
    }

    private void OnEnable()
    {
        RewardPointsProfile.UnlockOwnershipChanged += OnUnlockOwnershipChanged;
    }

    private void OnDisable()
    {
        RewardPointsProfile.UnlockOwnershipChanged -= OnUnlockOwnershipChanged;
    }

    private void SetupEggButtons()
    {
        if (eggOptions == null)
            return;

        foreach (var egg in eggOptions)
        {
            if (egg == null || egg.button == null)
                continue;

            if (egg.buttonLabel == null)
                egg.buttonLabel = egg.button.GetComponentInChildren<TextMeshProUGUI>();

            egg.button.onClick.AddListener(() => HatchEgg(egg));
        }

        RefreshEggOptions();
    }

    public void OpenSelection()
    {
        if (canvasRoot != null && !canvasRoot.activeSelf)
            canvasRoot.SetActive(true);

        RefreshEggOptions();

        if (selectionPanel != null)
            selectionPanel.SetActive(true);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (selectionPromptText != null)
        {
            if (selectionPromptTypingCoroutine != null)
            {
                StopCoroutine(selectionPromptTypingCoroutine);
            }

            selectionPromptTypingCoroutine = StartCoroutine(TypeTextCoroutine(selectionPromptText, selectionPrompt));
        }
    }

    public void CloseResult()
    {
        CloseAll();

        GameObject targetCanvas = canvasRoot != null ? canvasRoot : gameObject;
        if (targetCanvas != null)
            targetCanvas.SetActive(false);
    }

    public void CloseAll()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void HatchEgg(EggOption egg)
    {
        if (egg == null)
            return;

        if (!IsEggUnlocked(egg))
        {
            Debug.LogWarning($"Egg choice '{egg.eggName}' is locked.", this);
            RefreshEggOptions();
            return;
        }

        List<MonsterBase> validCandidates = GetValidCandidates(egg);
        if (validCandidates.Count == 0)
        {
            Debug.LogWarning("Egg is missing possible monsters.");
            return;
        }

        if (playerParty == null)
        {
            playerParty = FindObjectOfType<MonsterParty>();
            if (playerParty == null)
            {
                Debug.LogError("No MonsterParty found for egg hatching.");
                return;
            }
        }

        MonsterBase selectedMonsterBase = validCandidates[Random.Range(0, validCandidates.Count)];
        Monster hatchling = new Monster(selectedMonsterBase);

        if (!playerParty.AddMonsterToParty(hatchling))
        {
            Debug.LogWarning("Player party is full; monster could not be added.");
            if (resultText != null)
                resultText.text = "Your party is full!";
            if (resultPanel != null)
                resultPanel.SetActive(true);
            return;
        }

        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
        {
            if (resultTypingCoroutine != null)
            {
                StopCoroutine(resultTypingCoroutine);
            }

            resultTypingCoroutine = StartCoroutine(TypeTextCoroutine(resultText, $"You hatched a {selectedMonsterBase.Name}!"));
        }

        if (resultMonsterSprite != null)
            resultMonsterSprite.sprite = selectedMonsterBase.FrontSprite;
    }

    private void RefreshEggOptions()
    {
        if (eggOptions == null)
            return;

        foreach (EggOption egg in eggOptions)
        {
            if (egg == null || egg.button == null)
                continue;

            bool unlocked = IsEggUnlocked(egg);
            bool hasCandidates = GetValidCandidates(egg).Count > 0;

            if (egg.buttonLabel != null)
                egg.buttonLabel.text = unlocked ? egg.eggName : $"{egg.eggName} (Locked)";

            egg.button.interactable = unlocked && hasCandidates;
        }
    }

    private static bool IsEggUnlocked(EggOption egg)
    {
        return egg != null
            && (string.IsNullOrWhiteSpace(egg.requiredUnlockId)
                || RewardPointsProfile.IsUnlockOwned(egg.requiredUnlockId));
    }

    private static List<MonsterBase> GetValidCandidates(EggOption egg)
    {
        var candidates = new List<MonsterBase>();
        if (egg == null || egg.possibleMonsters == null)
            return candidates;

        foreach (MonsterBase candidate in egg.possibleMonsters)
        {
            if (candidate != null)
                candidates.Add(candidate);
        }

        return candidates;
    }

    private void OnUnlockOwnershipChanged(string unlockId)
    {
        RefreshEggOptions();
    }

    private IEnumerator TypeTextCoroutine(TextMeshProUGUI textObject, string message)
    {
        if (textObject == null)
            yield break;

        textObject.text = string.Empty;

        foreach (char letter in message.ToCharArray())
        {
            textObject.text += letter;
            yield return new WaitForSeconds(1f / lettersPerSecond);
        }
    }
}
