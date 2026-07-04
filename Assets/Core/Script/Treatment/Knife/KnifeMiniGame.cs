using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class KnifeMiniGame : MonoBehaviour
{
    [Serializable]
    private sealed class LesionSpawnOption
    {
        [SerializeField] private LesionType lesionType = LesionType.Tumor;
        [SerializeField] private Lesion lesionPrefab;

        public LesionType LesionType => lesionType;
        public Lesion LesionPrefab => lesionPrefab;
        public bool IsValid => lesionPrefab != null;
    }

    private sealed class RuntimeLesionAnchor
    {
        public RuntimeLesionAnchor(Transform spawnPoint, LesionSpawnAnchor anchor)
        {
            SpawnPoint = spawnPoint;
            Anchor = anchor;
        }

        public Transform SpawnPoint { get; }
        public LesionSpawnAnchor Anchor { get; }
        public bool IsConfigurable => Anchor != null;
        public bool IsValid => SpawnPoint != null;

        public Quaternion GetRotation(LesionCutOrientation orientation)
        {
            if (Anchor != null)
            {
                return Anchor.GetSpawnRotation(orientation);
            }

            return SpawnPoint != null ? SpawnPoint.rotation : Quaternion.identity;
        }
    }

    [SerializeField] private GameFlow flow;
    [SerializeField] private GameObject root;
    [SerializeField] private Transform lesionRoot;
    [SerializeField] private List<Lesion> lesions = new List<Lesion>();
    [Header("Spawn")]
    [SerializeField] private List<LesionSpawnOption> lesionSpawnOptions = new List<LesionSpawnOption>();
    [SerializeField] private List<LesionSpawnAnchor> lesionSpawnAnchors = new List<LesionSpawnAnchor>();
    [SerializeField] private List<Lesion> lesionPrefabs = new List<Lesion>();
    [SerializeField] private List<Transform> spawnAnchors = new List<Transform>();
    [SerializeField] private bool spawnLesionsOnBegin;
    [SerializeField, Min(1)] private int minLesions = 2;
    [SerializeField, Min(1)] private int maxLesions = 5;
    [Header("Input")]
    [SerializeField] private Camera inputCamera;
    [SerializeField] private float worldInputPlaneZ;
    [Header("UI")]
    [SerializeField] private MiniGameOverlay overlay;
    [SerializeField] private string overlayToolId = "Knife";
    [SerializeField] private string pullToolId = "Tongs";
    [Header("Obstacle")]
    [SerializeField] private PatientAggressionController aggressionController;
    [Header("Rules")]
    [SerializeField] private bool autoCompleteWhenAllLesionsDone = true;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool useUnscaledTime;
    [Header("Debug")]
    [SerializeField] private bool debugTreatmentFlow = true;

    private readonly List<Lesion> spawnedLesions = new List<Lesion>();
    private CustomerAgent activeCustomer;
    private Sanity activeSanity;
    private Lesion activeLesion;
    private Lesion meterLesion;
    private KnifeActionMode activeActionMode = KnifeActionMode.None;
    private KnifeToolState toolState = KnifeToolState.None;
    private bool unsupportedToolSelected;
    private bool isRunning;
    private bool isComplete;
    private bool completionRaised;
    private TreatmentBodyPrefab activeBodyPrefab;

    public bool IsRunning => isRunning;
    public bool IsComplete => isComplete;
    public KnifeToolState ToolState => toolState;

    public event Action MiniGameCompleted;

    public void ConfigureRuntimeContext(GameFlow runtimeFlow, Camera runtimeInputCamera, MiniGameOverlay runtimeOverlay)
    {
        LogTreatmentFlow($"ConfigureRuntimeContext flow={DescribeObject(runtimeFlow)} camera={DescribeObject(runtimeInputCamera)} overlay={DescribeObject(runtimeOverlay)}");
        if (runtimeFlow != null)
        {
            flow = runtimeFlow;
        }

        if (runtimeInputCamera != null)
        {
            inputCamera = runtimeInputCamera;
        }

        if (runtimeOverlay != null)
        {
            overlay = runtimeOverlay;
        }
    }

    private void Awake()
    {
        RefreshLesionList();
        SubscribeLesions();

        if (startHidden)
        {
            SetRootVisible(false);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeLesions();
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

        UpdateKnifeTipPreview(worldPointer);

        if (WasPrimaryPointerPressedThisFrame())
        {
            if (!IsPointerOverUi())
            {
                BeginActionAt(worldPointer);
            }
        }

        TickLesions(worldPointer, deltaTime);

        if (WasPrimaryPointerReleasedThisFrame())
        {
            EndAction();
        }
    }

    public void Begin(CustomerAgent customer)
    {
        LogTreatmentFlow($"Begin customer={DescribeObject(customer)} rootBefore={DescribeObject(root)} activeBody={DescribeBody(activeBodyPrefab)}");
        EnsureRootActiveForBegin();
        TryApplyParentBodyPrefab();

        activeCustomer = customer;
        activeSanity = customer != null ? customer.GetComponent<Sanity>() : null;
        activeLesion = null;
        meterLesion = null;
        activeActionMode = KnifeActionMode.None;
        toolState = KnifeToolState.None;
        unsupportedToolSelected = false;
        isRunning = true;
        isComplete = false;
        completionRaised = false;

        PrepareLesions();
        SubscribeLesions();
        ResetLesions();
        SetRootVisible(true);
        overlay?.Activate(CompleteMiniGame, HandleToolSelected, overlayToolId, pullToolId);
        RefreshCompletionState();
        RefreshMeters();
        BeginAggression();
        flow?.SetTreatmentStress(false);
    }

    public void ApplyBodyPrefab(TreatmentBodyPrefab body)
    {
        LogTreatmentFlow($"ApplyBodyPrefab body={DescribeBody(body)} currentRoot={DescribeObject(root)}");
        if (body == null)
        {
            LogTreatmentFlow("ApplyBodyPrefab skipped because body is null; using inspector anchors/fallback.");
            return;
        }

        UnsubscribeLesions();
        ClearSpawnedLesions();
        ReplaceRuntimeRoot(body);
        activeBodyPrefab = body;
        lesionRoot = body.LesionRoot != null ? body.LesionRoot : body.GameplayRoot;
        spawnAnchors.Clear();
        spawnAnchors.AddRange(body.GetLesionAnchorTransforms());
        lesionSpawnAnchors.Clear();

        for (int i = 0; i < spawnAnchors.Count; i++)
        {
            if (spawnAnchors[i] != null && spawnAnchors[i].TryGetComponent(out LesionSpawnAnchor anchor))
            {
                lesionSpawnAnchors.Add(anchor);
            }
        }

        LogTreatmentFlow($"ApplyBodyPrefab applied root={DescribeObject(root)} lesionRoot={DescribeObject(lesionRoot)} spawnAnchors={spawnAnchors.Count} lesionSpawnAnchors={lesionSpawnAnchors.Count}");
        RefreshLesionList();
        SubscribeLesions();
    }

    private void TryApplyParentBodyPrefab()
    {
        LogTreatmentFlow($"TryApplyParentBodyPrefab activeBody={DescribeBody(activeBodyPrefab)} parent={DescribeBody(GetComponentInParent<TreatmentBodyPrefab>(true))}");
        if (activeBodyPrefab != null)
        {
            return;
        }

        TreatmentBodyPrefab parentBody = GetComponentInParent<TreatmentBodyPrefab>(true);
        if (parentBody != null)
        {
            ApplyBodyPrefab(parentBody);
        }
    }

    public void Stop()
    {
        LogTreatmentFlow($"Stop root={DescribeObject(root)} activeBody={DescribeBody(activeBodyPrefab)}");
        EndAction();
        isRunning = false;
        activeBodyPrefab = null;
        aggressionController?.Stop();
        SetRootVisible(false);
        overlay?.Deactivate(CompleteMiniGame);
        flow?.SetTreatmentStress(false);
    }

    public void Pause()
    {
        LogTreatmentFlow($"Pause root={DescribeObject(root)} activeBody={DescribeBody(activeBodyPrefab)}");
        EndAction();
        isRunning = false;
        aggressionController?.Pause();
        SetRootVisible(false);
        overlay?.Deactivate(CompleteMiniGame);
        flow?.SetTreatmentStress(false);
    }

    public void Resume()
    {
        LogTreatmentFlow($"Resume root={DescribeObject(root)} activeBody={DescribeBody(activeBodyPrefab)}");
        SetRootVisible(true);
        overlay?.Activate(CompleteMiniGame, HandleToolSelected, overlayToolId, pullToolId);
        aggressionController?.Resume();

        if (isComplete)
        {
            return;
        }

        isRunning = true;
        RefreshMeters();
        flow?.SetTreatmentStress(false);
    }

    public void EquipKnife()
    {
        unsupportedToolSelected = false;
        toolState = KnifeToolState.Knife;
    }

    public void EquipTongs()
    {
        unsupportedToolSelected = false;
        toolState = KnifeToolState.Tongs;
    }

    public void PutKnifeDown()
    {
        unsupportedToolSelected = false;
        toolState = KnifeToolState.None;
        EndAction();
    }

    private void BeginAggression()
    {
        PatientAggressionProfile profile = ResolveAggressionProfile();
        if (profile == null || activeBodyPrefab == null || activeSanity == null)
        {
            aggressionController?.Stop();
            return;
        }

        if (aggressionController == null)
        {
            aggressionController = GetComponent<PatientAggressionController>();
        }

        if (aggressionController == null)
        {
            aggressionController = gameObject.AddComponent<PatientAggressionController>();
        }

        aggressionController.Begin(activeSanity, activeBodyPrefab.GameplayRoot, profile);
    }

    private PatientAggressionProfile ResolveAggressionProfile()
    {
        CustomerCaseProvider provider = activeCustomer != null ? activeCustomer.GetComponent<CustomerCaseProvider>() : null;
        return provider != null && provider.CaseData != null ? provider.CaseData.AggressionProfile : null;
    }

    private void HandleToolSelected(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            PutKnifeDown();
        }
        else if (toolId == overlayToolId)
        {
            EquipKnife();
        }
        else if (toolId == pullToolId)
        {
            EquipTongs();
        }
        else
        {
            unsupportedToolSelected = true;
            toolState = KnifeToolState.None;
            EndAction();
        }
    }

    public void CompleteMiniGame()
    {
        if (!isComplete)
        {
            return;
        }

        RaiseCompleted();
    }

    private void BeginActionAt(Vector2 pointerPosition)
    {
        Lesion target = FindLesionAt(pointerPosition);
        if (target == null)
        {
            return;
        }

        bool canUseTool = toolState == KnifeToolState.Knife && target.CanSlice;
        bool canUseTongs = toolState == KnifeToolState.Tongs && target.CanPull;
        if (!canUseTool && !canUseTongs)
        {
            return;
        }

        activeLesion = target;
        activeActionMode = canUseTool ? KnifeActionMode.Slice : KnifeActionMode.Pull;
        meterLesion = target;
        RefreshMeters();
        flow?.SetTreatmentStress(true);
    }

    private void EndAction()
    {
        activeLesion = null;
        activeActionMode = KnifeActionMode.None;
        flow?.SetTreatmentStress(false);
        RefreshMeters();
    }

    private void TickLesions(Vector2 worldPointer, float deltaTime)
    {
        for (int i = 0; i < lesions.Count; i++)
        {
            Lesion lesion = lesions[i];
            if (lesion == null || lesion.IsCompleted)
            {
                continue;
            }

            if (lesion == activeLesion)
            {
                if (activeActionMode == KnifeActionMode.Slice)
                {
                    lesion.TickSlice(worldPointer, deltaTime, activeSanity);
                }
                else if (activeActionMode == KnifeActionMode.Pull)
                {
                    lesion.TickPull(worldPointer, deltaTime, activeSanity);
                }
            }
            else
            {
                lesion.TickIdle(deltaTime);
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

        Camera cameraSource = inputCamera != null ? inputCamera : Camera.main;
        if (cameraSource == null)
        {
            worldPointer = screenPosition;
            return true;
        }

        float depth = Mathf.Abs(cameraSource.transform.position.z - worldInputPlaneZ);
        Vector3 world = cameraSource.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        worldPointer = world;
        return true;
    }

    private static bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            screenPosition = default;
            return false;
        }

        screenPosition = Mouse.current.position.ReadValue();
        return true;
#else
        screenPosition = default;
        return false;
#endif
    }

    private static bool WasPrimaryPointerPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return false;
#endif
    }

    private static bool WasPrimaryPointerReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
        return false;
