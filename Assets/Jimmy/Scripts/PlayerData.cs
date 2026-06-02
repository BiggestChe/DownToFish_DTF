using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PlayerData : UdonSharpBehaviour
{
    [Header("Identity Tracking")]
    [HideInInspector] public int ownerID = -1;

    [Header("Progression Data")]
    [UdonSynced] public int currentRodTier = 0; // 0 = Beginner, 1 = Carbon, 2 = Divine

    [Header("Physical Registry Assets Array")]
    // Inspector configuration elements:
    // Element 0: Beginner Rod Object child path
    // Element 1: Carbon Rod Object child path
    // Element 2: Divine Rod Object child path
    public GameObject[] personalRods;

    public void Initialize(int id)
    {
        ownerID = id;
        RefreshEquippedRod();
    }

    public void Reset()
    {
        ownerID = -1;
        currentRodTier = 0;
        RefreshEquippedRod();
    }

    public void RefreshEquippedRod()
    {
        if (personalRods == null || personalRods.Length == 0) return;

        for (int i = 0; i < personalRods.Length; i++)
        {
            if (personalRods[i] != null)
            {
                // Force hands to clear if upgrading actively
                if (i == currentRodTier - 1)
                {
                    VRC_Pickup pickup = (VRC_Pickup)personalRods[i].GetComponent(typeof(VRC_Pickup));
                    if (pickup != null && pickup.IsHeld) pickup.Drop();
                }

                personalRods[i].SetActive(i == currentRodTier);
            }
        }
    }

    public override void OnDeserialization()
    {
        RefreshEquippedRod();
    }
}