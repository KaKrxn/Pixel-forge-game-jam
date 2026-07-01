using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TreatmentCase", menuName = "Pixel Forge/Treatment Case")]
public sealed class TreatmentCaseData : ScriptableObject
{
    [SerializeField] private string caseId;
    [SerializeField] private string customerDisplayName = "Customer";
    [SerializeField] private DialogData dialogData;
    [SerializeField] private List<TreatmentAreaRequirement> requiredTreatments = new List<TreatmentAreaRequirement>();

    public string CaseId => caseId ?? string.Empty;
    public string CustomerDisplayName => string.IsNullOrWhiteSpace(customerDisplayName) ? "Customer" : customerDisplayName;
    public DialogData DialogData => dialogData;
    public IReadOnlyList<TreatmentAreaRequirement> RequiredTreatments => requiredTreatments;
    public int RequiredTreatmentCount => requiredTreatments != null ? requiredTreatments.Count : 0;

    public bool TryGetRequirement(BodyArea area, out TreatmentAreaRequirement requirement)
    {
        if (requiredTreatments != null)
        {
            for (int i = 0; i < requiredTreatments.Count; i++)
            {
                TreatmentAreaRequirement current = requiredTreatments[i];
                if (current != null && current.Area == area)
                {
                    requirement = current;
                    return true;
                }
            }
        }

        requirement = null;
        return false;
    }
}
