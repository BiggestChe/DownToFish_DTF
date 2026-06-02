using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonVR.PartyGames.Beerpong
{
    public class BeerpongV2Ball : UdonSharpBehaviour
    {
        public BeerpongV2 Controller;
        public int Player;
        public Transform RespawnPoint;
        public AudioSource BallAudio;

        private Rigidbody ball;

        public void InitBall()
        {
            ball = gameObject.GetComponent<Rigidbody>();
        }
        private void OnTriggerEnter(Collider _other)
        {
            Controller.CheckCollision(Player, gameObject, _other);
        }

        private void OnTriggerExit(Collider _other)
        {
            Debug.Log("Checking OnTriggerExit. Respawn on leave is " + Controller.UserSettings.RespawnOnLeave);
            if (Controller.UserSettings.RespawnOnLeave)
            {
                if (_other == Controller.RespawnBox && Networking.IsOwner(gameObject)) RespawnBall();
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            BallAudio.Stop();
            BallAudio.clip = Controller.BallSounds[Random.Range(0, Controller.BallSounds.Length)];
            BallAudio.Play();
        }

        public void N_RespawnBall()
        {
            SendCustomEventDelayedSeconds("RespawnBall", Controller.UserSettings.RespawnBall);
        }
        public void RespawnBall()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            ball.velocity = Vector3.zero;
            transform.position = RespawnPoint.position;
        }
    }
}