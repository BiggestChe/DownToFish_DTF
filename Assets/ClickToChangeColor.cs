using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ClickToChangeColor : UdonSharpBehaviour
{
    private Material _material;

    void Start()
    {
        _material = GetComponent<Renderer>().material;
    }

    public override void Interact()
    {
        _material.color = new Color(
            Random.Range(0f, 1f),
            Random.Range(0f, 1f),
            Random.Range(0f, 1f)
        );
    }
}
