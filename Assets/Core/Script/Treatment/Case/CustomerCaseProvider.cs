using UnityEngine;

public sealed class CustomerCaseProvider : MonoBehaviour
{
    [SerializeField] private TreatmentCaseData caseData;

    public TreatmentCaseData CaseData => caseData;
    public bool HasCase => caseData != null;
}
