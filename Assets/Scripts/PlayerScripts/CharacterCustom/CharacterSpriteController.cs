using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum SpriteSoundEventType
{
    Footstep,
    Attack,
    Skill,
    Dash,
    Hurt,
    Death
}

[Serializable]
public class SpriteFrameSoundEvent
{
    [Min(0)] public int frame;
    public SpriteSoundEventType sound;
}

[Serializable]
public class SpriteAnimationSoundTrack
{
    [Min(1)] public int frameCount = 1;
    public bool loop;
    public List<SpriteFrameSoundEvent> events = new List<SpriteFrameSoundEvent>();
}

public class CharacterSpriteController : MonoBehaviour
{
    [SerializeField]
    private PlayerWASD playerWASD;

    [SerializeField]
    private CharacterRenderer characterRenderer;

    [SerializeField]
    private PlayerSoundHelper playerSoundHelper;

    [Header("Sprite Animation")]
    [Min(0.01f)]
    [SerializeField] private float idleFramesPerSecond = 4f;
    [Min(0.01f)]
    [SerializeField] private float walkFramesPerSecond = 8f;
    [Min(0.01f)]
    [SerializeField] private float runningFramesPerSecond = 12f;
    [Min(0.01f)]
    [FormerlySerializedAs("attackFramesPerSecond")]
    [SerializeField] private float meleeFramesPerSecond = 10f;
    [Min(0.01f)]
    [FormerlySerializedAs("skillFramesPerSecond")]
    [SerializeField] private float castFramesPerSecond = 10f;
    [Min(0.01f)]
    [SerializeField] private float rangedFramesPerSecond = 10f;
    [Min(0.01f)]
    [SerializeField] private float dashFramesPerSecond = 14f;
    [Min(0.01f)]
    [SerializeField] private float deathFramesPerSecond = 6f;
    [Min(0f)]
    [FormerlySerializedAs("attackDuration")]
    [SerializeField] private float meleeDuration = 0.4f;
    [Min(0f)]
    [FormerlySerializedAs("skillDuration")]
    [SerializeField] private float castDuration = 0.4f;
    [Min(0f)]
    [SerializeField] private float rangedDuration = 0.4f;
    [Min(0f)]
    [SerializeField] private float dashDuration = 0.2f;

    [Header("Frame Sound Events")]
    [SerializeField] private SpriteAnimationSoundTrack idleSoundEvents;
    [SerializeField] private SpriteAnimationSoundTrack walkSoundEvents;
    [SerializeField] private SpriteAnimationSoundTrack runningSoundEvents;
    [FormerlySerializedAs("attackSoundEvents")]
    [SerializeField] private SpriteAnimationSoundTrack meleeSoundEvents;
    [FormerlySerializedAs("skillSoundEvents")]
    [SerializeField] private SpriteAnimationSoundTrack castSoundEvents;
    [SerializeField] private SpriteAnimationSoundTrack rangedSoundEvents;
    [SerializeField] private SpriteAnimationSoundTrack dashSoundEvents;
    [SerializeField] private SpriteAnimationSoundTrack deathSoundEvents;

    private CharacterAnimationState currentAnimationState;
    private float actionEndTime;
    private float stateStartTime;
    private bool actionActive;
    private int previousAnimationFrame = -1;

    private void Awake()
    {
        if (playerWASD == null)
            playerWASD = GetComponent<PlayerWASD>();

        if (characterRenderer == null)
            characterRenderer = GetComponent<CharacterRenderer>();

        if (playerSoundHelper == null)
            playerSoundHelper = GetComponent<PlayerSoundHelper>();

        stateStartTime = Time.time;
    }

    private void Update()
    {
        if (playerWASD == null || characterRenderer == null)
            return;

        CharacterDirection currentDirection = playerWASD.GetCurrentDirection();
        characterRenderer.CurrentDirection = currentDirection;

        if (actionActive && Time.time >= actionEndTime)
            actionActive = false;

        CharacterAnimationState nextState = actionActive
            ? currentAnimationState
            : playerWASD.MoveDirection.sqrMagnitude > 0.0001f
                ? playerWASD.IsSprinting
                    ? CharacterAnimationState.Running
                    : CharacterAnimationState.Walk
                : CharacterAnimationState.Idle;

        if (nextState != currentAnimationState)
            SetState(nextState);

        int frame = Mathf.FloorToInt(
            (Time.time - stateStartTime) * GetFramesPerSecond()
        );

        characterRenderer.SetAnimationState(currentAnimationState, frame);
        PlayFrameSoundEvents(frame);
    }

