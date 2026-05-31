
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using VRC.Udon.Common;


namespace CrazyEightBall
{
    public class EightBallFortuneScript : UdonSharpBehaviour
    {
        [SerializeField]  private GameObject innertrigger;
        public bool isheld;
        public float lerpamount;
        public Animator fortuneanimator;
        public int ownerNumber;
        [UdonSynced] public int fortunenumber;
        private float shaketimer;
        public float shakeendtimer;
        public bool shaken;
        private float movementdetection;
        public VRC_Pickup eightBallPickup;

        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            movementdetection = value;
        }

        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            movementdetection = value;
        }
        

        // When object is held, allow the inner trigger to move towards the center of the 8 ball
        public void Update()
        { 
            if (isheld)
                {
                    innertrigger.transform.position = Vector3.Lerp(innertrigger.transform.position, transform.position, lerpamount);
                }
        }

        private void OnTriggerExit(Collider other)
        {
            if (innertrigger == other.gameObject)
            {
                if (!shaken && movementdetection == 0)
                {
                    if (Networking.IsOwner(eightBallPickup.gameObject))
                        {
                            ownerNumber = Random.Range(0, 19);
                            fortunenumber = ownerNumber;
                            RequestSerialization();
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "FortuneResult");
                            shaken = true;
                        }
                }
            }
        }

        // Startup shake animation
        public void FortuneResult()
        {
            shaken = true;
            fortuneanimator.SetBool("Shaken", true);
            fortuneanimator.SetInteger("FortuneInt", fortunenumber);
            SendCustomEventDelayedSeconds("shakeAnimReset", 0.5f);
            SendCustomEventDelayedSeconds("FortuneResetSync", 2);
        }

        // Reset Shaken Bool so the animation doesn't loop itself.
        public void shakeAnimReset()
        {
            fortuneanimator.SetBool("Shaken", false);
        }
        
        // Reset Shaken value
        public void FortuneResetSync()
        {
            shaken = false;
        }
        
        // Sync up fortune result w/ everyone
        public override void OnPreSerialization()
        {
            fortunenumber = ownerNumber;
        }
    }
}
