using UnityEngine;

[CreateAssetMenu(fileName = "CustomerDefinition", menuName = "Pixel Forge/Customer Definition")]
public sealed class CustomerDefinition : ScriptableObject
{
    [SerializeField] private string customerId;
    [SerializeField] private string displayName;
    [SerializeField] private CustomerAgent customerPrefab;
    [SerializeField] private TreatmentCaseData caseData;

    public string CustomerId => customerId;
    public string DisplayName => displayName;
    public CustomerAgent CustomerPrefab => customerPrefab;
    public TreatmentCaseData CaseData => caseData;
    public bool IsValid => !string.IsNullOrWhiteSpace(customerId) && customerPrefab != null && caseData != null;
}
