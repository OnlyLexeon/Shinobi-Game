using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    public abstract void DoHoverOverActions();
    public abstract void DoInteraction();
}
