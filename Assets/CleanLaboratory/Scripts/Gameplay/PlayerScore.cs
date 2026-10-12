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

        public int CurrentScore => score.Value;

        public override void OnNetworkSpawn()
        {
            score.OnValueChanged += OnScoreChanged;
            UpdateScoreText(score.Value);

            // test fp2-3 : just host attribute point
            //if (IsHost)
            //{
            //    AddScore(10);
            //}

            // test fp2-6 
            //if (IsServer)
            //{
            //    if(OwnerClientId == 0)
            //        AddScore(10);
            //    else
            //        AddScore(20);
            //}
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