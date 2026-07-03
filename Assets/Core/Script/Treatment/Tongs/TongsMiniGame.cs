using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class TongsMiniGame : MonoBehaviour
{
    [Serializable]
    private sealed class ParasiteSpawnOption
    {
        [SerializeField] private ParasiteType parasiteType;
        [SerializeField] private Parasite parasitePrefab;

        public ParasiteType ParasiteType => parasiteType;
        public Parasite ParasitePrefab => parasitePrefab;
        public bool IsValid => parasiteType != null && parasitePrefab != null;
    }

    [SerializeField] private GameFlow flow;
    [SerializeField] private GameObject root;
    [SerializeField] private Transform parasiteRoot;
    [HideInInspector, SerializeField] private List<Parasite> parasites = new List<Parasite>();
    [Header("Spawn")]
    [SerializeField] private List<ParasiteSpawnOption> parasiteSpawnOptions = new List<ParasiteSpawnOption>();
    [SerializeField] private List<Transform> spawnAnchors = new List<Transform>();
    [FormerlySerializedAs("alignParasiteTopToSpawnAnchor")]
    [SerializeField] private bool alignParasiteAnchorToSpawnPoint = true;
    [SerializeField] private bool spawnParasitesOnBegin;
    [SerializeField, Min(1)] private int minParasites = 3;
    [SerializeField, Min(1)] private int maxParasites = 7;
    [SerializeField] private int parasiteSortingOrderStart = 27;
    [SerializeField, Min(2)] private int parasiteSortingOrderStep = 4;
    [SerializeField] private bool debugSpawn = true;
    [Header("Input")]
    [SerializeField] private Camera inputCamera;
    [SerializeField] private float worldInputPlaneZ;
    [Header("UI")]
    [SerializeField] private MiniGameOverlay overlay;
    [SerializeField] private string overlayToolId = "Tongs";
    [Header("Rules")]
    [SerializeField] private bool requireTongsEquipped = true;
    [SerializeField] private bool autoFindParasitesInChildren = true;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool completeTreatmentOnButton;
    [SerializeField] private bool useUnscaledTime;

    private CustomerAgent activeCustomer;
    private Sanity activeSanity;
    private Parasite activeParasite;
    private Parasite meterParasite;
    private readonly List<Parasite> spawnedParasites = new List<Parasite>();
    private bool isRunning;
    private bool isComplete;
    private bool tongsEquipped;

    public bool IsRunning => isRunning;
    public bool IsComplete => isComplete;
    public bool TongsEquipped => tongsEquipped;
    public Parasite ActiveParasite => activeParasite;

    public event Action MiniGameCompleted;

    private void Awake()
    {
        RefreshParasiteList();
        SubscribeParasites();

        if (startHidden)
        {
            SetRootVisible(false);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeParasites();
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        if (!TryGetPointerWorldPosition(out Vector2 worldPointer))
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (WasPrimaryPointerPressedThisFrame())
        {
            BeginHoldAt(worldPointer);
        }

        TickParasites(worldPointer, deltaTime);

        if (WasPrimaryPointerReleasedThisFrame())
        {
            EndActiveHold();
        }
    }

    public void Begin(CustomerAgent customer)
    {
        activeCustomer = customer;
        activeSanity = customer != null ? customer.GetComponent<Sanity>() : null;
        activeParasite = null;
        meterParasite = null;
        isRunning = true;
        isComplete = false;
        tongsEquipped = !requireTongsEquipped;

        PrepareParasites();
        SubscribeParasites();
        ResetParasites();
        SetRootVisible(true);
        overlay?.Activate(CompleteMiniGame, HandleToolSelected, overlayToolId);
        RefreshCompletionState();
        RefreshMeters();
        flow?.SetTreatmentStress(false);
    }

    public void Stop()
    {
        EndActiveHold();
        isRunning = false;
        SetRootVisible(false);
        overlay?.Deactivate(CompleteMiniGame);
        flow?.SetTreatmentStress(false);
    }

    public void Pause()
    {
        EndActiveHold();
        isRunning = false;
        SetRootVisible(false);
        overlay?.Deactivate(CompleteMiniGame);
        flow?.SetTreatmentStress(false);
    }

    public void Resume()
    {
        SetRootVisible(true);
        overlay?.Activate(CompleteMiniGame, HandleToolSelected, overlayToolId);

        if (isComplete)
        {
            SetCompleteButtonVisible(true);
            return;
        }

        isRunning = true;
        RefreshMeters();
        flow?.SetTreatmentStress(false);
    }

    private void HandleToolSelected(string toolId)
    {
        if (!requireTongsEquipped)
        {
            return;
        }

        SetTongsEquipped(toolId == overlayToolId);
    }

    public void EquipTongs()
    {
        tongsEquipped = true;
    }

    public void UnequipTongs()
    {
        tongsEquipped = false;
        EndActiveHold();
    }

    public void SetTongsEquipped(bool equipped)
    {
        if (equipped)
        {
            EquipTongs();
        }
        else
        {
            UnequipTongs();
        }
    }

    public void CompleteMiniGame()
    {
        if (!isComplete)
        {
            return;
        }

        Stop();

        if (completeTreatmentOnButton)
        {
            flow?.CompleteTreatment(activeCustomer);
            return;
        }

        MiniGameCompleted?.Invoke();
    }

    private void BeginHoldAt(Vector2 pointerPosition)
    {
        if (requireTongsEquipped && !tongsEquipped)
        {
            return;
        }

        Parasite target = FindParasiteAt(pointerPosition);
        if (target == null)
        {
            return;
        }

        EndActiveHold();
        activeParasite = target;
        meterParasite = target;
        activeParasite.BeginHold(pointerPosition);
        flow?.SetTreatmentStress(true);
        RefreshMeters();
    }

    private void EndActiveHold()
    {
        if (activeParasite != null)
        {
            activeParasite.EndHold();
        }

        activeParasite = null;
        flow?.SetTreatmentStress(false);
        RefreshMeters();
    }

    private void TickParasites(Vector2 worldPointer, float deltaTime)
    {
        for (int i = 0; i < parasites.Count; i++)
        {
            if (parasites[i] != null)
            {
                parasites[i].TickPull(worldPointer, deltaTime);
            }
        }
    }

    private bool TryGetPointerWorldPosition(out Vector2 worldPointer)
    {
        if (!TryGetPointerScreenPosition(out Vector2 screenPosition))
        {
            worldPointer = default;
            return false;
        }

        Camera camera = inputCamera != null ? inputCamera : Camera.main;
        if (camera == null)
        {
            worldPointer = default;
            return false;
        }

        float distanceFromCamera = worldInputPlaneZ - camera.transform.position.z;
        Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));
        worldPointer = worldPosition;
        return true;
    }

    private static bool TryGetPointerScreenPosition(out Vector2 pointerPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            pointerPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null)
        {
            pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
#endif

        pointerPosition = default;
        return false;
    }

    private static bool WasPrimaryPointerPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }
