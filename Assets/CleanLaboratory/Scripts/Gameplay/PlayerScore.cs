using Unity.Netcode;
using UnityEngine;
using TMPro;

namespace CleanLaboratory.Gameplay
{
    public class PlayerScore : NetworkBehaviour
    {
        [SerializeField]
        private TMP_Text scoreText;

        private NetworkVariable<int> score = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public override void OnNetworkSpawn()
        {
            score.OnValueChanged += OnScoreChanged;
            UpdateScoreText(score.Value);
        }

        public override void OnNetworkDespawn()
        {
            score.OnValueChanged -= OnScoreChanged;
        }

        private void OnScoreChanged(int previousValue, int newValue)
        {
            UpdateScoreText(newValue);
        }

        private void UpdateScoreText(int value)
        {
            if (scoreText != null)
                scoreText.text = "Score : " + value;
        }

        public void AddScore(int points)
        {
            if (!IsServer)
                return;

            score.Value += points;
        }
    }
}