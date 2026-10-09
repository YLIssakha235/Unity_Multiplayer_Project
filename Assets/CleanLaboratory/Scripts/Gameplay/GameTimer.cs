using System;
using System.Collections.Generic;
using CleanLaboratory.Gameplay;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameTimer : NetworkBehaviour
{
    // Easy access for other scripts: GameTimer.Instance, GameTimer.GameIsOver
    public static GameTimer Instance { get; private set; }
    public static bool GameIsOver => Instance != null && Instance.IsGameOver.Value;

    // Fired on EVERY machine when the game ends (used by the scoreboard, FP2-6)
    public static event Action OnGameEnded;

    [Header("Settings")]
    [SerializeField] private float gameDuration = 90f;          // 1 min 30
    [SerializeField] private string gameSceneName = "Laboratory";

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    // Server time at which the game ends. -1 = not started yet. Written ONCE by the server.
    private readonly NetworkVariable<double> endTime = new NetworkVariable<double>(-1);

    // Synced to everyone. Only the server sets it to true.
    public readonly NetworkVariable<bool> IsGameOver = new NetworkVariable<bool>(false);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        IsGameOver.OnValueChanged += OnGameOverChanged;

        if (IsServer)
            NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;

        // A client that spawns after the end still freezes
        if (IsGameOver.Value) ApplyGameOverLocally();
    }

    public override void OnNetworkDespawn()
    {
        IsGameOver.OnValueChanged -= OnGameOverChanged;

        if (IsServer && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;

        if (Instance == this) Instance = null;
    }

    // ===== SERVER: start when everyone has loaded the game scene =====

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode,
                                      List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != gameSceneName || endTime.Value >= 0) return;

        endTime.Value = NetworkManager.ServerTime.Time + gameDuration;
        Debug.Log($"[GameTimer] Game started: {gameDuration} seconds.");
    }

    // ===== EVERY MACHINE: display / SERVER: check the end =====

    private void Update()
    {
        if (!IsSpawned) return;

        // Not started yet → show the full duration
        if (endTime.Value < 0)
        {
            SetTimerText(gameDuration);
            return;
        }

        double remaining = endTime.Value - NetworkManager.ServerTime.Time;
        if (remaining < 0) remaining = 0;
        SetTimerText(remaining);

        // Only the SERVER decides the game is over
        if (IsServer && !IsGameOver.Value && remaining <= 0)
            EndGame();
    }

    private void SetTimerText(double seconds)
    {
        if (timerText == null) return;
        int s = Mathf.CeilToInt((float)seconds);
        timerText.text = $"{s / 60:00}:{s % 60:00}";
        timerText.color = s <= 10 ? Color.red : Color.white; // last 10 seconds in red
    }

    // ===== SERVER: end the game =====

    private void EndGame()
    {
        IsGameOver.Value = true; // sent to every client automatically
        Debug.Log("[GameTimer] Time is up! Game over.");
    }

    // ===== EVERY MACHINE: react to the end =====

    private void OnGameOverChanged(bool previous, bool current)
    {
        if (current) ApplyGameOverLocally();
    }

    private void ApplyGameOverLocally()
    {
        // Freeze MY player (each machine freezes its own)
        var player = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovementController>();
            if (movement != null) movement.enabled = false;

            var input = player.GetComponent<PlayerInput>();
            if (input != null) input.enabled = false;

            var actions = player.GetComponent<PlayerAction>();
            if (actions != null) actions.enabled = false;

            // Stop the walking animation (synced by ClientNetworkAnimator)
            var animator = player.GetComponentInChildren<Animator>();
            if (animator != null) animator.SetFloat("Speed", 0f);
        }

        // Free the cursor (for the scoreboard)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (timerText != null) timerText.text = "00:00";

        OnGameEnded?.Invoke();
    }
}
