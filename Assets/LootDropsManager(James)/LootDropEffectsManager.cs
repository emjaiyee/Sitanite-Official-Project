using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Plays the configured sprite animation and sound for a dropped item's rarity.
/// Add this component to a scene object and assign the effects in the Inspector.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Dimla/Inventory/Loot Drop Effects Manager")]
public class LootDropEffectsManager : MonoBehaviour
{
    [Serializable]
    private class RarityDropEffect
    {
        public ItemRarity rarity;
        [Tooltip("Sprites played in order when the item drops.")]
        public Sprite[] sprites = Array.Empty<Sprite>();
        [Min(0.01f)] public float framesPerSecond = 12f;
        [Tooltip("Sound played when the item drops.")]
        public AudioClip dropSound;
        [Range(0f, 1f)] public float soundVolume = 1f;
        [Min(0f)] public float spriteScale = 1f;
        public bool ignoreTimeScale;
    }

    public static LootDropEffectsManager Instance { get; private set; }

    [Header("Rarity Effects")]
    [SerializeField] private List<RarityDropEffect> rarityEffects =
        new List<RarityDropEffect>();

    [Header("Sprite Rendering")]
    [Tooltip("The effect is placed below the loot icon's sorting order.")]
    [SerializeField] private int sortingOrderOffset = -1;
    [SerializeField] private AudioMixerGroup audioMixerGroup;
    [SerializeField] private bool persistBetweenScenes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistBetweenScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Starts the configured rarity effects as children of the loot object.
    /// The child effect is therefore destroyed automatically with the loot.
    /// </summary>
    public void PlayDropEffects(
        ItemData itemData,
        Transform lootTransform,
        SpriteRenderer lootRenderer)
    {
        if (itemData == null || lootTransform == null)
            return;

        RarityDropEffect effect = FindEffect(itemData.Rarity);
        if (effect == null)
            return;

        if (effect.dropSound != null)
            PlaySound(effect, lootTransform.position);

        if (effect.sprites != null && effect.sprites.Length > 0)
        {
            GameObject visualObject = new GameObject(
                $"Loot Drop {effect.rarity} VFX"
            );
            visualObject.transform.SetParent(lootTransform, false);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localScale =
                Vector3.one * Mathf.Max(0f, effect.spriteScale);

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            if (lootRenderer != null)
            {
                renderer.sortingLayerID = lootRenderer.sortingLayerID;
                renderer.sortingOrder =
                    lootRenderer.sortingOrder + sortingOrderOffset;
            }

            StartCoroutine(PlaySprites(effect, visualObject, renderer));
        }
    }

    private RarityDropEffect FindEffect(ItemRarity rarity)
    {
        if (rarityEffects == null)
            return null;

        foreach (RarityDropEffect effect in rarityEffects)
        {
            if (effect != null && effect.rarity == rarity)
                return effect;
        }

        return null;
    }

    private void PlaySound(RarityDropEffect effect, Vector3 position)
    {
        GameObject soundObject = new GameObject($"Loot Drop {effect.rarity} SFX");
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = effect.dropSound;
        source.volume = Mathf.Clamp01(effect.soundVolume);
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = audioMixerGroup;
        source.Play();

        Destroy(soundObject, effect.dropSound.length);
    }

    private IEnumerator PlaySprites(
        RarityDropEffect effect,
        GameObject visualObject,
        SpriteRenderer renderer)
    {
        float frameDuration = 1f / Mathf.Max(0.01f, effect.framesPerSecond);
        int index = 0;
        while (visualObject != null && visualObject.transform.parent != null)
        {
            renderer.sprite = effect.sprites[index];
            yield return effect.ignoreTimeScale
                ? new WaitForSecondsRealtime(frameDuration)
                : new WaitForSeconds(frameDuration);

            index = (index + 1) % effect.sprites.Length;
        }
    }
}
