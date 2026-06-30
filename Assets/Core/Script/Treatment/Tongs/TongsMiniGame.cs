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
    [Header("Input")]
    [SerializeField] private Camera inputCamera;
    [SerializeField] private float worldInputPlaneZ;
    [Header("UI")]
    [SerializeField] private Slider pullProgressSlider;
    [SerializeField] private Slider painSlider;
    [SerializeField] private Button completeButton;
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
        if (completeButton != null)
        {
            completeButton.onClick.RemoveListener(CompleteMiniGame);
            completeButton.onClick.AddListener(CompleteMiniGame);
        }

        RefreshParasiteList();
        SubscribeParasites();
        SetCompleteButtonVisible(false);
        RefreshMeters();

        if (startHidden)
        {
            SetRootVisible(false);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeParasites();

        if (completeButton != null)
        {
            completeButton.onClick.RemoveListener(CompleteMiniGame);
        }
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
        SetCompleteButtonVisible(false);
        RefreshCompletionState();
        RefreshMeters();
        flow?.SetTreatmentStress(true);
    }

    public void Stop()
    {
        EndActiveHold();
        isRunning = false;
        SetRootVisible(false);
        flow?.SetTreatmentStress(false);
    }

    public void Pause()
    {
        EndActiveHold();
        isRunning = false;
        SetRootVisible(false);
        flow?.SetTreatmentStress(false);
    }

    public void Resume()
    {
        if (isComplete)
        {
            SetRootVisible(true);
            return;
        }

        isRunning = true;
        SetRootVisible(true);
        flow?.SetTreatmentStress(true);
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
        return collider != null && collider.OverlapPoint(worldPointer);
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
        if (spawnParasitesOnBegin && HasSpawnSource())
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

        Transform parent = parasiteRoot != null ? parasiteRoot : transform;

        for (int i = 0; i < count; i++)
        {
            if (!TryGetSpawnData(validSpawnOptions, spawnOptionBag, out ParasiteType type, out Parasite prefab))
            {
                break;
            }

            Parasite parasite = Instantiate(prefab, parent);
            parasite.name = $"{type.ParasiteVariant}_Parasite_{i + 1:00}";

            if (i < shuffledAnchors.Count)
            {
                ApplySpawnAnchor(parasite, shuffledAnchors[i]);
            }

            int direction = UnityEngine.Random.value < 0.5f ? -1 : 1;
            parasite.Configure(type, direction);
            parasites.Add(parasite);
            spawnedParasites.Add(parasite);
        }
    }

    private bool HasSpawnSource()
    {
        return HasValidSpawnOptions();
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
            return validAnchors;
        }

        for (int i = 0; i < spawnAnchors.Count; i++)
        {
            if (spawnAnchors[i] != null)
            {
                validAnchors.Add(spawnAnchors[i]);
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

    private void ApplySpawnAnchor(Parasite parasite, Transform anchor)
    {
        if (alignParasiteAnchorToSpawnPoint)
        {
            parasite.AlignSpawnAnchorToWorld(anchor.position);
            return;
        }

        parasite.transform.position = anchor.position;
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
        if (pullProgressSlider != null)
        {
            pullProgressSlider.value = meterParasite != null ? meterParasite.PullProgress : 0f;
        }

        if (painSlider != null)
        {
            painSlider.value = meterParasite != null ? meterParasite.PainLevel : 0f;
        }
    }

    private void SetCompleteButtonVisible(bool visible)
    {
        if (completeButton != null)
        {
            completeButton.gameObject.SetActive(visible);
        }
    }

    private void SetRootVisible(bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);
        }
    }
}
