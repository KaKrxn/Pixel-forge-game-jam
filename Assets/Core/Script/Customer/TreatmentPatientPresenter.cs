using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gives the active customer a presence in the Treatment Room. After dialog, the customer is reparented
/// under the treatment room root and placed at a patient point. The figure is shown on the Anatomy select
/// screen and hidden while a mini game is running (driven by <see cref="AnatomyController"/> navigation
/// state). Because the customer is left parented under the treatment room root, it stays behind when the
/// player returns to the counter to refill the candle — it does not follow. On cure, the customer is
/// reparented back to the counter room so the existing exit walk plays.
/// </summary>
public sealed class TreatmentPatientPresenter : MonoBehaviour
{
    [Header("Treatment Placement")]
    [SerializeField] private Transform treatmentRoomParent;      // reparent the customer under this while treating
    [SerializeField] private Transform treatmentPatientPoint;    // stand position in the Treatment Room
    [SerializeField] private bool matchPointRotation;

    [Header("Visibility Source")]
    [SerializeField] private AnatomyController anatomyController; // show on select level, hide inside a mini game

    [Header("Scale")]
    [SerializeField] private Vector3 treatmentScale = new Vector3(2f, 2f, 1f); // enlarged in the treatment room
    [SerializeField] private Vector3 shopScale = Vector3.zero;                 // normal shop size (zero = cached original)
    [SerializeField] private bool animateScale = true;
    [SerializeField, Min(0f)] private float scaleDuration = 0.35f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private CustomerAgent activeCustomer;
    private readonly List<SpriteRenderer> figureRenderers = new List<SpriteRenderer>();
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale = Vector3.one;
    private bool hasOriginalScale;
    private Coroutine scaleRoutine;
    private bool relocated;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>Reparent the customer into the Treatment Room and place it at the patient point.</summary>
    public void MoveToTreatment(CustomerAgent customer)
    {
        if (customer == null)
        {
            return;
        }

        if (treatmentRoomParent == null || treatmentPatientPoint == null)
        {
            Debug.LogWarning("TreatmentPatientPresenter: treatmentRoomParent or treatmentPatientPoint is not assigned.", this);
            return;
        }

        activeCustomer = customer;
        CacheFigureRenderers(customer);

        Transform customerTransform = customer.transform;
        if (!hasOriginalScale)
        {
            originalLocalScale = customerTransform.localScale;
            hasOriginalScale = true;
        }

        if (!relocated)
        {
            originalParent = customerTransform.parent;
            originalLocalPosition = customerTransform.localPosition;
            originalLocalRotation = customerTransform.localRotation;
        }

        customerTransform.SetParent(treatmentRoomParent, worldPositionStays: true);
        customerTransform.position = treatmentPatientPoint.position;
        if (matchPointRotation)
        {
            customerTransform.rotation = treatmentPatientPoint.rotation;
        }

        ApplyScale(customerTransform, treatmentScale); // enlarge in the treatment room

        relocated = true;
        ShowPatient();
        Log($"MoveToTreatment customer={Describe(customer)} point={Describe(treatmentPatientPoint)}");
    }

    /// <summary>Reparent the customer back to its counter home so the existing exit walk can play.</summary>
    public void ReturnToCounterForExit(CustomerAgent customer)
    {
        if (customer == null)
        {
            return;
        }

        Transform customerTransform = customer.transform;
        if (relocated && originalParent != null)
        {
            customerTransform.SetParent(originalParent, worldPositionStays: false);
            customerTransform.localPosition = originalLocalPosition;
            customerTransform.localRotation = originalLocalRotation;
        }

        ApplyScale(customerTransform, ResolveShopScale()); // shrink back to shop size for the walk-out

        relocated = false;
        SetFigureVisible(true); // visible for the walk-out
        Log($"ReturnToCounterForExit customer={Describe(customer)} restoredParent={Describe(originalParent)}");
        activeCustomer = null;
    }

    public void ShowPatient()
    {
        SetFigureVisible(true);
    }

    public void HidePatient()
    {
        SetFigureVisible(false);
    }

    private void ApplyScale(Transform target, Vector3 scale)
    {
        if (target == null)
        {
            return;
        }

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        // Coroutines need an active component; fall back to instant if animation is off or unavailable.
        if (!animateScale || scaleDuration <= 0f || !isActiveAndEnabled)
        {
            target.localScale = scale;
            return;
        }

        scaleRoutine = StartCoroutine(ScaleRoutine(target, scale));
    }

    private IEnumerator ScaleRoutine(Transform target, Vector3 to)
    {
        Vector3 from = target.localScale;
        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / scaleDuration);
            float eased = 1f - (1f - k) * (1f - k); // ease-out
            target.localScale = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }

        target.localScale = to;
        scaleRoutine = null;
    }

    private Vector3 ResolveShopScale()
    {
        if (shopScale != Vector3.zero)
        {
            return shopScale;
        }

        return hasOriginalScale ? originalLocalScale : Vector3.one;
    }

    private void Subscribe()
    {
        if (anatomyController != null)
        {
            anatomyController.NavigationStateChanged -= HandleNavigationStateChanged;
            anatomyController.NavigationStateChanged += HandleNavigationStateChanged;
        }
    }

    private void Unsubscribe()
    {
        if (anatomyController != null)
        {
            anatomyController.NavigationStateChanged -= HandleNavigationStateChanged;
        }
    }

    private void HandleNavigationStateChanged()
    {
        if (activeCustomer == null || !relocated)
        {
            return;
        }

        // Visible on the anatomy select level; hidden while the player is inside a body part / mini game.
        bool insidePart = anatomyController != null && anatomyController.IsInsidePart;
        SetFigureVisible(!insidePart);
    }

    private void CacheFigureRenderers(CustomerAgent customer)
    {
        figureRenderers.Clear();
        if (customer != null)
        {
            customer.GetComponentsInChildren(includeInactive: true, figureRenderers);
        }
    }

    private void SetFigureVisible(bool visible)
    {
        for (int i = 0; i < figureRenderers.Count; i++)
        {
            if (figureRenderers[i] != null)
            {
                figureRenderers[i].enabled = visible;
            }
        }
    }

    private void Log(string message)
    {
        if (debugLog)
        {
            Debug.Log($"[TreatmentFlow][PatientPresenter] {message}", this);
        }
    }

    private static string Describe(Object target)
    {
        return target != null ? $"{target.name} ({target.GetType().Name})" : "null";
    }
}
