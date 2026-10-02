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

    private void SetupEggButtons()
    {
        foreach (var egg in eggOptions)
        {
            if (egg.button == null)
                continue;

            egg.button.onClick.RemoveAllListeners();

            if (egg.buttonLabel == null)
                egg.buttonLabel = egg.button.GetComponentInChildren<TextMeshProUGUI>();

            if (egg.buttonLabel != null)
                egg.buttonLabel.text = egg.eggName;

            egg.button.onClick.AddListener(() => HatchEgg(egg));
        }
    }

    public void OpenSelection()
    {
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
        if (egg == null || egg.possibleMonsters == null || egg.possibleMonsters.Count == 0)
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

        MonsterBase selectedMonsterBase = egg.possibleMonsters[Random.Range(0, egg.possibleMonsters.Count)];
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
