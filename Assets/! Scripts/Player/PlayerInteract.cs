using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static CursorTipsManager;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interacting")]
    [SerializeField] private float interactRange = 3;
    public float scanAngle = 35f;
    private Interactable interactable;

    [Header("Debug Refer")]
    public GameObject lookingAt;

    [Header("Tip")]
    public CursorTipsManager.Tip tip;

    [Header("References (Auto)")]
    public Player playerScript;
    public LayerMask interactLayer;


    private void Start()
    {
        if (playerScript == null) playerScript = GetComponent<Player>();
        if (playerScript == null) Debug.LogWarning("No Playerscript found!");

        if (interactLayer == 0) interactLayer = LayerMask.GetMask("Interact");
    }

    // Update is called once per frame
    void Update()
    {
        if (ScanForInteractable())
        {
            interactable = lookingAt?.GetComponent<Interactable>();

            if (interactable != null)
            {
                interactable.DoHoverOverActions();

                if (CursorTipsManager.Instance != null)
                {
                    tip.key = KeybindManager.Instance.keybinds["Interact"];
                    tip.tipMessage = "Interact";
                    CursorTipsManager.Instance.MakeTip(tip);
                }

                if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Interact"]))
                {
                    interactable.DoInteraction();
                }
            }
        }
        else
        {
            interactable = null;

            if (CursorTipsManager.Instance != null)
            {
                tip.key = KeybindManager.Instance.keybinds["Interact"];
                tip.tipMessage = "Interact";
                CursorTipsManager.Instance.RemoveTip(tip);
            }
                
        }

    }

    public bool ScanForInteractable() // Closest/First rayhit enemy is Target
    {
        Collider[] hitColliders = Physics.OverlapSphere(playerScript.cameraFacing.position, interactRange, interactLayer);
        float closestDistance = float.MaxValue;
        GameObject closestEnemy = null;

        foreach (Collider collider in hitColliders)
        {
            Vector3 closestPointOnCollider = collider.ClosestPoint(playerScript.cameraFacing.position);
            Vector3 directionToPoint = (closestPointOnCollider - playerScript.cameraFacing.position).normalized;

            float angleToPoint = Vector3.Angle(playerScript.cameraFacing.forward, directionToPoint);
            if (angleToPoint <= scanAngle / 2)
            {
                float distanceToPoint = Vector3.Distance(playerScript.cameraFacing.position, closestPointOnCollider);

                if (distanceToPoint < closestDistance)
                {
                    closestDistance = distanceToPoint;
                    closestEnemy = collider.gameObject;
                }
            }
        }

        if (closestEnemy != null)
        {
            lookingAt = closestEnemy;
            return true;
        }

        lookingAt = null;
        return false;
    }

}
