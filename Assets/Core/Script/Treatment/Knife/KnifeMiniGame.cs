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
    [SerializeField] private GameFlow flow;
    [SerializeField] private GameObject root;
    [SerializeField] private Transform lesionRoot;
    [SerializeField] private List<Lesion> lesions = new List<Lesion>();
    [Header("Spawn")]
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
    [Header("Rules")]
    [SerializeField] private bool autoCompleteWhenAllLesionsDone = true;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool useUnscaledTime;

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

    public bool IsRunning => isRunning;
    public bool IsComplete => isComplete;
    public KnifeToolState ToolState => toolState;

    public event Action MiniGameCompleted;

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
        overlay?.Activate(CompleteMiniGame, HandleToolSelected, overlayToolId);
        RefreshCompletionState();
        RefreshMeters();
        flow?.SetTreatmentStress(false);
    }

    public void Stop()
    {
        EndAction();
        isRunning = false;
        SetRootVisible(false);
        overlay?.Deactivate(CompleteMiniGame);
        flow?.SetTreatmentStress(false);
    }

    public void Pause()
    {
        EndAction();
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

    public void PutKnifeDown()
    {
        unsupportedToolSelected = false;
        toolState = KnifeToolState.None;
        EndAction();
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
        bool canUseHand = !unsupportedToolSelected && toolState == KnifeToolState.None && target.CanPull;
        if (!canUseTool && !canUseHand)
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
        if (spawnLesionsOnBegin && lesionPrefabs.Count > 0 && spawnAnchors.Count > 0)
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

        List<Transform> anchors = GetShuffledAnchors();
        int low = Mathf.Max(1, minLesions);
        int high = Mathf.Max(low, maxLesions);
        int count = Mathf.Min(UnityEngine.Random.Range(low, high + 1), anchors.Count);
        Transform parent = lesionRoot != null ? lesionRoot : transform;
        List<Lesion> prefabBag = new List<Lesion>();

        for (int i = 0; i < count; i++)
        {
            if (!TryGetNextPrefab(prefabBag, out Lesion prefab))
            {
                break;
            }

            Lesion lesion = Instantiate(prefab, anchors[i].position, anchors[i].rotation, parent);
            lesion.name = $"{prefab.name}_{i + 1:00}";
            lesions.Add(lesion);
            spawnedLesions.Add(lesion);
        }
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

    private List<Transform> GetShuffledAnchors()
    {
        List<Transform> anchors = new List<Transform>();
        for (int i = 0; i < spawnAnchors.Count; i++)
        {
            if (spawnAnchors[i] != null)
            {
                anchors.Add(spawnAnchors[i]);
            }
        }

        for (int i = anchors.Count - 1; i > 0; i--)
        {
            int swap = UnityEngine.Random.Range(0, i + 1);
            (anchors[i], anchors[swap]) = (anchors[swap], anchors[i]);
        }

        return anchors;
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
        target.SetActive(visible);
    }
}

public enum KnifeActionMode
{
    None,
    Slice,
    Pull
}
