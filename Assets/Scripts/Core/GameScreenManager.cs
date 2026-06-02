using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Controls Start, Victory, Defeat, and Pause panels.
// Freezes time (timeScale = 0) on every screen except active gameplay.
public class GameScreenManager : MonoBehaviour
{
    public static GameScreenManager Instance { get; private set; }

    // Survives scene reloads so restart skips the start panel.
    private static bool _skipStartOnLoad;

    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private GameObject pausePanel;

    private bool _gameActive;
    private bool _isPaused;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameWon.AddListener(ShowVictoryScreen);

        WirePauseButtons();
        SetPanel(pausePanel, false);

        if (_skipStartOnLoad)
        {
            _skipStartOnLoad = false;
            SetPanel(startPanel,   false);
            SetPanel(victoryPanel, false);
            SetPanel(defeatPanel,  false);
            SetPlayerControls(true);
            _gameActive    = true;
            Time.timeScale = 1f;
            AudioManager.Instance?.PlayWorldMusic();
        }
        else
        {
            SetPanel(startPanel,   true);
            SetPanel(victoryPanel, false);
            SetPanel(defeatPanel,  false);
            SetPlayerControls(false);
            _gameActive    = false;
            Time.timeScale = 0f;
            AudioManager.Instance?.PlayTitleMusic();
        }
    }

    // Wires Resume and Restart buttons inside the pause panel by name.
    private void WirePauseButtons()
    {
        if (pausePanel == null) return;

        foreach (Button btn in pausePanel.GetComponentsInChildren<Button>(true))
        {
            switch (btn.gameObject.name)
            {
                case "ResumeButton":
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ResumeGame);
                    break;
                case "PauseRestartButton":
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(RestartGame);
                    break;
            }
        }
    }

    private void Update()
    {
        if (!_gameActive) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameWon.RemoveListener(ShowVictoryScreen);

        Time.timeScale = 1f;
    }

    // ── Panel control ─────────────────────────────────────────────────────────

    private void ShowVictoryScreen()
    {
        _gameActive    = false;
        _isPaused      = false;
        SetPanel(pausePanel,   false);
        SetPanel(victoryPanel, true);
        SetPanel(defeatPanel,  false);
        SetPlayerControls(false);
        Time.timeScale = 0f;
        AudioManager.Instance?.PlayVictoryMusic();
    }

    public void ShowDefeatScreen()
    {
        _gameActive    = false;
        _isPaused      = false;
        SetPanel(pausePanel,   false);
        SetPanel(defeatPanel,  true);
        SetPanel(victoryPanel, false);
        SetPlayerControls(false);
        Time.timeScale = 0f;
        AudioManager.Instance?.PlayDefeatMusic();
    }

    // ── Pause ─────────────────────────────────────────────────────────────────

    public void TogglePause()
    {
        if (_isPaused) ResumeGame();
        else           PauseGame();
    }

    public void PauseGame()
    {
        if (!_gameActive || _isPaused) return;
        _isPaused      = true;
        Time.timeScale = 0f;
        SetPanel(pausePanel, true);
        SetPlayerControls(false);
    }

    public void ResumeGame()
    {
        if (!_isPaused) return;
        _isPaused      = false;
        Time.timeScale = 1f;
        SetPanel(pausePanel, false);
        SetPlayerControls(true);
    }

    // ── Button callbacks ──────────────────────────────────────────────────────

    public void StartGame()
    {
        SetPanel(startPanel, false);
        SetPlayerControls(true);
        _gameActive    = true;
        Time.timeScale = 1f;
        AudioManager.Instance?.PlayWorldMusic();
    }

    public void RestartGame()
    {
        Time.timeScale   = 1f;
        _skipStartOnLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private static void SetPlayerControls(bool enabled)
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) return;

        if (playerObj.TryGetComponent(out PlayerMovement movement)) movement.enabled = enabled;
        if (playerObj.TryGetComponent(out PlayerAttack attack))     attack.enabled   = enabled;
        if (playerObj.TryGetComponent(out PlayerInput playerInput)) playerInput.enabled = enabled;
    }
}
