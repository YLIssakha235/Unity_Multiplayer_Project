using Unity.Netcode;
using UnityEngine;
namespace CleanLaboratory.Gameplay
{
    public class PickableItem : NetworkBehaviour
    {
        public string PickableName;

        public NetworkVariable<bool> IsPickableNetwork = new NetworkVariable<bool>();

        private Transform _currentAttachPoint;
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                IsPickableNetwork.Value = true;
            }
        }

        public void PickItem(Transform attachPoint)
        {
            if (IsServer && IsPickableNetwork.Value)
            {
                _currentAttachPoint = attachPoint;
                this.IsPickableNetwork.Value = false;
            }
        }

        public void DropItem()
        {
            if (IsServer)
            {
                _currentAttachPoint = null;
                this.IsPickableNetwork.Value = true;
            }
        }
        public void Update()
        {
            if (IsServer && _currentAttachPoint != null)
            {
                transform.position = _currentAttachPoint.position;
            }
        }
    }
}