using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modular weapon audio system.
///
/// Audio is configured separately from ItemData.
/// The currently equipped weapon is identified using ItemData.WeaponId.
/// Skill audio is identified using ItemData.WeaponSkillType.
///
/// Supports:
/// - Normal attacks
/// - Individual skill sounds
/// - Charge start
/// - Charge loop
/// - Fully charged
/// - Charge release
/// </summary>
public class WeaponAudioController : MonoBehaviour
{
    // =========================================================
    // AUDIO PROFILE
    // =========================================================

    [Serializable]
    public class WeaponAudioProfile
    {
        [Header("Weapon")]
        public string weaponId;

        [Header("Normal Attack")]
        public AudioClip attackSound;

        [Header("Skill Sounds")]
        public List<SkillAudioProfile> skills =
            new List<SkillAudioProfile>();
    }


    [Serializable]
    public class SkillAudioProfile
    {
        [Header("Skill")]
        public WeaponSkillType skillType;

        [Header("Skill")]
        public AudioClip skillSound;

        [Header("Charging")]
        public AudioClip chargeStartSound;
        public AudioClip chargeLoopSound;
        public AudioClip fullyChargedSound;
        public AudioClip chargeReleaseSound;
    }


    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private PlayerEquipment playerEquipment;

    [SerializeField] private AudioSource audioSource;


    // =========================================================
    // WEAPON PROFILES
    // =========================================================

    [Header("Weapon Audio Profiles")]
    [SerializeField]
    private List<WeaponAudioProfile> weaponProfiles =
        new List<WeaponAudioProfile>();


    // =========================================================
    // RUNTIME
    // =========================================================

    private WeaponAudioProfile currentWeaponProfile;

    private AudioSource chargeLoopSource;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (playerEquipment == null)
            playerEquipment =
                GetComponentInParent<PlayerEquipment>();

        if (audioSource == null)
            audioSource =
                GetComponent<AudioSource>();

        CreateChargeLoopSource();
    }


    private void OnEnable()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -=
                HandleEquipmentChanged;

            EquipmentManager.Instance.OnEquipmentChanged +=
                HandleEquipmentChanged;
        }

        RefreshCurrentWeapon();
    }


    private void OnDisable()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -=
                HandleEquipmentChanged;
        }

        StopChargeLoop();
    }


    // =========================================================
    // EQUIPMENT
    // =========================================================

    private void HandleEquipmentChanged(
        EquipmentType changedType,
        InventoryItem newItem)
    {
        if (changedType != EquipmentType.Weapon)
            return;

        RefreshCurrentWeapon();
    }


    private void RefreshCurrentWeapon()
    {
        StopChargeLoop();

        currentWeaponProfile = null;

        if (playerEquipment == null)
            return;

        ItemData weaponData =
            playerEquipment.CurrentWeaponData;

        if (weaponData == null)
            return;

        currentWeaponProfile =
            FindWeaponProfile(
                weaponData.WeaponId
            );

        if (currentWeaponProfile == null)
        {
            Debug.LogWarning(
                $"WeaponAudioController: " +
                $"No audio profile found for weapon " +
                $"'{weaponData.WeaponId}'.",
                this
            );
        }
    }


    private WeaponAudioProfile FindWeaponProfile(
        string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId))
            return null;

        foreach (WeaponAudioProfile profile in weaponProfiles)
        {
            if (profile == null)
                continue;

            if (string.Equals(
                    profile.weaponId,
                    weaponId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }
        }

        return null;
    }


    // =========================================================
    // NORMAL ATTACK
    // =========================================================

    public void PlayAttackSound()
{
    RefreshCurrentWeapon();

    if (currentWeaponProfile == null)
        return;

    PlayOneShot(currentWeaponProfile.attackSound);
}


    // =========================================================
    // SKILL
    // =========================================================

   public void PlaySkillSound()
{
    RefreshCurrentWeapon();

    if (currentWeaponProfile == null)
        return;

    ItemData weaponData =
        playerEquipment == null
            ? null
            : playerEquipment.CurrentWeaponData;

    if (weaponData == null)
        return;

    SkillAudioProfile skillProfile =
        FindSkillProfile(weaponData.WeaponSkillType);

    if (skillProfile == null)
        return;

    PlayOneShot(skillProfile.skillSound);
}


    private SkillAudioProfile FindSkillProfile(
        WeaponSkillType skillType)
    {
        if (currentWeaponProfile == null)
            return null;

        foreach (SkillAudioProfile skill
                 in currentWeaponProfile.skills)
        {
            if (skill == null)
                continue;

            if (skill.skillType == skillType)
                return skill;
        }

        return null;
    }


    // =========================================================
    // CHARGING
    // =========================================================

    public void PlayChargeStartSound()
    {
        SkillAudioProfile skill =
            GetCurrentSkillProfile();

        if (skill == null)
            return;

        PlayOneShot(
            skill.chargeStartSound
        );
    }


    public void StartChargeLoop()
    {
        SkillAudioProfile skill =
            GetCurrentSkillProfile();

        if (skill == null)
            return;

        if (skill.chargeLoopSound == null)
            return;

        if (chargeLoopSource == null)
            CreateChargeLoopSource();

        if (chargeLoopSource.isPlaying)
            return;

        chargeLoopSource.clip =
            skill.chargeLoopSound;

        chargeLoopSource.loop = true;

        chargeLoopSource.Play();
    }


    public void PlayFullyChargedSound()
    {
        SkillAudioProfile skill =
            GetCurrentSkillProfile();

        if (skill == null)
            return;

        PlayOneShot(
            skill.fullyChargedSound
        );
    }


    public void PlayChargeReleaseSound()
    {
        SkillAudioProfile skill =
            GetCurrentSkillProfile();

        if (skill == null)
            return;

        PlayOneShot(
            skill.chargeReleaseSound
        );
    }


    public void StopChargeLoop()
    {
        if (chargeLoopSource == null)
            return;

        if (chargeLoopSource.isPlaying)
            chargeLoopSource.Stop();

        chargeLoopSource.clip = null;
    }


    // =========================================================
    // CURRENT SKILL
    // =========================================================

    private SkillAudioProfile GetCurrentSkillProfile()
    {
        if (currentWeaponProfile == null)
            return null;

        if (playerEquipment == null)
            return null;

        ItemData weaponData =
            playerEquipment.CurrentWeaponData;

        if (weaponData == null)
            return null;

        return FindSkillProfile(
            weaponData.WeaponSkillType
        );
    }


    // =========================================================
    // AUDIO
    // =========================================================

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
            return;

        if (audioSource == null)
        {
            Debug.LogWarning(
                "WeaponAudioController: " +
                "AudioSource is not assigned.",
                this
            );

            return;
        }

        audioSource.PlayOneShot(clip);
    }


    // =========================================================
    // CHARGE AUDIO SOURCE
    // =========================================================

    private void CreateChargeLoopSource()
    {
        if (chargeLoopSource != null)
            return;

        GameObject loopObject =
            new GameObject(
                "Weapon Charge Audio"
            );

        loopObject.transform.SetParent(transform);

        loopObject.transform.localPosition =
            Vector3.zero;

        chargeLoopSource =
            loopObject.AddComponent<AudioSource>();

        chargeLoopSource.playOnAwake = false;
        chargeLoopSource.loop = true;
    }
}