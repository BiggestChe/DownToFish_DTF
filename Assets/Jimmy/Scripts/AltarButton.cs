
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AltarButton : UdonSharpBehaviour
{
[Header("References")]
    public GameManager gameManager;
    
    [Header("Visual Animations")]
    public Animator leverAnimator; // Optional: Plays a pull animation down

    public override void Interact()
    {
        if (gameManager == null) return;

        Debug.Log("[Lever] Pulling Lever! Submitting offerings to the Fish God.");
        
        if (leverAnimator != null)
        {
            leverAnimator.SetTrigger("Pull");
        }

        // Direct call to run the judgment checking sequence inside the manager
        gameManager.BeginTheFeast();
    }
}
