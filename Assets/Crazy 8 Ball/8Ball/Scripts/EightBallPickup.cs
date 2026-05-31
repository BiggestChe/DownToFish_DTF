
using CrazyEightBall;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace CrazyEightBall
{
    public class EightBallPickup : UdonSharpBehaviour
    {
        public EightBallFortuneScript mainScript;
        public GameObject eightballObject;

        void Start()
        {
        
        }
        
        public override void OnPickup()
        {
            if (Networking.IsOwner(gameObject))
            {
                mainScript.isheld = true;
                Networking.SetOwner(Networking.LocalPlayer, eightballObject);
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "OnPickup");
            }
        }
        
        public override void OnDrop()
        {
            mainScript.isheld = false;
        }
    }
}
