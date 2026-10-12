
using System.Linq;
using System.Text;
using CleanLaboratory.Gameplay;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreboardManager : MonoBehaviour
{
    [SerializeField] private GameObject scoreboardPanel;
    [SerializeField] private TMP_Text rankingText;

    private void Start()
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameTimer.OnGameEnded += ShowScoreboard;
    }

    private void OnDisable()
    {
        GameTimer.OnGameEnded -= ShowScoreboard;
    }

    private void ShowScoreboard()
    {
        if (NetworkManager.Singleton == null || rankingText == null)
            return;

        // Accessible sur le Host ET sur les Clients.
        var players = FindObjectsOfType<PlayerScore>()
            .Where(player => player.IsSpawned)
            .OrderByDescending(player => player.CurrentScore)
            .ThenBy(player => player.OwnerClientId)
            .ToList();

        StringBuilder ranking = new StringBuilder();

        for (int i = 0; i < players.Count; i++)
        {
            ranking.AppendLine(
                $"{i + 1}. Joueur {players[i].OwnerClientId + 1} : " +
                $"{players[i].CurrentScore} points"
            );
        }

        rankingText.text = ranking.ToString();

        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(true);
    }
}
