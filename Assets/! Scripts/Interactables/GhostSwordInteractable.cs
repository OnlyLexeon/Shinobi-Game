using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GhostSwordInteractable : Interactable
{
    public bool disappearOnInteract = true;
    
    public AudioClip pickUpClip;
    public AudioClip ambientClip;

    [Header("Auto")]
    public AudioSource audioSource;
    public GameObject player;
    public bool isPlayingAmbientLoop = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) Debug.LogWarning("No audio sauce!");

        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (player != null)
        {
            if (!isPlayingAmbientLoop && Vector3.Distance(player.transform.position, gameObject.transform.position) < 20f)
            {
                isPlayingAmbientLoop = true;
                audioSource.loop = true;
                audioSource.clip = ambientClip;
                audioSource.Play();
            }
            else if (Vector3.Distance(player.transform.position, gameObject.transform.position) > 20f)
            {
                audioSource.Stop();
                isPlayingAmbientLoop = false;
            }
        }
    }

    public override void DoHoverOverActions()
    {
        Debug.Log("Hovering over interactable");
    }

    public override void DoInteraction()
    {
        Debug.Log("Interacted");

        Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        player.hasGhostSword = true;

        //permanently unlock ghost mode
        Unlocked.Instance.UnlockGhostMode();

        audioSource.PlayOneShot(pickUpClip);

        if (disappearOnInteract) Destroy(gameObject);
        else
        {
            audioSource.Stop();
        }
    }
}
