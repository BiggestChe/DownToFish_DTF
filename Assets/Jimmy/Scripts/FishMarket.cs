// FishMarket.cs
// Players bring their bucket here and sell all fish.
// Adds value to PlayerData currency.
// Place a trigger collider around the market stall —
// player walks in with bucket and presses interact to sell.
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;

public class FishMarket : UdonSharpBehaviour
{
    [Header("Managers Injection")]
    public GameManager gameManager;
    public PlayerDataPool dataPool;

    [Header("UI Fields Mapping")]
    public TextMeshProUGUI walletDisplay;
    public TextMeshProUGUI notificationText;

    [Header("Shop Definitions Settings")]
    public float[] rodUpgradeCosts = { 150f, 400f };
    public string[] rodUpgradeNames = { "Carbon Fiber Rod", "Divine Altar Rod" };

    void Start()
    {
        UpdateBalanceUI();
    }

    public override void OnDeserialization()
    {
        UpdateBalanceUI();
    }

    // Activated via your Shop World Space UI Menu Button click action
    public void BuyNextRodUpgrade()
    {
        if (gameManager == null || dataPool == null) return;

        PlayerData localData = dataPool.GetLocalData();
        if (localData == null)
        {
            DisplayNotice("Error: Data slot mapping missing.");
            return;
        }

        int nextTierIndex = localData.currentRodTier + 1;

        if (nextTierIndex > rodUpgradeCosts.Length)
        {
            DisplayNotice("Maximum Rod level reached!");
            return;
        }

        float cost = rodUpgradeCosts[nextTierIndex - 1];
        string name = rodUpgradeNames[nextTierIndex - 1];

        if (gameManager.divineFavor >= cost)
        {
            // Set network owner sequence for atomic deductions
            if (!Networking.IsOwner(gameManager.gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameManager.gameObject);

            gameManager.divineFavor -= cost;

            if (!Networking.IsOwner(localData.gameObject))
                Networking.SetOwner(Networking.LocalPlayer, localData.gameObject);

            localData.currentRodTier = nextTierIndex;
            localData.RefreshEquippedRod();

            DisplayNotice($"Purchased {name}!");

            gameManager.RequestSerialization();
            localData.RequestSerialization();

            UpdateBalanceUI();
            gameManager.UpdateDivineUI();
        }
        else
        {
            DisplayNotice($"Need ${cost.ToString("F0")} for {name}!");
        }
    }

    public void UpdateBalanceUI()
    {
        if (gameManager == null || walletDisplay == null) return;
        walletDisplay.text = $"Leftover Cash: ${gameManager.divineFavor.ToString("F2")}";
    }

    private void DisplayNotice(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            SendCustomEventDelayedSeconds(nameof(ResetNoticeText), 3.0f);
        }
    }

    public void ResetNoticeText()
    {
        if (notificationText != null)
            notificationText.text = "Welcome to the Altar Shop";
    }
}