using UnityEngine;
using UnityEngine.UI;

public class FloorGatewayDirectionIndicator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform arrowTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Canvas canvas;

    [Header("Appearance")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float screenEdgeInset = 64f;
    [SerializeField] private float onScreenVerticalOffset = 96f;

    private Camera gameplayCamera;
    private RoomManager roomManager;
    private PlayerRoomTracker roomTracker;

    private void Reset()
    {
        arrowTransform = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Awake()
    {
        if (arrowTransform == null)
            arrowTransform = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (arrowTransform == null || canvasGroup == null || canvas == null)
        {
            Debug.LogError(
                "FloorGatewayDirectionIndicator requires a RectTransform " +
                "CanvasGroup, and parent Canvas."
            );
            enabled = false;
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void LateUpdate()
    {
        FindDependencies();

        Gateway target = roomManager != null &&
            roomManager.IsFloorGatewayUnlocked
            ? roomManager.ActiveFloorGateway
            : null;

        float targetAlpha = target != null ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            targetAlpha,
            Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration)
        );

        if (target == null || gameplayCamera == null)
            return;

        Gateway navigationTarget = GetNavigationTarget(target);
        PositionArrow(navigationTarget.transform.position);
    }

    private void FindDependencies()
    {
        if (roomManager == null)
            roomManager = FindFirstObjectByType<RoomManager>();

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
    }

    private Gateway GetNavigationTarget(Gateway floorGateway)
    {
        RoomInstance floorRoom =
            floorGateway.GetComponentInParent<RoomInstance>();

        if (roomTracker == null && Player.Instance != null)
            roomTracker = Player.Instance.GetComponent<PlayerRoomTracker>();

        RoomInstance playerRoom = roomTracker != null
            ? roomTracker.CurrentRoom
            : roomManager.CurrentPlayerRoom;

        if (playerRoom == null || playerRoom == floorRoom)
            return floorGateway;

        GatewayFlow requiredFlow = playerRoom.RoomNumber < floorRoom.RoomNumber
            ? GatewayFlow.Forward
            : GatewayFlow.Backward;

        Gateway nextGateway = FindRoomGateway(
            playerRoom,
            requiredFlow
        );

        return nextGateway != null ? nextGateway : floorGateway;
    }

    private Gateway FindRoomGateway(
        RoomInstance room,
        GatewayFlow flow)
    {
        Gateway[] gateways = room.GetComponentsInChildren<Gateway>(true);

        foreach (Gateway gateway in gateways)
        {
            if (gateway.Flow == flow)
                return gateway;
        }

        return null;
    }

    private void PositionArrow(Vector3 targetPosition)
    {
        Vector3 screenPosition =
            gameplayCamera.WorldToScreenPoint(targetPosition);

        bool targetIsVisible = screenPosition.z > 0f &&
            screenPosition.x >= 0f && screenPosition.x <= Screen.width &&
            screenPosition.y >= 0f && screenPosition.y <= Screen.height;

        if (targetIsVisible)
        {
            SetArrowPosition(screenPosition +
                Vector3.up * onScreenVerticalOffset);
            arrowTransform.rotation = Quaternion.Euler(0f, 0f, 180f);
            return;
        }

        if (screenPosition.z < 0f)
            screenPosition *= -1f;

        Vector2 screenCenter = new Vector2(
            Screen.width * 0.5f,
            Screen.height * 0.5f
        );

        Vector2 direction =
            ((Vector2)screenPosition - screenCenter).normalized;

        float horizontalLimit = screenCenter.x - screenEdgeInset;
        float verticalLimit = screenCenter.y - screenEdgeInset;
        float scale = Mathf.Min(
            horizontalLimit / Mathf.Max(Mathf.Abs(direction.x), 0.001f),
            verticalLimit / Mathf.Max(Mathf.Abs(direction.y), 0.001f)
        );

        SetArrowPosition(screenCenter + direction * scale);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg -
            90f;
        arrowTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SetArrowPosition(Vector2 screenPosition)
    {
        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                arrowTransform.parent as RectTransform,
                screenPosition,
                canvasCamera,
                out Vector2 localPosition))
        {
            arrowTransform.anchoredPosition = localPosition;
        }
    }
}