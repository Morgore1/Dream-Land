using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("UI References")]
    public GameObject pauseMenuUI;       // Assign Panel in Inspector
    public Button[] buttons;             // Assign TMP buttons (Inventory, Party, Save, Quit)
    [SerializeField] PartyScreenOOB partyScreenOOB;
    public GameObject PartyScreenUI;
    [SerializeField] private GameObject inventoryCanvas; // your canvas
    [SerializeField] private InventoryUI inventoryUI;    // assign the panel with InventoryUI script

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    private TextMeshProUGUI[] texts;     // Cached TMP text components
    private int selectedIndex = 0;

    private MonsterParty playerParty;
    private int currentMember;
    private bool inPartyScreen = false;

    private HealingItem pendingHealingItem;   // The healing item waiting to be used
    private Inventory currentInventory;       // The inventory it came from
    // -------- Initialization --------
    void Start()
    {
        texts = new TextMeshProUGUI[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
            texts[i] = buttons[i].GetComponentInChildren<TextMeshProUGUI>();

        partyScreenOOB.init();
    }

    public void Init(MonsterParty playerParty)
    {
        this.playerParty = playerParty;
    }

    // -------- Update Loop --------
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inPartyScreen)
            {
                partyScreenOOB.gameObject.SetActive(false);
                PartyScreenUI.SetActive(false);

                // Resume game completely
                Time.timeScale = 1f;
                GameIsPaused = false;
                inPartyScreen = false;

                return;
            }

            var battle = FindObjectOfType<BattleSystem>();
            if (battle != null && battle.isActiveAndEnabled)
                return;

            if (GameIsPaused)
                Resume();
            else
                Pause();

            return;
        }

        if (!GameIsPaused || (!pauseMenuUI.activeSelf && !inventoryCanvas.activeSelf && !PartyScreenUI.activeSelf))
            return;

        if (inventoryCanvas.activeSelf) return;

        if (inPartyScreen)
        {
            HandlePartySelection();
            return;
        }

        // Pause menu navigation
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex - 1 + buttons.Length) % buttons.Length;
            HighlightText();
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % buttons.Length;
            HighlightText();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            DoSelectedAction();
        }
    }

    // -------- Pause Handling --------
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        inventoryCanvas.SetActive(false); // just in case
        Time.timeScale = 1f;
        GameIsPaused = false;
        inPartyScreen = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;

        selectedIndex = 0;
        HighlightText();

        // Ensure playerParty is set
        if (playerParty == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                Init(player.GetComponent<MonsterParty>());
            else
                Debug.LogError("PauseMenu: Could not find Player with MonsterParty!");
        }
    }

    void HighlightText()
    {
        for (int i = 0; i < texts.Length; i++)
            texts[i].color = (i == selectedIndex) ? highlightColor : normalColor;
    }

    void DoSelectedAction()
    {
        switch (selectedIndex)
        {
            case 0: OpenInventory(); break;
            case 1: OpenParty(); break;
            case 2: SaveGame(); break;
            case 3: QuitGame(); break;
        }
    }

    // -------- Party Navigation --------
    void HandlePartySelection()
    {
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            currentMember += 2;
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            currentMember -= 2;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMember;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMember;

        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Monsters.Count - 1);
        partyScreenOOB.UpdateMemberSelection(currentMember);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            Monster chosen = playerParty.Monsters[currentMember];

            if (pendingHealingItem != null)
            {

                bool healed = pendingHealingItem.HealMonster(chosen);

                if (healed)
                {
                    // Consume the item properly (decrement slot count or remove slot)
                    var slot = currentInventory.slots.Find(s => s.item == pendingHealingItem);
                    if (slot != null)
                    {
                        slot.count--;
                        if (slot.count <= 0)
                            currentInventory.slots.Remove(slot);
                    }

                    partyScreenOOB.SetMessageText($"{chosen.Base.Name} was healed!");
                }
                else
                {
                    partyScreenOOB.SetMessageText($"{chosen.Base.Name} is already at full HP!");
                }

                // Reset context
                pendingHealingItem = null;
                currentInventory = null;

                // Close party screen back to Inventory
                partyScreenOOB.gameObject.SetActive(false);
                PartyScreenUI.SetActive(false);
                inventoryCanvas.SetActive(true);
                inventoryUI.RefreshUI();

                inPartyScreen = false;
            }
            else
            {
                Debug.Log("Selected monster (normal party view): " + chosen.Base.Name);
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            // Exit party screen
            partyScreenOOB.gameObject.SetActive(false);
            PartyScreenUI.SetActive(false);
            pauseMenuUI.SetActive(true);
            inPartyScreen = false;
            HighlightText();
        }
    }
    public void ReturnFromInventory()
    {
        inventoryCanvas.SetActive(false);
        pauseMenuUI.SetActive(true);
        HighlightText();

        // Game is paused ONLY if pause menu is actually visible
        GameIsPaused = pauseMenuUI.activeSelf;
    }

    public void OpenPartyForHealing(HealingItem item, Inventory sourceInventory)
    {
        pendingHealingItem = item;
        currentInventory = sourceInventory;

        // Disable other menus
        pauseMenuUI.SetActive(false);
        inventoryCanvas.SetActive(false);

        // Enable party screen
        PartyScreenUI.SetActive(true);
        partyScreenOOB.SetPartyData(playerParty.Monsters);
        partyScreenOOB.gameObject.SetActive(true);

        currentMember = 0;
        partyScreenOOB.UpdateMemberSelection(currentMember);
        inPartyScreen = true;
    }

    // -------- Actions --------
    void OpenInventory()
    {
        pauseMenuUI.SetActive(false);
        inventoryCanvas.SetActive(true);   // enable the canvas
        inventoryUI.RefreshUI();             // refresh UI safely
        GameIsPaused = true;
    }


    void OpenParty()
    {
        if (playerParty == null)
        {
            Debug.LogError("PauseMenu: No MonsterParty set!");
            return;
        }


        pauseMenuUI.SetActive(false);
        PartyScreenUI.SetActive(true);
        partyScreenOOB.SetPartyData(playerParty.Monsters);
        partyScreenOOB.gameObject.SetActive(true);

        currentMember = 0;
        partyScreenOOB.UpdateMemberSelection(currentMember);
        inPartyScreen = true;
    }

    void SaveGame()
    {
        Debug.Log("Game saved!");
    }

    void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("Quit to main menu!");
        SceneManager.LoadScene("MainMenu");
    }
}