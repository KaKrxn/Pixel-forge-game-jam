using System;
using UnityEngine;

public enum ClinicFlowState
{
    Idle,
    CustomerEntering,
    WaitingForDialog,
    DialogActive,
    TreatmentReady,
    CustomerLeaving,
    Complete
}

public sealed class GameFlow : MonoBehaviour
{
    [SerializeField] private CustomerAgent firstCustomer;
    [SerializeField] private Dialog dialog;
    [SerializeField] private bool startOnPlay = true;

    public ClinicFlowState CurrentState { get; private set; } = ClinicFlowState.Idle;

    public event Action<ClinicFlowState> StateChanged;

    private CustomerAgent activeCustomer;

    private void Start()
    {
        if (startOnPlay && firstCustomer != null)
        {
            StartFirstCustomer();
        }
    }

    public void StartFirstCustomer()
    {
        activeCustomer = firstCustomer;
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

        SetState(ClinicFlowState.CustomerLeaving);

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

        SetState(ClinicFlowState.Complete);
    }

    public void CompleteTestTreatment()
    {
        if (CurrentState != ClinicFlowState.TreatmentReady)
        {
            return;
        }

        SetState(ClinicFlowState.Complete);
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
