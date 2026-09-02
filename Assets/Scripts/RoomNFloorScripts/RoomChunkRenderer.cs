using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RoomChunkRenderer : MonoBehaviour
{
    [Header("Chunk Settings")]
    [Min(1f)]
    [SerializeField] private float chunkSize = 16f;
    [Min(0)]
    [SerializeField] private int visibleChunkRadius = 1;
    [Min(0f)]
    [SerializeField] private float updateThreshold = 1f;

    [Header("Renderer Filtering")]
    [SerializeField] private bool includeInactiveChildren = true;
    [SerializeField] private bool includeParticleSystems;

    private readonly Dictionary<Vector2Int, List<Renderer>> renderersByChunk =
        new Dictionary<Vector2Int, List<Renderer>>();

    private readonly Dictionary<Renderer, bool> originalRendererStates =
        new Dictionary<Renderer, bool>();

    private Vector3 lastPlayerPosition;
    private Vector2Int lastPlayerChunk;
    private bool hasUpdated;

    private void Awake()
    {
        BuildChunkLookup();
    }

    private void LateUpdate()
    {
        if (Player.Instance == null)
            return;

        Vector3 playerPosition = Player.Instance.transform.position;

        if (
            hasUpdated &&
            Vector3.Distance(playerPosition, lastPlayerPosition) < updateThreshold
        )
            return;

        Vector2Int playerChunk = GetChunk(playerPosition);

        if (hasUpdated && playerChunk == lastPlayerChunk)
        {
            lastPlayerPosition = playerPosition;
            return;
        }

        UpdateVisibleChunks(playerChunk);
        lastPlayerChunk = playerChunk;
        lastPlayerPosition = playerPosition;
        hasUpdated = true;
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<Renderer, bool> entry in originalRendererStates)
        {
            if (entry.Key != null)
                entry.Key.enabled = entry.Value;
        }
    }

    private void BuildChunkLookup()
    {
        renderersByChunk.Clear();
        originalRendererStates.Clear();

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(includeInactiveChildren);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !ShouldTrackRenderer(renderer))
                continue;

            originalRendererStates[renderer] = renderer.enabled;

            Vector2Int chunk = GetChunk(renderer.bounds.center);

            if (!renderersByChunk.TryGetValue(chunk, out List<Renderer> chunkRenderers))
            {
                chunkRenderers = new List<Renderer>();
                renderersByChunk.Add(chunk, chunkRenderers);
            }

            chunkRenderers.Add(renderer);
        }
    }

    private bool ShouldTrackRenderer(Renderer renderer)
    {
        if (renderer is ParticleSystemRenderer && !includeParticleSystems)
            return false;

        return renderer.gameObject != gameObject;
    }

    private void UpdateVisibleChunks(Vector2Int playerChunk)
    {
        foreach (KeyValuePair<Renderer, bool> entry in originalRendererStates)
        {
            if (entry.Key != null)
                entry.Key.enabled = false;
        }

        for (int x = -visibleChunkRadius; x <= visibleChunkRadius; x++)
        {
            for (int y = -visibleChunkRadius; y <= visibleChunkRadius; y++)
            {
                Vector2Int chunk = playerChunk + new Vector2Int(x, y);

                if (!renderersByChunk.TryGetValue(chunk, out List<Renderer> chunkRenderers))
                    continue;

                foreach (Renderer renderer in chunkRenderers)
                {
                    if (renderer != null)
                        renderer.enabled = originalRendererStates[renderer];
                }
            }
        }
    }

    private Vector2Int GetChunk(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkSize),
            Mathf.FloorToInt(worldPosition.y / chunkSize)
        );
    }
}
