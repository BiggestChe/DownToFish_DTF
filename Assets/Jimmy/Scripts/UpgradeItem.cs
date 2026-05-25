// UpgradeItem.cs
// One instance per purchasable item in the shop.
// Handles its own purchase logic and UI state.
// Scalable — add new items by placing new UpgradeItem GameObjects.
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;
using UnityEngine.UI;

public class UpgradeItem : UdonSharpBehaviour
{
    [Header("Item Settings")]
    public string itemName        = "Basic Rod";
    public string itemDescription = "Casts further, reels faster";
    public float  price           = 50f;
    public int    requiredTier    = 0;   // player must have this tier to buy
    public int    grantsTier      = 1;   // rod tier this purchase unlocks

    [Header("References")]
    public PlayerDataPool dataPool;
    public RodSpawner     rodSpawner;

    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI statusText;   // "Owned" / "Can't afford" / "Buy"
    public Button          buyButton;
    public Image           buttonImage;

    [Header("Colors")]
    public Color affordableColor   = Color.green;
    public Color unaffordableColor = Color.red;
    public Color ownedColor        = Color.gray;

    void Start()
    {
        if (nameText        != null) nameText.text        = itemName;
        if (descriptionText != null) descriptionText.text = itemDescription;
        if (priceText       != null) priceText.text       = "$" + price.ToString("F0");
    }

    void Update()
    {
        RefreshUI();
    }

    void RefreshUI()
    {
        if (dataPool == null) return;

        PlayerData data = dataPool.GetLocalData();
        if (data == null) return;

        bool owned      = data.rodTier >= grantsTier;
        bool eligible   = data.rodTier == requiredTier;
        bool canAfford  = data.currency >= price;

        if (owned)
        {
            if (statusText  != null) statusText.text  = "Owned";
            if (buttonImage != null) buttonImage.color = ownedColor;
            if (buyButton   != null) buyButton.interactable = false;
        }
        else if (!eligible)
        {
            if (statusText  != null) statusText.text  = "Locked";
            if (buttonImage != null) buttonImage.color = ownedColor;
            if (buyButton   != null) buyButton.interactable = false;
        }
        else if (!canAfford)
        {
            if (statusText  != null) statusText.text  = "Need $" + price.ToString("F0");
            if (buttonImage != null) buttonImage.color = unaffordableColor;
            if (buyButton   != null) buyButton.interactable = false;
        }
        else
        {
            if (statusText  != null) statusText.text  = "Buy";
            if (buttonImage != null) buttonImage.color = affordableColor;
            if (buyButton   != null) buyButton.interactable = true;
        }
    }

    // Wired to Buy button OnClick in Inspector
    public void OnBuyPressed()
    {
        if (dataPool == null) return;

        PlayerData data = dataPool.GetLocalData();
        if (data == null) return;

        if (data.rodTier >= grantsTier)
        {
            Debug.Log("[UpgradeItem] Already owned: " + itemName);
            return;
        }

        if (data.rodTier != requiredTier)
        {
            Debug.Log("[UpgradeItem] Not eligible: " + itemName);
            return;
        }

        if (!data.SpendCurrency(price))
        {
            Debug.Log("[UpgradeItem] Cannot afford: " + itemName);
            return;
        }

        data.SetRodTier(grantsTier);

        // Swap the player's rod to the new tier
        if (rodSpawner != null)
            rodSpawner.UpgradeRod(grantsTier);

        Debug.Log("[UpgradeItem] Purchased: " + itemName);
    }
}