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

    [Tooltip("Death clips used when the character is male.")]
    [SerializeField]
    private List<AudioClip> deathClips;

    [Tooltip("Death clips used when the character is female.")]
    [SerializeField]
    private List<AudioClip> femaleDeathClips;

    [Min(0f)]
    [SerializeField] private float hurtCooldown = 2f;

    [Range(0f, 1f)]
    [SerializeField] private float hurtDamageThreshold = 0.15f;

    private PlayerStats playerStats;
    private float nextHurtTime = float.NegativeInfinity;



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


    public void PlayHurt(float damageTaken, float healthBeforeDamage)
    {
        if (damageTaken <= 0f || healthBeforeDamage <= 0f ||
            Time.time < nextHurtTime)
            return;

        float damageFraction = damageTaken / healthBeforeDamage;
        if (damageFraction < hurtDamageThreshold &&
            !Mathf.Approximately(damageFraction, hurtDamageThreshold))
            return;

        List<AudioClip> clips =
            playerStats != null && playerStats.Gender == CharacterGender.Female
                ? femaleHurtClips
                : hurtClips;

        if (PlayRandomClip(clips))
            nextHurtTime = Time.time + hurtCooldown;
    }


    public void PlayDeath()
    {
        List<AudioClip> clips =
            playerStats != null && playerStats.Gender == CharacterGender.Female
                ? femaleDeathClips
                : deathClips;

        PlayRandomClip(clips);
    }



    private bool PlayRandomClip(List<AudioClip> clips)
    {
        if(audioSource == null)
            return false;


        if(clips == null || clips.Count == 0)
            return false;


        AudioClip clip = clips[Random.Range(0, clips.Count)];
        if(clip == null)
            return false;


        audioSource.PlayOneShot(clip);
        return true;
    }
}