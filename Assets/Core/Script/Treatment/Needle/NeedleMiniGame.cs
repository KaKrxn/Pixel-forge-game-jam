using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class NeedleMiniGame : MonoBehaviour
{
    [SerializeField] private GameFlow flow;
    [SerializeField] private GameObject root;
    [SerializeField] private Transform pustuleRoot;
    [SerializeField] private List<Pustule> pustules = new List<Pustule>();
    [Header("Spawn")]
    [SerializeField] private List<Pustule> pustulePrefabs = new List<Pustule>();
    [SerializeField] private List<Transform> spawnAnchors = new List<Transform>();
    [SerializeField] private bool spawnPustulesOnBegin;
    [SerializeField, Min(1)] private int minPustules = 3;
    [SerializeField, Min(1)] private int maxPustules = 7;
    [Header("Input")]
    [SerializeField] private Camera inputCamera;
    [SerializeField] private float worldInputPlaneZ;
    [Header("UI")]
    [SerializeField] private MiniGameOverlay overlay;
    [SerializeField] private string overlayToolId = "Needle";
    [Header("Rules")]
    [SerializeField] private bool autoCompleteWhenAllPustulesDone = true;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool useUnscaledTime;

    private readonly List<Pustule> spawnedPustules = new List<Pustule>();
    private CustomerAgent activeCustomer;
    private Sanity activeSanity;
    private Pustule activePustule;
    private Pustule meterPustule;
    private NeedleToolState toolState = NeedleToolState.None;
    private NeedleActionMode activeActionMode = NeedleActionMode.None;
    private bool unsupportedToolSelected;
    private bool isRunning;
    private bool isComplete;
    private bool completionRaised;

    public bool IsRunning => isRunning;
    public bool IsComplete => isComplete;
    public NeedleToolState ToolState => toolState;

    public event Action MiniGameCompleted;

    private void Awake()
    {
        RefreshPustuleList();
        SubscribePustules();

        if (startHidden)
        {
            SetRootVisible(false);
        }
    }

    private void OnDestroy()
    {
        UnsubscribePustules();
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
        UpdateNeedleTipPreview(worldPointer);

        if (WasPrimaryPointerPressedThisFrame() && !IsPointerOverUi())
        {
            BeginActionAt(worldPointer);
        }

        TickPustules(worldPointer, deltaTime);

        if (WasPrimaryPointerReleasedThisFrame())
        {
            EndAction();
        }
    }

    public void Begin(CustomerAgent customer)
    {
        activeCustomer = customer;
        activeSanity = customer != null ? customer.GetComponent<Sanity>() : null;
        activePustule = null;
        meterPustule = null;
        toolState = NeedleToolState.None;
        activeActionMode = NeedleActionMode.None;
        unsupportedToolSelected = false;
        isRunning = true;
        isComplete = false;
        completionRaised = false;

        PreparePustules();
        SubscribePustules();
        ResetPustules();
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

    public void EquipNeedle()
    {
        unsupportedToolSelected = false;
        toolState = NeedleToolState.Needle;
    }

    public void PutNeedleDown()
    {
        unsupportedToolSelected = false;
        toolState = NeedleToolState.None;
        EndAction();
    }

    public void CompleteMiniGame()
    {
        if (!isComplete)
        {
            return;
        }

        RaiseCompleted();
    }

    private void HandleToolSelected(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            PutNeedleDown();
        }
        else if (toolId == overlayToolId)
        {
            EquipNeedle();
        }
        else
        {
            unsupportedToolSelected = true;
            toolState = NeedleToolState.None;
            EndAction();
        }
    }

    private void BeginActionAt(Vector2 pointerPosition)
    {
        Pustule target = FindPustuleAt(pointerPosition);
        if (target == null)
        {
            return;
        }

        bool canPierce = toolState == NeedleToolState.Needle && target.CanPierce;
        bool canSqueeze = !unsupportedToolSelected && toolState == NeedleToolState.None && target.CanSqueeze;
        bool canDrain = toolState == NeedleToolState.Needle && target.CanDrain;
        if (!canPierce && !canSqueeze && !canDrain)
        {
            return;
        }

        activePustule = target;
        meterPustule = target;
        activeActionMode = canPierce
            ? NeedleActionMode.Pierce
            : canSqueeze
                ? NeedleActionMode.Squeeze
                : NeedleActionMode.Drain;
        RefreshMeters();
        flow?.SetTreatmentStress(activeActionMode != NeedleActionMode.Pierce);
    }

    private void EndAction()
    {
        activePustule = null;
        activeActionMode = NeedleActionMode.None;
        flow?.SetTreatmentStress(false);
        RefreshMeters();
    }

    private void TickPustules(Vector2 worldPointer, float deltaTime)
    {
        for (int i = 0; i < pustules.Count; i++)
        {
            Pustule pustule = pustules[i];
            if (pustule == null || pustule.IsCompleted)
            {
                continue;
            }

            if (pustule == activePustule)
            {
                switch (activeActionMode)
                {
                    case NeedleActionMode.Pierce:
                        pustule.TickPierce(deltaTime);
                        break;
                    case NeedleActionMode.Squeeze:
                        pustule.TickSqueeze(deltaTime, activeSanity);
                        break;
                    case NeedleActionMode.Drain:
                        pustule.TickDrain(worldPointer, deltaTime, activeSanity);
                        break;
                }
            }
            else
            {
                pustule.TickIdle(deltaTime);
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

    private Pustule FindPustuleAt(Vector2 pointerPosition)
    {
        for (int i = pustules.Count - 1; i >= 0; i--)
        {
            Pustule pustule = pustules[i];
            if (pustule != null && !pustule.IsCompleted && pustule.ContainsPoint(pointerPosition))
            {
                return pustule;
            }
        }

        return null;
    }

    private void UpdateNeedleTipPreview(Vector2 worldPointer)
    {
        if (toolState != NeedleToolState.Needle)
        {
            return;
        }

        for (int i = 0; i < pustules.Count; i++)
        {
            Pustule pustule = pustules[i];
            if (pustule != null && !pustule.IsCompleted)
            {
                pustule.SetNeedleTipPreview(worldPointer);
            }
        }
    }

    private void PreparePustules()
    {
        if (spawnPustulesOnBegin && pustulePrefabs.Count > 0 && spawnAnchors.Count > 0)
        {
            SpawnPustules();
            return;
        }

        RefreshPustuleList();
    }

    private void SpawnPustules()
    {
        ClearSpawnedPustules();
        pustules.Clear();

        List<Transform> anchors = GetShuffledAnchors();
        int low = Mathf.Max(1, minPustules);
        int high = Mathf.Max(low, maxPustules);
        int count = Mathf.Min(UnityEngine.Random.Range(low, high + 1), anchors.Count);
        Transform parent = pustuleRoot != null ? pustuleRoot : transform;
        List<Pustule> prefabBag = new List<Pustule>();

        for (int i = 0; i < count; i++)
        {
            if (!TryGetNextPrefab(prefabBag, out Pustule prefab))
            {
                break;
            }

            Pustule pustule = Instantiate(prefab, anchors[i].position, anchors[i].rotation, parent);
            pustule.name = $"{prefab.name}_{i + 1:00}";
            pustules.Add(pustule);
            spawnedPustules.Add(pustule);
        }
    }

    private bool TryGetNextPrefab(List<Pustule> prefabBag, out Pustule prefab)
    {
        if (prefabBag.Count == 0)
        {
            for (int i = 0; i < pustulePrefabs.Count; i++)
            {
                if (pustulePrefabs[i] != null)
                {
                    prefabBag.Add(pustulePrefabs[i]);
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

    private void RefreshPustuleList()
    {
        Transform searchRoot = pustuleRoot != null ? pustuleRoot : transform;
        pustules.Clear();
        searchRoot.GetComponentsInChildren(includeInactive: true, pustules);
    }

    private void ClearSpawnedPustules()
    {
        for (int i = spawnedPustules.Count - 1; i >= 0; i--)
        {
            if (spawnedPustules[i] != null)
            {
                Destroy(spawnedPustules[i].gameObject);
            }
        }

        spawnedPustules.Clear();
    }

    private void ResetPustules()
    {
        for (int i = 0; i < pustules.Count; i++)
        {
            pustules[i]?.ResetRuntimeState();
        }
    }

    private void SubscribePustules()
    {
        for (int i = 0; i < pustules.Count; i++)
        {
            if (pustules[i] == null)
            {
                continue;
            }

            pustules[i].Completed -= HandlePustuleCompleted;
            pustules[i].PainChanged -= HandleMeterChanged;
            pustules[i].ProgressChanged -= HandleMeterChanged;
            pustules[i].Completed += HandlePustuleCompleted;
            pustules[i].PainChanged += HandleMeterChanged;
            pustules[i].ProgressChanged += HandleMeterChanged;
        }
    }

    private void UnsubscribePustules()
    {
        for (int i = 0; i < pustules.Count; i++)
        {
            if (pustules[i] == null)
            {
                continue;
            }

            pustules[i].Completed -= HandlePustuleCompleted;
            pustules[i].PainChanged -= HandleMeterChanged;
            pustules[i].ProgressChanged -= HandleMeterChanged;
        }
    }

    private void HandlePustuleCompleted(Pustule pustule)
    {
        if (meterPustule == pustule)
        {
            meterPustule = null;
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
        isComplete = HasAnyPustule() && AreAllPustulesCompleted();
        SetCompleteButtonVisible(isComplete);

        if (isComplete && autoCompleteWhenAllPustulesDone)
        {
            RaiseCompleted();
        }
    }

    private bool HasAnyPustule()
    {
        for (int i = 0; i < pustules.Count; i++)
        {
            if (pustules[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool AreAllPustulesCompleted()
    {
        for (int i = 0; i < pustules.Count; i++)
        {
            if (pustules[i] != null && !pustules[i].IsCompleted)
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
        float progress = meterPustule != null ? meterPustule.OverallProgress : 0f;
        float pain = meterPustule != null ? meterPustule.PainLevel : 0f;
        overlay?.SetMeters(progress, pain);
    }

    private void SetCompleteButtonVisible(bool visible)
    {
        overlay?.SetCompleteVisible(visible && !autoCompleteWhenAllPustulesDone);
    }

    private void SetRootVisible(bool visible)
    {
        GameObject target = root != null ? root : gameObject;
        target.SetActive(visible);
    }
}
