using System;
using UnityEngine;

public enum ClinicFlowState
{
    Idle,
    CustomerEntering,
    WaitingForDialog,
    DialogActive,
    TransitionToTreatment,
    TreatmentReady,
    TreatmentActive,
    CustomerLeaving,
    CounterBreather,
    AllCustomersComplete,
    GameOver,
    Complete
}

public sealed class GameFlow : MonoBehaviour
{
    [SerializeField] private CustomerAgent firstCustomer;
    [SerializeField] private bool useCustomerQueue;
    [SerializeField] private CustomerQueueBuilder customerQueueBuilder;
    [SerializeField] private CustomerSpawner customerSpawner;
    [SerializeField] private bool autoStartNextQueuedCustomer;
    [SerializeField] private Dialog dialog;
    [SerializeField] private RoomTransition roomTransition;
    [SerializeField] private Treatment treatment;
    [SerializeField] private Candle candle;
    [SerializeField] private bool startOnPlay = true;
    [Header("Debug")]
    [SerializeField] private bool debugTreatmentFlow = true;

    public ClinicFlowState CurrentState { get; private set; } = ClinicFlowState.Idle;

    public event Action<ClinicFlowState> StateChanged;

    private CustomerAgent activeCustomer;
    private Sanity activeSanity;
    private readonly CustomerQueueRuntime customerQueue = new CustomerQueueRuntime();
    private bool queueRunning;
    private bool activeCustomerSpawnedByQueue;

    private void Awake()
    {
        ResolveSceneReferences();
    }

    private void Start()
    {
        SetCandleAtCounter(true);

        if (!startOnPlay)
        {
            return;
        }

        if (useCustomerQueue && TryStartCustomerQueue())
        {
            return;
        }

        if (firstCustomer != null)
        {
            StartFirstCustomer();
        }
    }

    private void Update()
    {
        TickHiddenStatusSystems(Time.deltaTime);
    }

    public void StartFirstCustomer()
    {
        queueRunning = false;
        customerQueue.Clear();
        BeginCustomer(firstCustomer, spawnedByQueue: false);
    }

    public bool TryStartCustomerQueue()
    {
        if (customerQueueBuilder == null || customerSpawner == null)
        {
            return false;
        }

        customerQueue.SetQueue(customerQueueBuilder.BuildQueue());
        if (customerQueue.Current == null)
        {
            customerQueue.Clear();
            return false;
        }

        queueRunning = true;
        return SpawnCurrentQueuedCustomer();
    }

    public void StartNextQueuedCustomer()
    {
        if (!queueRunning || CurrentState != ClinicFlowState.CounterBreather)
        {
            return;
        }

        SpawnCurrentQueuedCustomer();
    }

    private bool SpawnCurrentQueuedCustomer()
    {
        CustomerDefinition definition = customerQueue.Current;
        if (definition == null)
        {
            SetState(ClinicFlowState.AllCustomersComplete);
            return false;
        }

        if (customerQueue.CurrentIndex == 0 && firstCustomer != null)
        {
            ApplyDefinitionToCustomer(firstCustomer, definition);
            BeginCustomer(firstCustomer, spawnedByQueue: true);
            return true;
        }

        if (customerSpawner == null)
        {
            SetState(ClinicFlowState.AllCustomersComplete);
            return false;
        }

        CustomerAgent customer = customerSpawner.Spawn(definition);
        if (customer == null)
        {
            SetState(ClinicFlowState.GameOver);
            return false;
        }

        BeginCustomer(customer, spawnedByQueue: true);
        return true;
    }

    private static void ApplyDefinitionToCustomer(CustomerAgent customer, CustomerDefinition definition)
    {
        if (customer == null || definition == null)
        {
            return;
        }

        customer.name = string.IsNullOrWhiteSpace(definition.CustomerId)
            ? customer.name
            : definition.CustomerId;

        CustomerCaseProvider caseProvider = customer.GetComponent<CustomerCaseProvider>();
        if (caseProvider != null)
        {
            caseProvider.SetCase(definition.CaseData);
        }
    }

    private void BeginCustomer(CustomerAgent customer, bool spawnedByQueue)
    {
        LogTreatmentFlow($"BeginCustomer customer={DescribeObject(customer)} spawnedByQueue={spawnedByQueue}");
        if (customer == null)
        {
            return;
        }

        activeCustomer = customer;
        activeCustomerSpawnedByQueue = spawnedByQueue;
        activeSanity = activeCustomer != null ? activeCustomer.GetComponent<Sanity>() : null;
        if (activeSanity != null)
        {
            activeSanity.SetCandle(candle);
            activeSanity.ResetSanity();
            activeSanity.StopMonitoring();
            activeSanity.TransformationReached -= TriggerTransformation;
            activeSanity.TransformationReached += TriggerTransformation;
        }

        SetState(ClinicFlowState.CustomerEntering);
        activeCustomer.BeginEnter(this);
    }

    public void CustomerReachedCounter(CustomerAgent customer)
    {
        if (customer != activeCustomer)
        {
            return;
        }

        SetState(ClinicFlowState.WaitingForDialog);
        activeSanity?.BeginMonitoring();
    }

    public void BeginDialog(CustomerAgent customer)
    {
        if (customer != activeCustomer || CurrentState != ClinicFlowState.WaitingForDialog)
        {
            return;
        }

        SetState(ClinicFlowState.DialogActive);
        customer.HideBubble();

        if (dialog != null)
        {
            dialog.Open(customer);
        }
    }

