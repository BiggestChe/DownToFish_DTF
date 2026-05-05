// ─────────────────────────────────────────────────────────────────
// FishingLineDebug.cs
// Attach to the same GameObject as FishingLineRenderer.
// Draws gizmos in the Scene view and logs issues to Console so you
// can see exactly what the line renderer is doing each frame without
// needing a VR headset.
// ─────────────────────────────────────────────────────────────────
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FishingLineDebug : UdonSharpBehaviour
{
    [Header("References — match these to FishingLineRenderer")]
    public FishingLineRenderer lineRenderer;
    public Transform rodTip;
    public Transform bobber;
    public LineRenderer lr;

    [Header("Test Controls")]
    public bool forceUpdateLine = false;    // tick this in Inspector to force a line update
    public float testLineLength = 8f;       // fed into UpdateLine() when forcing

    [Header("Gizmo Settings")]
    public bool drawGizmos = true;
    public bool logEveryFrame = false;      // very noisy — only enable for a few seconds

    void Update()
    {
        // Force a line update directly from this script so you can test
        // the renderer independently of the state machine
        if (forceUpdateLine && lineRenderer != null)
            lineRenderer.UpdateLine(testLineLength, true);

        if (logEveryFrame)
            LogLineState();
    }

    // Call this from the Inspector context menu to get a one-shot
    // snapshot of everything without spamming the console
    [ContextMenu("Log Line State Now")]
    void LogLineState()
    {
        if (lr == null)
        {
            Debug.LogError("[LineDebug] LineRenderer reference is NULL — assign it in Inspector");
            return;
        }

        // Check 1 — is the LineRenderer component enabled?
        Debug.Log("[LineDebug] LineRenderer enabled: " + lr.enabled);

        // Check 2 — is the GameObject itself active?
        Debug.Log("[LineDebug] GameObject active: " + lr.gameObject.activeInHierarchy);

        // Check 3 — how many positions does it think it has?
        Debug.Log("[LineDebug] positionCount: " + lr.positionCount);

        // Check 4 — print every position so you can see if they are
        // clustered at the origin or spread correctly
        for (int i = 0; i < lr.positionCount; i++)
            Debug.Log("[LineDebug] position[" + i + "] = " + lr.GetPosition(i));

        // Check 5 — material and width
        Debug.Log("[LineDebug] material: " + (lr.material != null ? lr.material.name : "NULL"));
        Debug.Log("[LineDebug] startWidth: " + lr.startWidth + "  endWidth: " + lr.endWidth);

        // Check 6 — rod tip and bobber positions
        if (rodTip != null)
            Debug.Log("[LineDebug] rodTip world pos: " + rodTip.position);
        else
            Debug.LogError("[LineDebug] rodTip is NULL");

        if (bobber != null)
            Debug.Log("[LineDebug] bobber world pos: " + bobber.position);
        else
            Debug.LogError("[LineDebug] bobber is NULL");

        // Check 7 — are tip and bobber at the same position?
        // If so the line has zero length and won't be visible
        if (rodTip != null && bobber != null)
        {
            float dist = Vector3.Distance(rodTip.position, bobber.position);
            Debug.Log("[LineDebug] distance tip → bobber: " + dist.ToString("F3") + "m");
            if (dist < 0.01f)
                Debug.LogWarning("[LineDebug] tip and bobber are almost at the same position — line will be invisible");
        }
    }

    // Draws the line path and endpoint markers in the Scene view
    // so you can see what the line renderer is doing spatially
    void OnDrawGizmos()
    {
        if (!drawGizmos || lr == null) return;

        // Draw a magenta sphere at each line position
        Gizmos.color = Color.magenta;
        for (int i = 0; i < lr.positionCount; i++)
            Gizmos.DrawWireSphere(lr.GetPosition(i), 0.03f);

        // Draw yellow lines connecting the positions so you can see the arc
        Gizmos.color = Color.yellow;
        for (int i = 0; i < lr.positionCount - 1; i++)
            Gizmos.DrawLine(lr.GetPosition(i), lr.GetPosition(i + 1));

        // Draw green sphere at rod tip, red at bobber
        if (rodTip != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(rodTip.position, 0.05f);
        }

        if (bobber != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bobber.position, 0.05f);
        }
    }
}