#endif

        return false;
    }

    private static bool WasPrimaryPointerReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            return true;
        }
#endif

        return false;
    }

    private Parasite FindParasiteAt(Vector2 worldPointer)
    {
        for (int i = parasites.Count - 1; i >= 0; i--)
        {
            Parasite parasite = parasites[i];
            if (parasite == null || parasite.IsExtracted || !parasite.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (ContainsPointer(parasite, worldPointer))
            {
                return parasite;
            }
        }

        return null;
    }

    private static bool ContainsPointer(Parasite parasite, Vector2 worldPointer)
    {
        Collider2D collider = parasite.GetComponent<Collider2D>();
        if (collider != null && collider.OverlapPoint(worldPointer))
        {
            return true;
        }

        Collider2D[] childColliders = parasite.GetComponentsInChildren<Collider2D>(false);
        for (int i = 0; i < childColliders.Length; i++)
        {
            if (childColliders[i] != null && childColliders[i].OverlapPoint(worldPointer))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshParasiteList()
    {
        if (!autoFindParasitesInChildren)
        {
            return;
        }

        Transform searchRoot = parasiteRoot != null ? parasiteRoot : transform;
        parasites.Clear();
        searchRoot.GetComponentsInChildren(includeInactive: true, parasites);
    }

    private void PrepareParasites()
    {
        if (ShouldSpawnParasitesOnBegin())
        {
            SpawnParasites();
            return;
        }

        RefreshParasiteList();
    }

    private void SpawnParasites()
    {
        ClearSpawnedParasites();
        parasites.Clear();

        List<ParasiteSpawnOption> validSpawnOptions = GetValidSpawnOptions();
        List<ParasiteSpawnOption> spawnOptionBag = new List<ParasiteSpawnOption>();
        int low = Mathf.Max(1, minParasites);
        int high = Mathf.Max(low, maxParasites);
        int count = UnityEngine.Random.Range(low, high + 1);
        List<Transform> shuffledAnchors = GetShuffledSpawnAnchors();
        if (shuffledAnchors.Count > 0)
        {
            count = Mathf.Min(count, shuffledAnchors.Count);
        }

        LogSpawn($"Begin spawn. validOptions={validSpawnOptions.Count}, rawAnchors={CountConfiguredSpawnAnchors()}, effectiveAnchors={shuffledAnchors.Count}, min={minParasites}, max={maxParasites}, finalCount={count}, alignAnchor={alignParasiteAnchorToSpawnPoint}.");
        LogSpawnAnchors(shuffledAnchors);
        HideSpawnVisuals(shuffledAnchors);

        Transform parent = parasiteRoot != null ? parasiteRoot : transform;

        for (int i = 0; i < count; i++)
        {
            if (!TryGetSpawnData(validSpawnOptions, spawnOptionBag, out ParasiteType type, out Parasite prefab))
            {
                LogSpawnWarning($"Spawn stopped at index {i}. No valid parasite type/prefab pair was available.");
                break;
            }

            Parasite parasite = Instantiate(prefab, parent);
            parasite.name = $"{type.ParasiteVariant}_Parasite_{i + 1:00}";
            Transform selectedAnchor = i < shuffledAnchors.Count ? shuffledAnchors[i] : null;
            ParasiteSpawnAnchor spawnAnchor = GetSpawnAnchorData(selectedAnchor);
            int direction = spawnAnchor != null ? spawnAnchor.RequiredDirection : UnityEngine.Random.value < 0.5f ? -1 : 1;
            parasite.Configure(type, direction);

            if (selectedAnchor != null)
            {
                LogSpawn($"Spawn #{i + 1}: prefab='{prefab.name}', type='{type.name}', selectedAnchor='{GetTransformPath(selectedAnchor)}', selectedAnchorPosition={FormatVector(selectedAnchor.position)}, spawnAnchorComponent={(spawnAnchor != null ? spawnAnchor.name : "none")}, resolvedSpawnPosition={(spawnAnchor != null ? FormatVector(spawnAnchor.SpawnPosition) : FormatVector(selectedAnchor.position))}, requiredDirection={direction}.", selectedAnchor);
                ApplySpawnAnchor(parasite, selectedAnchor, i, type);
            }
            else
            {
                LogSpawnWarning($"Spawn #{i + 1}: no selected anchor. Parasite remains at instantiated local/default position {FormatVector(parasite.transform.position)}.", parasite);
            }

            parasites.Add(parasite);
            spawnedParasites.Add(parasite);
        }
    }

    private bool HasSpawnSource()
    {
        return HasValidSpawnOptions();
    }

    private bool ShouldSpawnParasitesOnBegin()
    {
        if (!HasSpawnSource())
        {
            return false;
        }

        return spawnParasitesOnBegin || HasConfiguredSpawnAnchors();
    }

    private bool HasConfiguredSpawnAnchors()
    {
        if (spawnAnchors == null)
        {
            return false;
        }

        for (int i = 0; i < spawnAnchors.Count; i++)
        {
            if (spawnAnchors[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasValidSpawnOptions()
    {
        if (parasiteSpawnOptions == null)
        {
            return false;
        }

        for (int i = 0; i < parasiteSpawnOptions.Count; i++)
        {
            if (parasiteSpawnOptions[i] != null && parasiteSpawnOptions[i].IsValid)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetSpawnData(List<ParasiteSpawnOption> validOptions, List<ParasiteSpawnOption> spawnOptionBag, out ParasiteType type, out Parasite prefab)
    {
        if (validOptions.Count > 0)
        {
            if (spawnOptionBag.Count == 0)
            {
                FillShuffledSpawnOptionBag(validOptions, spawnOptionBag);
            }

            ParasiteSpawnOption option = spawnOptionBag[0];
            spawnOptionBag.RemoveAt(0);
            type = option.ParasiteType;
            prefab = option.ParasitePrefab;
            return true;
        }

        type = null;
        prefab = null;
        return false;
    }

    private static void FillShuffledSpawnOptionBag(List<ParasiteSpawnOption> source, List<ParasiteSpawnOption> destination)
    {
        destination.Clear();
        destination.AddRange(source);

        for (int i = destination.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            (destination[i], destination[swapIndex]) = (destination[swapIndex], destination[i]);
        }
    }

    private List<ParasiteSpawnOption> GetValidSpawnOptions()
    {
        List<ParasiteSpawnOption> validOptions = new List<ParasiteSpawnOption>();
        if (parasiteSpawnOptions == null)
        {
            return validOptions;
        }

        for (int i = 0; i < parasiteSpawnOptions.Count; i++)
        {
            if (parasiteSpawnOptions[i] != null && parasiteSpawnOptions[i].IsValid)
            {
                validOptions.Add(parasiteSpawnOptions[i]);
            }
        }

        return validOptions;
    }

    private List<Transform> GetShuffledSpawnAnchors()
    {
        List<Transform> validAnchors = new List<Transform>();
        if (spawnAnchors == null || spawnAnchors.Count == 0)
        {
            LogSpawnWarning("No spawn anchors are assigned in TongsMiniGame.Spawn Anchors.");
            return validAnchors;
        }

        for (int i = 0; i < spawnAnchors.Count; i++)
        {
            if (spawnAnchors[i] != null)
            {
                AddSpawnAnchorOrChildren(spawnAnchors[i], validAnchors);
            }
            else
            {
                LogSpawnWarning($"Spawn Anchors element {i} is null.");
            }
        }

        for (int i = validAnchors.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            Transform temp = validAnchors[i];
            validAnchors[i] = validAnchors[swapIndex];
            validAnchors[swapIndex] = temp;
        }

        return validAnchors;
    }

    private static void AddSpawnAnchorOrChildren(Transform anchor, List<Transform> validAnchors)
    {
        if (anchor == null)
        {
            return;
        }

        if (GetSpawnAnchorData(anchor) != null || anchor.childCount == 0 || !IsSpawnAnchorGroup(anchor))
        {
            AddUniqueSpawnAnchor(anchor, validAnchors);
            return;
        }

        for (int i = 0; i < anchor.childCount; i++)
        {
            Transform child = anchor.GetChild(i);
            if (child != null)
            {
                AddUniqueSpawnAnchor(child, validAnchors);
            }
        }
    }

    private static void AddUniqueSpawnAnchor(Transform anchor, List<Transform> validAnchors)
    {
        if (anchor != null && !validAnchors.Contains(anchor))
        {
            validAnchors.Add(anchor);
        }
    }

    private static bool IsSpawnAnchorGroup(Transform anchor)
    {
        string anchorName = anchor.name.ToLowerInvariant();
        return anchorName.Contains("spawn points")
            || anchorName.Contains("spawn anchors")
            || anchorName.Contains("spawnanchors");
    }

    private void ApplySpawnAnchor(Parasite parasite, Transform anchor, int spawnIndex, ParasiteType parasiteType)
    {
        ParasiteSpawnAnchor spawnAnchor = GetSpawnAnchorData(anchor);
        spawnAnchor?.ShowSpawnVisual(parasiteType);
        parasite.BindSpawnAnchor(spawnAnchor, anchor, GetFallbackMaskSprite(anchor), parasiteSortingOrderStart + spawnIndex * parasiteSortingOrderStep);
        Vector3 spawnPosition = spawnAnchor != null ? spawnAnchor.SpawnPosition : anchor.position;
        Vector3 beforeRootPosition = parasite.transform.position;
        Vector3 beforePrefabAnchorPosition = parasite.SpawnAnchorWorldPosition;

        if (alignParasiteAnchorToSpawnPoint)
        {
            parasite.AlignSpawnAnchorToWorld(spawnPosition);
            LogSpawn($"Applied anchor alignment for '{parasite.name}'. beforeRoot={FormatVector(beforeRootPosition)}, beforePrefabSpawnAnchor={FormatVector(beforePrefabAnchorPosition)}, targetSpawn={FormatVector(spawnPosition)}, afterRoot={FormatVector(parasite.transform.position)}, afterPrefabSpawnAnchor={FormatVector(parasite.SpawnAnchorWorldPosition)}, rootDelta={FormatVector(parasite.transform.position - beforeRootPosition)}.", parasite);
            return;
        }

        parasite.transform.position = spawnPosition;
        parasite.RebaseVisualPosition();
        LogSpawn($"Applied root-position spawn for '{parasite.name}'. beforeRoot={FormatVector(beforeRootPosition)}, targetSpawn={FormatVector(spawnPosition)}, afterRoot={FormatVector(parasite.transform.position)}, prefabSpawnAnchorNow={FormatVector(parasite.SpawnAnchorWorldPosition)}.", parasite);
    }

    private static ParasiteSpawnAnchor GetSpawnAnchorData(Transform anchor)
    {
        if (anchor == null)
        {
            return null;
        }

        ParasiteSpawnAnchor spawnAnchor = anchor.GetComponent<ParasiteSpawnAnchor>();
        return spawnAnchor != null ? spawnAnchor : anchor.GetComponentInParent<ParasiteSpawnAnchor>();
    }

    private static Sprite GetFallbackMaskSprite(Transform anchor)
    {
        if (anchor == null)
        {
            return null;
        }

        SpriteRenderer spriteRenderer = anchor.GetComponentInChildren<SpriteRenderer>(true);
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private static void HideSpawnVisuals(List<Transform> anchors)
    {
        if (anchors == null)
        {
            return;
        }

        for (int i = 0; i < anchors.Count; i++)
        {
            ParasiteSpawnAnchor spawnAnchor = GetSpawnAnchorData(anchors[i]);
            if (spawnAnchor != null)
            {
                spawnAnchor.HideSpawnVisuals();
            }
        }
    }

    private int CountConfiguredSpawnAnchors()
    {
        if (spawnAnchors == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < spawnAnchors.Count; i++)
        {
            if (spawnAnchors[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void LogSpawnAnchors(List<Transform> anchors)
    {
        if (!debugSpawn)
        {
            return;
        }

        if (anchors == null || anchors.Count == 0)
        {
            LogSpawnWarning("Effective anchor list is empty. Parasites will not align to authored spawn points.");
            return;
        }

        for (int i = 0; i < anchors.Count; i++)
        {
            Transform anchor = anchors[i];
            ParasiteSpawnAnchor spawnAnchor = GetSpawnAnchorData(anchor);
            string source = spawnAnchor != null
                ? $"ParasiteSpawnAnchor='{spawnAnchor.name}', spawnPosition={FormatVector(spawnAnchor.SpawnPosition)}, usesCustomSpawnPoint={(spawnAnchor.transform != anchor ? "parent/child lookup" : "direct")}"
                : "Transform fallback";
            LogSpawn($"Effective anchor {i}: '{GetTransformPath(anchor)}', transformPosition={FormatVector(anchor.position)}, {source}.", anchor);
        }
    }

    private void LogSpawn(string message, UnityEngine.Object context = null)
    {
        if (!debugSpawn)
        {
            return;
        }

        Debug.Log($"[TongsSpawn] {message}", context != null ? context : this);
    }

    private void LogSpawnWarning(string message, UnityEngine.Object context = null)
    {
        if (!debugSpawn)
        {
            return;
        }

        Debug.LogWarning($"[TongsSpawn] {message}", context != null ? context : this);
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:0.###}, {value.y:0.###}, {value.z:0.###})";
    }

    private static string GetTransformPath(Transform transform)
    {
        if (transform == null)
        {
            return "null";
        }

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = $"{parent.name}/{path}";
            parent = parent.parent;
        }

        return path;
    }

    private void ClearSpawnedParasites()
    {
        for (int i = 0; i < spawnedParasites.Count; i++)
        {
            if (spawnedParasites[i] != null)
            {
                Destroy(spawnedParasites[i].gameObject);
            }
        }

        spawnedParasites.Clear();
    }

    private void SubscribeParasites()
    {
        for (int i = 0; i < parasites.Count; i++)
        {
            SubscribeParasite(parasites[i]);
        }
    }

    private void UnsubscribeParasites()
    {
        for (int i = 0; i < parasites.Count; i++)
        {
            UnsubscribeParasite(parasites[i]);
        }
    }

    private void SubscribeParasite(Parasite parasite)
    {
        if (parasite == null)
        {
            return;
        }

        parasite.Extracted -= HandleParasiteExtracted;
        parasite.PullProgressChanged -= HandleParasiteMeterChanged;
        parasite.PainChanged -= HandleParasiteMeterChanged;
        parasite.SanitySpikeRequested -= HandleSanitySpikeRequested;
        parasite.ContinuousSanityRequested -= HandleContinuousSanityRequested;

        parasite.Extracted += HandleParasiteExtracted;
        parasite.PullProgressChanged += HandleParasiteMeterChanged;
        parasite.PainChanged += HandleParasiteMeterChanged;
        parasite.SanitySpikeRequested += HandleSanitySpikeRequested;
        parasite.ContinuousSanityRequested += HandleContinuousSanityRequested;
    }

    private void UnsubscribeParasite(Parasite parasite)
    {
        if (parasite == null)
        {
            return;
        }

        parasite.Extracted -= HandleParasiteExtracted;
        parasite.PullProgressChanged -= HandleParasiteMeterChanged;
        parasite.PainChanged -= HandleParasiteMeterChanged;
        parasite.SanitySpikeRequested -= HandleSanitySpikeRequested;
        parasite.ContinuousSanityRequested -= HandleContinuousSanityRequested;
    }

    private void ResetParasites()
    {
        for (int i = 0; i < parasites.Count; i++)
        {
            if (parasites[i] != null)
            {
                parasites[i].ResetRuntimeState();
            }
        }
    }

    private void HandleParasiteExtracted(Parasite parasite)
    {
        if (activeParasite == parasite)
        {
            activeParasite = null;
        }

        if (meterParasite == parasite)
        {
            meterParasite = null;
        }

        RefreshMeters();
        RefreshCompletionState();
    }

    private void HandleParasiteMeterChanged(float _)
    {
        RefreshMeters();
    }

    private void HandleSanitySpikeRequested(float amount)
    {
        activeSanity?.AddSanity(amount);
    }

    private void HandleContinuousSanityRequested(float amount)
    {
        activeSanity?.AddSanity(amount);
    }

    private void RefreshCompletionState()
    {
        isComplete = HasAnyParasite() && AreAllParasitesExtracted();
        SetCompleteButtonVisible(isComplete);
    }

    private bool HasAnyParasite()
    {
        for (int i = 0; i < parasites.Count; i++)
        {
            if (parasites[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool AreAllParasitesExtracted()
    {
        for (int i = 0; i < parasites.Count; i++)
        {
            if (parasites[i] != null && !parasites[i].IsExtracted)
            {
                return false;
            }
        }

        return true;
    }

    private void RefreshMeters()
    {
        float progress = meterParasite != null ? meterParasite.PullProgress : 0f;
        float pain = meterParasite != null ? meterParasite.PainLevel : 0f;
        overlay?.SetMeters(progress, pain);
    }

    private void SetCompleteButtonVisible(bool visible)
    {
        overlay?.SetCompleteVisible(visible);
    }

    private void SetRootVisible(bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);
        }
    }
}