    public void PlayWeaponAction(WeaponAttackType weaponAttackType)
    {
        switch (weaponAttackType)
        {
            case WeaponAttackType.Melee:
                PlayAction(CharacterAnimationState.Melee, meleeDuration);
                break;
            case WeaponAttackType.Ranged:
                PlayAction(CharacterAnimationState.Ranged, rangedDuration);
                break;
            case WeaponAttackType.Spell:
                PlayAction(CharacterAnimationState.Cast, castDuration);
                break;
        }
    }

    public void PlayDash()
    {
        PlayAction(CharacterAnimationState.Dash, dashDuration);
    }

    public void PlayDeath()
    {
        actionActive = true;
        actionEndTime = float.PositiveInfinity;
        SetState(CharacterAnimationState.Death);
    }

    private void PlayAction(
        CharacterAnimationState animationState,
        float duration)
    {
        actionActive = true;
        actionEndTime = Time.time + duration;
        SetState(animationState);
    }

    private void SetState(CharacterAnimationState animationState)
    {
        if (currentAnimationState == animationState)
            return;

        currentAnimationState = animationState;
        stateStartTime = Time.time;
        previousAnimationFrame = -1;
    }

    private float GetFramesPerSecond()
    {
        return currentAnimationState switch
        {
            CharacterAnimationState.Idle => idleFramesPerSecond,
            CharacterAnimationState.Walk => walkFramesPerSecond,
            CharacterAnimationState.Running => runningFramesPerSecond,
            CharacterAnimationState.Melee => meleeFramesPerSecond,
            CharacterAnimationState.Cast => castFramesPerSecond,
            CharacterAnimationState.Ranged => rangedFramesPerSecond,
            CharacterAnimationState.Dash => dashFramesPerSecond,
            CharacterAnimationState.Death => deathFramesPerSecond,
            _ => walkFramesPerSecond
        };
    }

    private void PlayFrameSoundEvents(int animationFrame)
    {
        if (playerSoundHelper == null ||
            animationFrame == previousAnimationFrame)
            return;

        SpriteAnimationSoundTrack soundTrack = GetSoundTrack();
        if (soundTrack == null || soundTrack.events == null)
        {
            previousAnimationFrame = animationFrame;
            return;
        }

        int firstFrame = Mathf.Max(previousAnimationFrame + 1, 0);

        for (int enteredFrame = firstFrame;
             enteredFrame <= animationFrame;
             enteredFrame++)
        {
            if (!soundTrack.loop && enteredFrame >= soundTrack.frameCount)
                break;

            int soundFrame = soundTrack.loop
                ? enteredFrame % soundTrack.frameCount
                : enteredFrame;

            foreach (SpriteFrameSoundEvent soundEvent in soundTrack.events)
            {
                if (soundEvent != null && soundEvent.frame == soundFrame)
                    PlaySound(soundEvent.sound);
            }
        }

        previousAnimationFrame = animationFrame;
    }

    private SpriteAnimationSoundTrack GetSoundTrack()
    {
        return currentAnimationState switch
        {
            CharacterAnimationState.Idle => idleSoundEvents,
            CharacterAnimationState.Walk => walkSoundEvents,
            CharacterAnimationState.Running => runningSoundEvents,
            CharacterAnimationState.Melee => meleeSoundEvents,
            CharacterAnimationState.Cast => castSoundEvents,
            CharacterAnimationState.Ranged => rangedSoundEvents,
            CharacterAnimationState.Dash => dashSoundEvents,
            CharacterAnimationState.Death => deathSoundEvents,
            _ => null
        };
    }

    private void PlaySound(SpriteSoundEventType soundEvent)
    {
        switch (soundEvent)
        {
            case SpriteSoundEventType.Footstep:
                playerSoundHelper.PlayFootstep();
                break;
            case SpriteSoundEventType.Attack:
                playerSoundHelper.PlayAttack();
                break;
            case SpriteSoundEventType.Skill:
                playerSoundHelper.PlaySkill();
                break;
            case SpriteSoundEventType.Dash:
                playerSoundHelper.PlayDash();
                break;
            case SpriteSoundEventType.Hurt:
                playerSoundHelper.PlayHurt();
                break;
            case SpriteSoundEventType.Death:
                playerSoundHelper.PlayDeath();
                break;
        }
    }
}