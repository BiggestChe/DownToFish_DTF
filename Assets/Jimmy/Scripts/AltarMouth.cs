using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
public class AltarMouth : UdonSharpBehaviour
{
    public GameManager gameManager;
    public AudioSource munchSound;

    public void OnTriggerEnter(Collider other)
    {
        FishPrefab fish = other.GetComponent<FishPrefab>();
        if (fish != null)
        {
            gameManager.SacrificeFish(fish.fishValue);
            if (munchSound != null) munchSound.Play();
            
            // Delete the fish object
            Networking.Destroy(other.gameObject);
        }
    }
}