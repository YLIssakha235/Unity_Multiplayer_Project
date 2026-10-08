using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
namespace CleanLaboratory.Network
{
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}