#endif
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private Lesion FindLesionAt(Vector2 pointerPosition)
    {
        for (int i = lesions.Count - 1; i >= 0; i--)
        {
            Lesion lesion = lesions[i];
            if (lesion == null || lesion.IsCompleted)
            {
                continue;
            }

            bool containsPointer = lesion.ContainsPoint(pointerPosition);
            bool canStartSlice = toolState == KnifeToolState.Knife && lesion.CanBeginSliceAt(pointerPosition);
            if (containsPointer || canStartSlice)
            {
                return lesion;
            }
        }

        return null;
    }

    private void UpdateKnifeTipPreview(Vector2 worldPointer)
    {
        if (toolState != KnifeToolState.Knife)
        {
            return;
        }

        for (int i = 0; i < lesions.Count; i++)
        {
            Lesion lesion = lesions[i];
            if (lesion != null && !lesion.IsCompleted && lesion.CanSlice)
            {
                lesion.SetKnifeTipPreview(worldPointer);
            }
        }
    }

    private void PrepareLesions()
    {
        // Spawn whenever lesion prefabs + anchors exist, matching TongsMiniGame (which spawns on
        // configured anchors regardless of the spawn-on-begin flag). Falls back to pre-placed lesions.
        if (HasAnyLesionPrefabSource() && HasAnyLesionAnchorSource())
        {
            SpawnLesions();
            return;
        }

        RefreshLesionList();
    }

    private void SpawnLesions()
    {
        ClearSpawnedLesions();
        lesions.Clear();
        HideAllLesionSpawnVisuals();

        List<RuntimeLesionAnchor> anchors = GetShuffledAnchors();
        int low = Mathf.Max(1, minLesions);
        int high = Mathf.Max(low, maxLesions);
        int count = Mathf.Min(UnityEngine.Random.Range(low, high + 1), anchors.Count);
        Transform parent = lesionRoot != null ? lesionRoot : transform;
        List<Lesion> prefabBag = new List<Lesion>();

        for (int i = 0; i < count; i++)
        {
            RuntimeLesionAnchor anchor = anchors[i];
            if (!TryGetPrefabForAnchor(anchor, prefabBag, out Lesion prefab, out LesionType lesionType, out LesionCutOrientation orientation))
            {
                continue;
            }

            Lesion lesion = Instantiate(prefab, anchor.SpawnPoint.position, anchor.GetRotation(orientation), parent);
            lesion.name = $"{prefab.name}_{i + 1:00}_{lesionType}_{orientation}";
            lesions.Add(lesion);
            spawnedLesions.Add(lesion);
            anchor.Anchor?.ShowSpawnVisual(lesionType, orientation);
        }
    }

    private bool HasAnyLesionPrefabSource()
    {
        if (lesionSpawnOptions != null)
        {
            for (int i = 0; i < lesionSpawnOptions.Count; i++)
            {
                if (lesionSpawnOptions[i] != null && lesionSpawnOptions[i].IsValid)
                {
                    return true;
                }
            }
        }

        if (lesionPrefabs != null)
        {
            for (int i = 0; i < lesionPrefabs.Count; i++)
            {
                if (lesionPrefabs[i] != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasAnyLesionAnchorSource()
    {
        if (lesionSpawnAnchors != null)
        {
            for (int i = 0; i < lesionSpawnAnchors.Count; i++)
            {
                if (lesionSpawnAnchors[i] != null && lesionSpawnAnchors[i].IsUsable)
                {
                    return true;
                }
            }
        }

        if (spawnAnchors != null)
        {
            for (int i = 0; i < spawnAnchors.Count; i++)
            {
                if (spawnAnchors[i] != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetPrefabForAnchor(RuntimeLesionAnchor anchor, List<Lesion> prefabBag, out Lesion prefab, out LesionType lesionType, out LesionCutOrientation orientation)
    {
        prefab = null;
        lesionType = LesionType.Tumor;
        orientation = LesionCutOrientation.Horizontal;

        if (anchor == null || !anchor.IsValid)
        {
            return false;
        }

        if (anchor.IsConfigurable)
        {
            if (!anchor.Anchor.TryGetRandomLesionType(out lesionType) || !anchor.Anchor.TryGetRandomOrientation(out orientation))
            {
                return false;
            }

            return TryGetPrefabForType(lesionType, out prefab);
        }

        if (TryGetRandomTypedLesionPrefab(out lesionType, out prefab))
        {
            return true;
        }

        if (TryGetNextPrefab(prefabBag, out prefab))
        {
            lesionType = prefab.Type;
            return true;
        }

        return false;
    }

    private bool TryGetPrefabForType(LesionType lesionType, out Lesion prefab)
    {
        List<Lesion> candidates = new List<Lesion>();

        if (lesionSpawnOptions != null)
        {
            for (int i = 0; i < lesionSpawnOptions.Count; i++)
            {
                LesionSpawnOption option = lesionSpawnOptions[i];
                if (option != null && option.IsValid && option.LesionType == lesionType)
                {
                    candidates.Add(option.LesionPrefab);
                }
            }
        }

        if (candidates.Count == 0 && lesionPrefabs != null)
        {
            for (int i = 0; i < lesionPrefabs.Count; i++)
            {
                Lesion current = lesionPrefabs[i];
                if (current != null && current.Type == lesionType)
                {
                    candidates.Add(current);
                }
            }
        }

        if (candidates.Count == 0)
        {
            prefab = null;
            return false;
        }

        prefab = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return prefab != null;
    }

    private bool TryGetRandomTypedLesionPrefab(out LesionType lesionType, out Lesion prefab)
    {
        List<LesionSpawnOption> candidates = new List<LesionSpawnOption>();
        if (lesionSpawnOptions != null)
        {
            for (int i = 0; i < lesionSpawnOptions.Count; i++)
            {
                LesionSpawnOption option = lesionSpawnOptions[i];
                if (option != null && option.IsValid)
                {
                    candidates.Add(option);
                }
            }
        }

        if (candidates.Count == 0)
        {
            lesionType = LesionType.Tumor;
            prefab = null;
            return false;
        }

        LesionSpawnOption selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        lesionType = selected.LesionType;
        prefab = selected.LesionPrefab;
        return prefab != null;
    }

    private bool TryGetNextPrefab(List<Lesion> prefabBag, out Lesion prefab)
    {
        if (prefabBag.Count == 0)
        {
            for (int i = 0; i < lesionPrefabs.Count; i++)
            {
                if (lesionPrefabs[i] != null)
                {
                    prefabBag.Add(lesionPrefabs[i]);
                }
            }

            for (int i = prefabBag.Count - 1; i > 0; i--)
            {
                int swap = UnityEngine.Random.Range(0, i + 1);
                (prefabBag[i], prefabBag[swap]) = (prefabBag[swap], prefabBag[i]);
            }
        }

        if (prefabBag.Count == 0)
        {
            prefab = null;
            return false;
        }

        prefab = prefabBag[0];
        prefabBag.RemoveAt(0);
        return prefab != null;
    }

    private List<RuntimeLesionAnchor> GetShuffledAnchors()
    {
        List<RuntimeLesionAnchor> anchors = new List<RuntimeLesionAnchor>();

        if (lesionSpawnAnchors != null)
        {
            for (int i = 0; i < lesionSpawnAnchors.Count; i++)
            {
                AddRuntimeAnchor(anchors, lesionSpawnAnchors[i]);
            }
        }

        if (spawnAnchors != null)
        {
            for (int i = 0; i < spawnAnchors.Count; i++)
            {
                Transform spawnAnchor = spawnAnchors[i];
                if (spawnAnchor == null)
                {
                    continue;
                }

                LesionSpawnAnchor configurableAnchor = spawnAnchor.GetComponent<LesionSpawnAnchor>();
                if (configurableAnchor != null)
                {
                    AddRuntimeAnchor(anchors, configurableAnchor);
                }
                else if (!ContainsSpawnPoint(anchors, spawnAnchor))
                {
                    anchors.Add(new RuntimeLesionAnchor(spawnAnchor, null));
                }
            }
        }

        for (int i = anchors.Count - 1; i > 0; i--)
        {
            int swap = UnityEngine.Random.Range(0, i + 1);
            (anchors[i], anchors[swap]) = (anchors[swap], anchors[i]);
        }

        return anchors;
    }

    private static void AddRuntimeAnchor(List<RuntimeLesionAnchor> anchors, LesionSpawnAnchor anchor)
    {
        if (anchor == null || !anchor.IsUsable || ContainsSpawnPoint(anchors, anchor.SpawnPoint))
        {
            return;
        }

        anchors.Add(new RuntimeLesionAnchor(anchor.SpawnPoint, anchor));
    }

    private static bool ContainsSpawnPoint(List<RuntimeLesionAnchor> anchors, Transform spawnPoint)
    {
        for (int i = 0; i < anchors.Count; i++)
        {
            if (anchors[i] != null && anchors[i].SpawnPoint == spawnPoint)
            {
                return true;
            }
        }

        return false;
    }

    private void HideAllLesionSpawnVisuals()
    {
        if (lesionSpawnAnchors != null)
        {
            for (int i = 0; i < lesionSpawnAnchors.Count; i++)
            {
                lesionSpawnAnchors[i]?.HideSpawnVisuals();
            }
        }

        if (spawnAnchors == null)
        {
            return;
        }

        for (int i = 0; i < spawnAnchors.Count; i++)
        {
            if (spawnAnchors[i] != null && spawnAnchors[i].TryGetComponent(out LesionSpawnAnchor anchor))
            {
                anchor.HideSpawnVisuals();
            }
        }
    }

    private void RefreshLesionList()
    {
        Transform searchRoot = lesionRoot != null ? lesionRoot : transform;
        lesions.Clear();
        searchRoot.GetComponentsInChildren(includeInactive: true, lesions);
    }

    private void ClearSpawnedLesions()
    {
        for (int i = spawnedLesions.Count - 1; i >= 0; i--)
        {
            if (spawnedLesions[i] != null)
            {
                Destroy(spawnedLesions[i].gameObject);
            }
        }

        spawnedLesions.Clear();
    }

    private void ResetLesions()
    {
        for (int i = 0; i < lesions.Count; i++)
        {
            lesions[i]?.ResetRuntimeState();
        }
    }

    private void SubscribeLesions()
    {
        for (int i = 0; i < lesions.Count; i++)
        {
            if (lesions[i] == null)
            {
                continue;
            }

            lesions[i].Completed -= HandleLesionCompleted;
            lesions[i].PainChanged -= HandleMeterChanged;
            lesions[i].ProgressChanged -= HandleMeterChanged;
            lesions[i].Completed += HandleLesionCompleted;
            lesions[i].PainChanged += HandleMeterChanged;
            lesions[i].ProgressChanged += HandleMeterChanged;
        }
    }

    private void UnsubscribeLesions()
    {
        for (int i = 0; i < lesions.Count; i++)
        {
            if (lesions[i] == null)
            {
                continue;
            }

            lesions[i].Completed -= HandleLesionCompleted;
            lesions[i].PainChanged -= HandleMeterChanged;
            lesions[i].ProgressChanged -= HandleMeterChanged;
        }
    }

    private void HandleLesionCompleted(Lesion lesion)
    {
        if (meterLesion == lesion)
        {
            meterLesion = null;
        }

        RefreshCompletionState();
        RefreshMeters();
    }

    private void HandleMeterChanged(float value)
    {
        RefreshMeters();
    }

    private void RefreshCompletionState()
    {
        isComplete = HasAnyLesion() && AreAllLesionsCompleted();
        SetCompleteButtonVisible(isComplete);

        if (isComplete && autoCompleteWhenAllLesionsDone)
        {
            RaiseCompleted();
        }
    }

    private bool HasAnyLesion()
    {
        for (int i = 0; i < lesions.Count; i++)
        {
            if (lesions[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool AreAllLesionsCompleted()
    {
        for (int i = 0; i < lesions.Count; i++)
        {
            if (lesions[i] != null && !lesions[i].IsCompleted)
            {
                return false;
            }
        }

        return true;
    }

    private void RaiseCompleted()
    {
        if (completionRaised)
        {
            return;
        }

        completionRaised = true;
        Stop();
        MiniGameCompleted?.Invoke();
    }

    private void RefreshMeters()
    {
        float progress = meterLesion != null ? meterLesion.OverallProgress : 0f;
        float pain = meterLesion != null ? meterLesion.PainLevel : 0f;
        overlay?.SetMeters(progress, pain);
    }

    private void SetCompleteButtonVisible(bool visible)
    {
        overlay?.SetCompleteVisible(visible && !autoCompleteWhenAllLesionsDone);
    }

    private void SetRootVisible(bool visible)
    {
        GameObject target = root != null ? root : gameObject;
        LogTreatmentFlow($"SetRootVisible visible={visible} target={DescribeObject(target)}");
        target.SetActive(visible);
    }

    private void EnsureRootActiveForBegin()
    {
        // The mini game's own GameObject can be left inactive by startHidden in Awake (when root
        // still pointed at this object). After ApplyBodyPrefab reassigns root to the spawned body,
        // SetRootVisible only activates the body, so re-activate this object here or Update() never runs.
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        GameObject target = root != null ? root : gameObject;
        LogTreatmentFlow($"EnsureRootActiveForBegin target={DescribeObject(target)} activeBefore={(target != null && target.activeSelf)}");
        if (target != null && !target.activeSelf)
        {
            target.SetActive(true);
        }
    }

    private void ReplaceRuntimeRoot(TreatmentBodyPrefab body)
    {
        LogTreatmentFlow($"ReplaceRuntimeRoot oldRoot={DescribeObject(root)} newBody={DescribeBody(body)}");
        if (root != null && root != body.gameObject && root.TryGetComponent(out TreatmentBodyPrefab _))
        {
            root.SetActive(false);
        }

        root = body.gameObject;
    }

    private void LogTreatmentFlow(string message)
    {
        if (debugTreatmentFlow)
        {
            Debug.Log($"[TreatmentFlow][KnifeMiniGame] {message}", this);
        }
    }

    private static string DescribeObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return "null";
        }

        return $"{target.name} ({target.GetType().Name})";
    }

    private static string DescribeBody(TreatmentBodyPrefab body)
    {
        if (body == null)
        {
            return "null";
        }

        return $"{body.name} ({body.MiniGameType}/{body.Area}, active={body.gameObject.activeSelf})";
    }
}

public enum KnifeActionMode
{
    None,
    Slice,
    Pull
}
