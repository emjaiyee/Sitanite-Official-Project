using UnityEngine;

[DisallowMultipleComponent]
public class FallenDescenderRenderer : MonoBehaviour
{
    [Header("Rotting Parts")]
    [SerializeField] private CharacterPartDefinition maleBody;
    [SerializeField] private CharacterPartDefinition femaleBody;
    [SerializeField] private CharacterPartDefinition maleEyes;
    [SerializeField] private CharacterPartDefinition femaleEyes;

    [Header("Paperdoll Layers")]
    [SerializeField] private SpriteRenderer weaponUnderRenderer;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer legsRenderer;
    [SerializeField] private SpriteRenderer torsoRenderer;
    [SerializeField] private SpriteRenderer eyesRenderer;
    [SerializeField] private SpriteRenderer hairRenderer;
    [SerializeField] private SpriteRenderer headwearRenderer;
    [SerializeField] private SpriteRenderer weaponOverRenderer;
    [SerializeField] private SpriteRenderer shieldRenderer;

    [Header("Animation")]
    [Min(1f)] [SerializeField] private float idleFramesPerSecond = 4f;
    [Min(1f)] [SerializeField] private float runFramesPerSecond = 12f;
    [Min(1f)] [SerializeField] private float attackFramesPerSecond = 10f;

    private readonly CharacterAppearance appearance = new CharacterAppearance();
    private CharacterDirection direction = CharacterDirection.SouthWest;
    private CharacterAnimationState state = CharacterAnimationState.Idle;
    private float stateStart;
    private float opacity = 1f;

    public bool CanRender(CharacterGender gender)
    {
        return bodyRenderer != null && eyesRenderer != null &&
            (gender == CharacterGender.Female ? femaleBody != null && femaleEyes != null : maleBody != null && maleEyes != null);
    }

    public void Configure(DungeonMemory.SavedDeath death, DungeonMemory memory)
    {
        appearance.gender = death.gender;
        appearance.body = death.gender == CharacterGender.Female ? femaleBody : maleBody;
        appearance.eyes = death.gender == CharacterGender.Female ? femaleEyes : maleEyes;
        appearance.hair = memory.ResolveHair(death.combat.hairId);
        appearance.hideHeadwear = death.combat.hideHeadwear;
        foreach (DungeonMemory.SavedEquipment item in death.combat.equipment)
        {
            CharacterPartDefinition part = memory.ResolveItem(item.itemId)?.CharacterDefinition;
            switch (item.slot)
            {
                case EquipmentType.Helmet: appearance.headwear = part as HeadwearDefinition; break;
                case EquipmentType.Chestplate: appearance.torso = part; break;
                case EquipmentType.Legging: appearance.legs = part; break;
                case EquipmentType.Weapon: appearance.weapon = part as WeaponDefinition; break;
                case EquipmentType.Shield: appearance.shield = part; break;
            }
        }
        stateStart = Time.time;
        Refresh();
    }

    public void SetState(CharacterAnimationState animation)
    {
        if (state == animation)
            return;

        state = animation;
        stateStart = Time.time;
        Refresh();
    }

    public void Face(Vector2 facing)
    {
        if (facing.sqrMagnitude < 0.0001f)
            return;

        int sector = (Mathf.RoundToInt(Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg / 45f) + 8) % 8;
        direction = sector switch
        {
            0 => CharacterDirection.East,
            1 => CharacterDirection.NorthEast,
            2 => CharacterDirection.North,
            3 => CharacterDirection.NorthWest,
            4 => CharacterDirection.West,
            5 => CharacterDirection.SouthWest,
            6 => CharacterDirection.South,
            _ => CharacterDirection.SouthEast
        };
    }

    private void LateUpdate()
    {
        Refresh();
    }

    public void SetOpacity(float alpha)
    {
        opacity = alpha;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }

    private void Refresh()
    {
        float rate = state == CharacterAnimationState.Idle ? idleFramesPerSecond :
            state == CharacterAnimationState.Running ? runFramesPerSecond : attackFramesPerSecond;
        int frame = Mathf.FloorToInt(Mathf.Max(0f, Time.time - stateStart) * rate);
        SetSprite(bodyRenderer, appearance.body, frame);
        SetSprite(eyesRenderer, appearance.eyes, frame);
        SetSprite(legsRenderer, appearance.legs, frame);
        SetSprite(torsoRenderer, appearance.torso, frame);
        bool hideHair = appearance.headwear != null && appearance.headwear.hidesHair && !appearance.hideHeadwear;
        SetSprite(hairRenderer, hideHair ? null : appearance.hair, frame);
        SetSprite(headwearRenderer, appearance.hideHeadwear ? null : appearance.headwear, frame);
        SetSprite(shieldRenderer, appearance.shield, frame);

        bool under = direction == CharacterDirection.SouthWest || direction == CharacterDirection.South ||
            direction == CharacterDirection.SouthEast || direction == CharacterDirection.East || direction == CharacterDirection.West;
        if (state == CharacterAnimationState.Melee || state == CharacterAnimationState.Cast)
            under = !under;
        SetSprite(weaponUnderRenderer, under ? appearance.weapon : null, frame);
        SetSprite(weaponOverRenderer, under ? null : appearance.weapon, frame);
    }

    private void SetSprite(SpriteRenderer renderer, CharacterPartDefinition part, int frame)
    {
        if (renderer == null)
            return;

        renderer.sprite = part != null ? part.GetSprite(direction, state, frame) : null;
        renderer.enabled = renderer.sprite != null;
        Color color = renderer.color;
        color.a = opacity;
        renderer.color = color;
    }
}