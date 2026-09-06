using System.Collections.Generic;
using UnityEngine;


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
    [SerializeField]
    private List<AudioClip> hurtClips;

    [SerializeField]
    private List<AudioClip> deathClips;



    private void Awake()
    {
        if(audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
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
        PlayRandomClip(hurtClips);
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


        AudioClip clip =
            clips[
                Random.Range(
                    0,
                    clips.Count
                )
            ];


        audioSource.PlayOneShot(clip);
    }
}