using TMPro;
using Unity.Netcode;
using UnityEngine;
namespace CleanLaboratory.Gameplay
{
    public class PlayerActions : NetworkBehaviour
    {
        private ActionInput _actionInput;
        private NetworkList<int> _inventoryNetworkList;
        public LayerMask PickableLayer;

        [Header("Items")]
        public int itemSlots = 2;
        [SerializeField]
        Transform[] AttachPoints;
        [SerializeField] private PickableItem[] _items;

        [Header("UI")]
        [SerializeField] private TMP_Text _focusedItemText;

        [Header("Debug")]
        [SerializeField] private PickableItem _focusedPickableItem;

        private ClientRpcParams _rpcParams;

        private void Awake()
        {
            _inventoryNetworkList = new NetworkList<int>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer || IsOwner)
            {
                if (AttachPoints.Length < itemSlots)
                {
                    itemSlots = AttachPoints.Length;
                    Debug.LogWarning("Not enough of attachPoints to put items from the inventory. Consider adding more attach points or reducing item slots");
                }
                _items = new PickableItem[itemSlots];
            }
            if (IsServer)
            {
                _rpcParams = new ClientRpcParams()
                {
                    Send = new ClientRpcSendParams()
                    {
                        TargetClientIds = new ulong[1] { OwnerClientId }
                    }
                };
            }
            if (IsOwner)
            {
                if (_actionInput == null)
                {
                    _actionInput = GetComponent<ActionInput>();
                }
                if (_focusedItemText == null)
                {
                    _focusedItemText = GetComponentInChildren<TMP_Text>();
                }
            }
        }

        private void FixedUpdate()
        {
            if ((!IsOwner))
            {
                return;
            }
            GetNewTargetItem();
        }

        /// <summary>
        /// Get the first available slot of the inventory.
        /// </summary>
        /// <returns>The index of the available slot.</returns>
        private int RequestItemSlot()
        {
            for (int i = 0; i < itemSlots; i++)
            {
                if (_items[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Check if the item is pickable then request the server the authorization.
        /// </summary>
        public void TryGrabItem()
        {
            if (_focusedPickableItem == null || !_focusedPickableItem.IsPickableNetwork.Value)
            {
                return;
            }
            int slotIndex = RequestItemSlot();
            if (slotIndex == -1)
            {
                return;
            }
            RequestItemServerRpc(slotIndex, _focusedPickableItem.NetworkObject);
        }

        /// <summary>
        /// Request the server to pick the item.
        /// </summary>
        /// <param name="slotIndex">Index of the slot to fill.</param>
        /// <param name="pickableObjectRef">Reference to the NetworkObject of the objet to pick.</param>
        [ServerRpc]
        public void RequestItemServerRpc(int slotIndex, NetworkObjectReference pickableObjectRef)
        {
            //We could check if the player is at range and in the right direction to ensure he's not cheating
            NetworkObject no = pickableObjectRef;
            if (pickableObjectRef.TryGet(out NetworkObject pickableObject))
            {
                PickableItem pickable = pickableObject.GetComponent<PickableItem>();
                if (!pickable.IsPickableNetwork.Value)
                {
                    return;
                }
                if (_items[slotIndex] != null)
                {
                    Debug.LogError("Discrepancy between server and client. Something bad happened!");
                }
                _items[slotIndex] = pickable;
                pickable.PickItem(AttachPoints[slotIndex]);
                GrabItemClientRpc(slotIndex, pickableObjectRef, _rpcParams);
                return;
            }
        }

        /// <summary>
        /// Get the item out of the NetworkObjectReference the PickableItem to the itemList.
        /// </summary>
        /// <param name="slotIndex">Index of the item list to fill.</param>
        /// <param name="pickableObjectRef">Reference to the NetworkObject of the objet to pick.</param>
        [ClientRpc]
        public void GrabItemClientRpc(int slotIndex, NetworkObjectReference pickableObjectRef, ClientRpcParams clientRpcParams)
        {
            if (!IsOwner)
            {
                return;
            }
            if (pickableObjectRef.TryGet(out NetworkObject pickableObject))
            {
                PickableItem pickable = pickableObject.GetComponent<PickableItem>();
                _items[slotIndex] = pickable;
            }
        }

        /// <summary>
        /// Drop the Items in front of the player
        /// </summary>
        public void DropItem()
        {
            DropItemsServerRpc();
        }

        /// <summary>
        /// Removes the items from the item list.
        /// </summary>
        /// <param name="clientRpcParams"></param>
        [ClientRpc]
        private void ConfirmDropItemsClientRpc(ClientRpcParams clientRpcParams)
        {
            for (int i = 0; i < itemSlots; i++)
            {
                if (_items[i] == null)
                {
                    return;
                }
                _items[i] = null;
            }
        }

        /// <summary>
        /// The server drops the items on the ground on front of the player. If the floor is too low (>10 units), the drop fails.
        /// </summary>
        [ServerRpc]
        public void DropItemsServerRpc()
        {
            Vector3 dropPosition = transform.position + (transform.forward * 0.5f);
            RaycastHit hit;
            if (Physics.Raycast(dropPosition, Vector3.down, out hit, 10f, ~PickableLayer))
            {
                for (int i = 0; i < itemSlots; i++)
                {
                    if (_items[i] == null)
                    {
                        break;
                    }
                    PickableItem pi = _items[i];
                    NetworkObject piObject = pi.GetComponent<NetworkObject>();
                    pi.DropItem();
                    pi.transform.position = hit.point;
                    _items[i] = null;
                    ConfirmDropItemsClientRpc(_rpcParams);
                }
            }
            else
            {
                Debug.LogError("Tried to drop in an empty space.");
            }
        }

        private void GetNewTargetItem()
        {
            RaycastHit hit;
            Transform cameraTransform = Camera.main.transform;
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, 2, PickableLayer))
            {
                PickableItem pi = hit.transform.GetComponent<PickableItem>();
                if (pi == null || !pi.IsPickableNetwork.Value)
                {
                    return;
                }
                _focusedItemText.text = pi.PickableName;
                _focusedPickableItem = pi;
                return;
            }
            _focusedItemText.text = "";
            _focusedPickableItem = null;
        }

        public void ToggleCursor()
        {
            int newState = 0;
            if (Cursor.lockState == 0)
            {
                newState = 1;
            }
            Cursor.lockState = (CursorLockMode)newState;
        }
    }
}