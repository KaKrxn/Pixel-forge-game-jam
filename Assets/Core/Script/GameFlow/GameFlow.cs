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
    Complete
}

public sealed class GameFlow : MonoBehaviour
{
    [SerializeField] private CustomerAgent firstCustomer;
    [SerializeField] private Dialog dialog;
    [SerializeField] private RoomTransition roomTransition;
    [SerializeField] private Treatment treatment;
    [SerializeField] private Candle candle;
    [SerializeField] private bool startOnPlay = true;

    public ClinicFlowState CurrentState { get; private set; } = ClinicFlowState.Idle;

    public event Action<ClinicFlowState> StateChanged;

    private CustomerAgent activeCustomer;
    private Sanity activeSanity;

    private void Awake()
    {
        ResolveSceneReferences();
    }

    private void Start()
    {
        SetCandleAtCounter(true);

        if (startOnPlay && firstCustomer != null)
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
        activeCustomer = firstCustomer;
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
        if (CurrentState != ClinicFlowState.DialogActive)
        {
            return;
        }

        SetState(ClinicFlowState.TransitionToTreatment);
        SetCandleAtCounter(false);

        if (treatment != null)
        {
            SetState(ClinicFlowState.TreatmentActive);
            treatment.Begin(activeCustomer);
            return;
        }

        roomTransition?.ShowTreatmentRoom();
        SetState(ClinicFlowState.TreatmentReady);
    }

    public void CompleteTreatment(CustomerAgent customer)
    {
        if (customer != activeCustomer || (CurrentState != ClinicFlowState.TreatmentReady && CurrentState != ClinicFlowState.TreatmentActive))
        {
            return;
        }

        SetTreatmentStress(false);
        activeSanity?.StopMonitoring();
        activeSanity?.ResetSanity();
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

        SetState(ClinicFlowState.Complete);
    }

    public void SetTreatmentStress(bool active)
    {
        activeSanity?.SetTreatmentStress(active);
    }

    public void SetCandleAtCounter(bool atCounter)
    {
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
        SetState(ClinicFlowState.Complete);
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
        StateChanged?.Invoke(CurrentState);
    }
}
