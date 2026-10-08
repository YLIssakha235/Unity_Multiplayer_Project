using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CleanLaboratory.Gameplay
{
    public class ItemInstantiator : NetworkBehaviour
    {
        public static ItemInstantiator Singleton;

        public GameObject[] PickableItemsPrefabs;
        public GameObject[] SpawnPositions;

        public int MinNumberOfItemsToSpawn = 3;
        public int MaxNumberofItemsToSpawn = 8;

        public override void OnNetworkSpawn()
        {
            if (Singleton == null) { Singleton = this; }
            if (!IsServer)
            {
                enabled = false;
                return;
            }
            SpawnItemsRandomnly();
        }

        private void SpawnItemsRandomnly()
        {
            int numberOfItemsToSpawn = Random.Range(MinNumberOfItemsToSpawn, MaxNumberofItemsToSpawn);
            if (numberOfItemsToSpawn > SpawnPositions.Length)
            {
                numberOfItemsToSpawn = SpawnPositions.Length; // Prevent to try spawning more objects than there are spaces for it
            }
            int numberOfSpawnedItems = 0;
            int numberOfItemPossibilities = PickableItemsPrefabs.Length;
            int numberOfSpawnPositions = SpawnPositions.Length;
            List<GameObject> availablePositions = SpawnPositions.ToList();
            while (numberOfSpawnedItems != numberOfItemsToSpawn)
            {
                int pickableIndex = Random.Range(0, numberOfItemPossibilities - 1);
                int spawnPositionIndex = Random.Range(0, numberOfSpawnPositions - 1);

                GameObject instance = NetworkObject.Instantiate(PickableItemsPrefabs[pickableIndex]);
                instance.transform.position = availablePositions[spawnPositionIndex].transform.position;
                availablePositions.RemoveAt(spawnPositionIndex);
                numberOfSpawnPositions--;
                numberOfSpawnedItems++;
                instance.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}