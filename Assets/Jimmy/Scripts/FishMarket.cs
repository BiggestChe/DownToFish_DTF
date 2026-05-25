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
    [Header("References")]
    public PlayerDataPool  dataPool;
    public FishingBucket   bucket;          // the player's bucket
    public TextMeshProUGUI lastSaleText;    // shows "Sold 3 fish for $45"

    // Called by interact button or trigger zone UI button
    public void SellAll()
    {
        if (dataPool == null || bucket == null) return;

        PlayerData data = dataPool.GetLocalData();
        if (data == null) return;

        int   count = bucket.GetFishCount();
        float value = bucket.GetTotalValue();

        if (count == 0)
        {
            Debug.Log("[FishMarket] Bucket is empty");
            if (lastSaleText != null)
                lastSaleText.text = "Bucket is empty";
            return;
        }

        // Pay the player
        data.AddCurrency(value);

        if (lastSaleText != null)
            lastSaleText.text = "Sold " + count
                              + " fish for $" + value.ToString("F2");

        Debug.Log("[FishMarket] Sold " + count
                + " fish for $" + value.ToString("F2")
                + "  new balance=" + data.currency);

        // Clear the bucket after selling
        // FishingBucket needs an EmptyBucket method without return logic
        // Add this minimal version if not already present:
        bucket.EmptyBucket();
    }
}