using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class GameManager : UdonSharpBehaviour
{
    [Header("UI / Altar Display")]
    public TextMeshProUGUI hungerDisplay; // "Hunger: $50 / $500"
    public TextMeshProUGUI coinDisplay;   // "Divine Favor: $100"
    public TextMeshProUGUI timeDisplay;    // "Days until Feast: 2"

    [Header("Divine Economy")]
    [UdonSynced] public float currentHungerGoal = 200f;
    [UdonSynced] public float currentOfferingValue = 0f;
    [UdonSynced] public float divineFavor = 0f;
    [UdonSynced] public int daysUntilFeast = 3;

    [Header("Scaling")]
    public float hungerGrowth = 1.5f;
    [Header("Divine Audio")]
    public AudioSource godVoiceSource;
    public AudioClip appeasedClip;
    public AudioClip displeasureClip;

    public void PlayAppeasedSound()
    {
        // Tells every player in the instance to run the local 'LocalPlayAppeased' method
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LocalPlayAppeased");
    }

    // This must be public to be called by the Network Event
    public void LocalPlayAppeased()
    {
        if (godVoiceSource != null && appeasedClip != null)
        {
            godVoiceSource.PlayOneShot(appeasedClip);
            // Bonus: Trigger a screen shake or light flash here for extra "Lethal" juice
        }
    }

    // Called by the Altar/Mouth when a fish is thrown in
    public void SacrificeFish(float value)
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);

        currentOfferingValue += value;
        RequestSerialization();
        UpdateDivineUI();
    }

    // Called when the timer hits zero
    public void TheFeast()
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);

        if (currentOfferingValue >= currentHungerGoal)
        {
            // The God is appeased
            float excess = currentOfferingValue - currentHungerGoal;
            divineFavor += excess; // Excess value is granted back as shop currency

            // Next cycle is harder
            currentHungerGoal = Mathf.Round(currentHungerGoal * hungerGrowth);
            currentOfferingValue = 0;
            daysUntilFeast = 3;

            PlayAppeasedSound();
        }
        else
        {
            // DEATH: The God eats the players
            ExecutePlayers();
        }

        RequestSerialization();
    }

    void ExecutePlayers()
    {
        Debug.Log("The Fish God is displeased. You are the catch.");
        // Logic to trigger a giant mouth animation or respawn everyone
    }

    public override void OnDeserialization()
    {
        UpdateDivineUI();
    }

    void UpdateDivineUI()
    {
        if (hungerDisplay != null) hungerDisplay.text = $"God's Hunger: {currentOfferingValue}/{currentHungerGoal}";
        if (coinDisplay != null) coinDisplay.text = $"Divine Favor: {divineFavor}";
    }
}