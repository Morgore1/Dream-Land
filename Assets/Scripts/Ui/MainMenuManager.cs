using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "MainGame";
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject tutorialCanvas;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private float lettersPerSecond = 24f;
    [SerializeField] private string tutorialPrompt = "Do you want to play a quick tutorial?";

    private Coroutine typingCoroutine;

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenMultiplayerMenu()
    {
        Debug.Log("Multiplayer button pressed");
    }

    public void OpenSinglePlayerTutorialPrompt()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(true);
            mainMenuCanvas.SetActive(false);
        }

        if (tutorialText != null)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            typingCoroutine = StartCoroutine(TypeTutorialPrompt());
        }
        else
        {
            Debug.LogWarning("Tutorial text reference is not assigned.");
        }
    }

    public void StartSinglePlayerGame()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
            mainMenuCanvas.SetActive(true);
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OnTutorialYes()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
            mainMenuCanvas.SetActive(true);
        }
        Debug.Log("Starting tutorial...");
    }

    private IEnumerator TypeTutorialPrompt()
    {
        tutorialText.text = string.Empty;

        foreach (char letter in tutorialPrompt.ToCharArray())
        {
            tutorialText.text += letter;
            yield return new WaitForSeconds(1f / lettersPerSecond);
        }

        typingCoroutine = null;
    }
}
