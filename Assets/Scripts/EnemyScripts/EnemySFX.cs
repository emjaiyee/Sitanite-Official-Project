using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemySFX : MonoBehaviour
{
    [Header("Place Animation SFX Here")]
    [SerializeField] private AudioClip[] walkSFX;
    [SerializeField] private AudioClip[] attackSFX;
    [SerializeField] private AudioClip deathSFX;

    private AudioSource audioSource;
    private int walkSFXIndex;
    private int attackSFXIndex;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>();

        if (audioSource != null)
        {
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }
    }

    private static AudioClip GetNextClip(AudioClip[] clips, ref int index)
    {
        if (clips == null || clips.Length == 0)
            return null;

        AudioClip clip = clips[index % clips.Length];
        index = (index + 1) % clips.Length;
        return clip;
    }

    public void PlayWalkSFX()
    {
        if (audioSource == null)
            return;

        AudioClip clip = GetNextClip(walkSFX, ref walkSFXIndex);
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void PlayAttackSFX() // Attack, Shoot or Casting SFX
    {
        if (audioSource == null)
            return;

        AudioClip clip = GetNextClip(attackSFX, ref attackSFXIndex);
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void PlayDeathSFX()
    {
        if (audioSource == null || deathSFX == null)
            return;

        audioSource.PlayOneShot(deathSFX);
    }
}