    public void CompleteDialog()
    {
        LogTreatmentFlow($"CompleteDialog state={CurrentState} activeCustomer={DescribeObject(activeCustomer)} treatment={DescribeObject(treatment)} roomTransition={DescribeObject(roomTransition)}");
        if (CurrentState != ClinicFlowState.DialogActive)
        {
            return;
        }

        SetState(ClinicFlowState.TransitionToTreatment);
        SetCandleAtCounter(false);

        if (treatment != null)
        {
            LogTreatmentFlow("CompleteDialog route=Treatment.Begin");
            SetState(ClinicFlowState.TreatmentActive);
            treatment.Begin(activeCustomer);
            return;
        }

        roomTransition?.ShowTreatmentRoom();
        SetState(ClinicFlowState.TreatmentReady);
    }

    public void CompleteTreatment(CustomerAgent customer)
    {
        LogTreatmentFlow($"CompleteTreatment customer={DescribeObject(customer)} activeCustomer={DescribeObject(activeCustomer)} state={CurrentState}");
        if (customer != activeCustomer || (CurrentState != ClinicFlowState.TreatmentReady && CurrentState != ClinicFlowState.TreatmentActive))
        {
            return;
        }

        SetTreatmentStress(false);
        activeSanity?.StopMonitoring();
        activeSanity?.ResetSanity();
        customer.MarkCured(); // swap to the cured visual before the customer walks out
        SetState(ClinicFlowState.CustomerLeaving);

        if (roomTransition != null)
        {
            roomTransition.ShowCounterRoom(BeginActiveCustomerExit);
            return;
        }

        BeginActiveCustomerExit();
    }

    private void BeginActiveCustomerExit()
    {
        SetCandleAtCounter(true);

        if (activeCustomer != null)
        {
            activeCustomer.BeginExit();
        }
        else
        {
            SetState(ClinicFlowState.Complete);
        }
    }

    public void CustomerLeft(CustomerAgent customer)
    {
        if (customer != activeCustomer)
        {
            return;
        }

        if (activeSanity != null)
        {
            activeSanity.TransformationReached -= TriggerTransformation;
            activeSanity.StopMonitoring();
        }

        CustomerAgent completedCustomer = activeCustomer;
        bool shouldAdvanceQueue = queueRunning && activeCustomerSpawnedByQueue;

        activeCustomer = null;
        activeSanity = null;
        activeCustomerSpawnedByQueue = false;

        if (shouldAdvanceQueue && completedCustomer != null)
        {
            Destroy(completedCustomer.gameObject);
        }

        if (!shouldAdvanceQueue)
        {
            SetState(ClinicFlowState.Complete);
            return;
        }

        if (customerQueue.Advance())
        {
            SetState(ClinicFlowState.CounterBreather);
            if (autoStartNextQueuedCustomer)
            {
                StartNextQueuedCustomer();
            }

            return;
        }

        queueRunning = false;
        SetState(ClinicFlowState.AllCustomersComplete);
    }

    public void SetTreatmentStress(bool active)
    {
        LogTreatmentFlow($"SetTreatmentStress active={active} activeSanity={DescribeObject(activeSanity)}");
        activeSanity?.SetTreatmentStress(active);
    }

    public void SetCandleAtCounter(bool atCounter)
    {
        LogTreatmentFlow($"SetCandleAtCounter atCounter={atCounter} candle={DescribeObject(candle)}");
        candle?.SetAtCounter(atCounter);
    }

    private void TickHiddenStatusSystems(float deltaTime)
    {
        if (candle != null && !candle.gameObject.activeInHierarchy)
        {
            candle.Tick(deltaTime);
        }

        if (activeSanity != null && !activeSanity.gameObject.activeInHierarchy)
        {
            activeSanity.Tick(deltaTime);
        }
    }

    private void TriggerTransformation()
    {
        SetTreatmentStress(false);
        SetState(ClinicFlowState.GameOver);
        Debug.Log("Sanity reached maximum. Transformation placeholder triggered.");
    }

    private void ResolveSceneReferences()
    {
        if (dialog == null)
        {
            dialog = GetComponent<Dialog>();
        }

        if (roomTransition == null)
        {
            roomTransition = GetComponent<RoomTransition>();
        }

        if (treatment == null)
        {
            treatment = GetComponent<Treatment>();
        }

        if (firstCustomer == null)
        {
            firstCustomer = FindFirstObjectByType<CustomerAgent>();
        }

        if (customerQueueBuilder == null)
        {
            customerQueueBuilder = FindFirstObjectByType<CustomerQueueBuilder>();
        }

        if (customerSpawner == null)
        {
            customerSpawner = FindFirstObjectByType<CustomerSpawner>();
        }

        if (customerSpawner == null && firstCustomer != null)
        {
            customerSpawner = gameObject.AddComponent<CustomerSpawner>();
        }

        customerSpawner?.ConfigureMissingFromTemplate(firstCustomer);

        if (candle == null)
        {
            candle = FindFirstObjectByType<Candle>();
        }
    }

    public void CompleteTestTreatment()
    {
        if (CurrentState != ClinicFlowState.TreatmentReady && CurrentState != ClinicFlowState.TreatmentActive)
        {
            return;
        }

        CompleteTreatment(activeCustomer);
    }

    private void SetState(ClinicFlowState nextState)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        CurrentState = nextState;
        LogTreatmentFlow($"SetState {CurrentState}");
        StateChanged?.Invoke(CurrentState);
    }

    private void LogTreatmentFlow(string message)
    {
        if (debugTreatmentFlow)
        {
            Debug.Log($"[TreatmentFlow][GameFlow] {message}", this);
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
}
