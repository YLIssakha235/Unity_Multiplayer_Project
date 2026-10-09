using System;
using System.Collections;
using System.Collections.Generic;
using CleanLaboratory.Gameplay;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameTimer : NetworkBehaviour
{
    // Easy access for other scripts
    public static GameTimer Instance { get; private set; }
    public static bool GameIsOver => Instance != null && Instance.IsGameOver.Value;

    // Fired on EVERY machine after the "TIME'S UP!" animation (used by the scoreboard, FP2-6)
    public static event Action OnGameEnded;

    [Header("Settings")]
    [SerializeField] private float gameDuration = 90f;
    [SerializeField] private string gameSceneName = "Laboratory";

    [Header("UI - Timer")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private RectTransform timerPanel;            // the part that pulses
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.75f, 0.2f);   // orange (≤ 30 s)
    [SerializeField] private Color dangerColor = new Color(1f, 0.3f, 0.25f);    // red (≤ 10 s)

    [Header("UI - Time's up")]
    [SerializeField] private CanvasGroup timeUpPanel;
    [SerializeField] private RectTransform timeUpText;
    [SerializeField] private float timeUpDisplayDuration = 2.5f;

    [Header("Sounds (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tickClip;                   // last 5 seconds
    [SerializeField] private AudioClip endClip;                    // at 00:00

    // Server time when the game ends. -1 = not started. Written ONCE by the server.
    private readonly NetworkVariable<double> endTime = new NetworkVariable<double>(-1);

    // Synced to everyone. Only the server sets it to true.
    public readonly NetworkVariable<bool> IsGameOver = new NetworkVariable<bool>(false);

    private int lastShownSecond = -1;
    private bool gameOverApplied = false;

    // ================= SETUP =================

    private void Awake()
    {
        Instance = this;
        HideTimeUpPanel();
    }

    public override void OnNetworkSpawn()
    {
        IsGameOver.OnValueChanged += OnGameOverChanged;

        if (IsServer)
            NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;

        if (IsGameOver.Value) ApplyGameOverLocally();
    }

    public override void OnNetworkDespawn()
    {
        IsGameOver.OnValueChanged -= OnGameOverChanged;

        if (IsServer && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;

        if (Instance == this) Instance = null;
    }

    // ================= SERVER: start when everyone has loaded =================

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode,
                                      List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != gameSceneName || endTime.Value >= 0) return;

        endTime.Value = NetworkManager.ServerTime.Time + gameDuration;
        Debug.Log($"[GameTimer] Game started: {gameDuration} seconds.");
    }

    // ================= EVERY FRAME =================

    private void Update()
    {
        if (!IsSpawned || gameOverApplied) return;

        // Not started yet → show full duration, no effects
        if (endTime.Value < 0)
        {
            UpdateTimerVisual(gameDuration, false);
            return;
        }

        double remaining = endTime.Value - NetworkManager.ServerTime.Time;
        if (remaining < 0) remaining = 0;

        UpdateTimerVisual(remaining, true);

        // Only the SERVER decides the game is over
        if (IsServer && !IsGameOver.Value && remaining <= 0)
        {
            IsGameOver.Value = true;
            Debug.Log("[GameTimer] Time is up! Game over.");
        }
    }

    // ================= TIMER VISUALS =================

    private void UpdateTimerVisual(double seconds, bool running)
    {
        if (timerText == null) return;

        int s = Mathf.CeilToInt((float)seconds);

        // Text "01:30"
        timerText.text = $"{s / 60:00}:{s % 60:00}";

        // Color: white → orange (30 s) → red (10 s)
        if (s <= 10) timerText.color = dangerColor;
        else if (s <= 30) timerText.color = warningColor;
        else timerText.color = normalColor;

        // Tick sound once per second during the last 5 seconds
        if (running && s != lastShownSecond)
        {
            lastShownSecond = s;
            if (s <= 10 && s > 0) PlaySound(tickClip);
        }

        // Pulse during the last 10 seconds: big at each new second, then shrinks
        if (timerPanel != null)
        {
            float scale = 1f;
            if (running && s <= 10 && s > 0)
            {
                float t = (float)(seconds % 1.0);   // goes 0.99 → 0.00 during each second
                scale = 1f + 0.15f * t * t;
            }
            timerPanel.localScale = Vector3.one * scale;
        }
    }

    // ================= GAME OVER =================

    private void OnGameOverChanged(bool previous, bool current)
    {
        if (current) ApplyGameOverLocally();
    }

    private void ApplyGameOverLocally()
    {
        if (gameOverApplied) return;
        gameOverApplied = true;

        // Freeze MY player
        var player = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovementController>();
            if (movement != null) movement.enabled = false;

            var input = player.GetComponent<PlayerInput>();
            if (input != null) input.enabled = false;

            var actions = player.GetComponent<PlayerAction>();
            if (actions != null) actions.enabled = false;

            var animator = player.GetComponentInChildren<Animator>();
            if (animator != null) animator.SetFloat("Speed", 0f);
        }

        // Timer frozen at 00:00, red, normal size
        if (timerText != null)
        {
            timerText.text = "00:00";
            timerText.color = dangerColor;
        }
        if (timerPanel != null) timerPanel.localScale = Vector3.one;

        // Free the cursor (for the scoreboard)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlaySound(endClip);
        StartCoroutine(TimeUpAnimation());
    }

    // "TIME'S UP!": fade in + pop, wait, fade out, then tell the scoreboard
    private IEnumerator TimeUpAnimation()
    {
        if (timeUpPanel != null)
        {
            // Fade in + pop (0.4 s)
            float duration = 0.4f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                timeUpPanel.alpha = k;
                if (timeUpText != null)
                {
                    float ease = 1f - Mathf.Pow(1f - k, 3);          // ease-out
                    timeUpText.localScale = Vector3.one * Mathf.Lerp(2f, 1f, ease);
                }
                yield return null;
            }
            timeUpPanel.alpha = 1f;
            if (timeUpText != null) timeUpText.localScale = Vector3.one;

            // Stay on screen
            yield return new WaitForSecondsRealtime(timeUpDisplayDuration);

            // Fade out (0.5 s)
            duration = 0.5f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                timeUpPanel.alpha = 1f - t / duration;
                yield return null;
            }
            HideTimeUpPanel();
        }

        // Now the scoreboard can appear
        OnGameEnded?.Invoke();
    }

    // ================= HELPERS =================

    private void HideTimeUpPanel()
    {
        if (timeUpPanel == null) return;
        timeUpPanel.alpha = 0f;
        timeUpPanel.blocksRaycasts = false;   // never blocks clicks
        timeUpPanel.interactable = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}