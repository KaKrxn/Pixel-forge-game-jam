using System.Collections.Generic;

public sealed class TreatmentCaseRuntime
{
    private readonly Dictionary<BodyArea, TreatmentAreaRequirement> requirements = new Dictionary<BodyArea, TreatmentAreaRequirement>();
    private readonly HashSet<BodyArea> treatedAreas = new HashSet<BodyArea>();

    public int RequiredCount => requirements.Count;
    public bool HasRequirements => requirements.Count > 0;

    public TreatmentCaseRuntime(TreatmentCaseData caseData)
    {
        if (caseData == null || caseData.RequiredTreatments == null)
        {
            return;
        }

        IReadOnlyList<TreatmentAreaRequirement> source = caseData.RequiredTreatments;
        for (int i = 0; i < source.Count; i++)
        {
            TreatmentAreaRequirement requirement = source[i];
            if (requirement == null)
            {
                continue;
            }

            requirements[requirement.Area] = requirement;
        }
    }

    public bool IsRequired(BodyArea area)
    {
        return requirements.ContainsKey(area);
    }

    public bool TryGetMiniGameType(BodyArea area, out TreatmentMiniGameType miniGameType)
    {
        if (requirements.TryGetValue(area, out TreatmentAreaRequirement requirement))
        {
            miniGameType = requirement.MiniGameType;
            return true;
        }

        miniGameType = TreatmentMiniGameType.None;
        return false;
    }

    public IEnumerable<BodyArea> GetRequiredAreas()
    {
        return requirements.Keys;
    }

    public void MarkTreated(BodyArea area)
    {
        if (requirements.ContainsKey(area))
        {
            treatedAreas.Add(area);
        }
    }

    public bool AreAllRequiredAreasTreated()
    {
        return requirements.Count > 0 && treatedAreas.Count >= requirements.Count;
    }
}
