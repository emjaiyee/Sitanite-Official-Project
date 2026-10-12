using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerSoundHelper : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField]
    private AudioSource audioSource;


    [Header("Movement")]
    [SerializeField]
    private List<AudioClip> footstepClips;


    [Header("Combat")]
    [SerializeField]
    private List<AudioClip> attackClips;

    [SerializeField]
    private List<AudioClip> skillClips;


    [Header("Movement Actions")]
    [SerializeField]
    private List<AudioClip> dashClips;


    [Header("Damage")]
    [Tooltip("Hurt clips used when the character is male.")]
    [SerializeField]
    private List<AudioClip> hurtClips;

    [Tooltip("Hurt clips used when the character is female.")]
    [SerializeField]
    private List<AudioClip> femaleHurtClips;

    [SerializeField]
    private List<AudioClip> deathClips;

    private PlayerStats playerStats;



    private void Awake()
    {
        if(audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if(audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>(true);
        }

        playerStats = GetComponentInParent<PlayerStats>();
        if (playerStats == null)
        {
            playerStats = GetComponentInChildren<PlayerStats>(true);
        }
    }



    public void PlayFootstep()
    {
        PlayRandomClip(footstepClips);
    }


    public void PlayAttack()
    {
        PlayRandomClip(attackClips);
    }


    public void PlaySkill()
    {
        PlayRandomClip(skillClips);
    }


    public void PlayDash()
    {
        PlayRandomClip(dashClips);
    }


    public void PlayHurt()
    {
        List<AudioClip> clips =
            playerStats != null && playerStats.Gender == CharacterGender.Female
                ? femaleHurtClips
                : hurtClips;

        PlayRandomClip(clips);
    }


    public void PlayDeath()
    {
        PlayRandomClip(deathClips);
    }



    private void PlayRandomClip(List<AudioClip> clips)
    {
        if(audioSource == null)
            return;


        if(clips == null || clips.Count == 0)
            return;


        AudioClip clip = clips[Random.Range(0, clips.Count)];
        if(clip == null)
            return;


        audioSource.PlayOneShot(clip);
    }
}