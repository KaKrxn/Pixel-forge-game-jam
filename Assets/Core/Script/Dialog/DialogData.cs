using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogSpeaker
{
    Player,
    Customer
}

[Serializable]
public sealed class DialogLine
{
    [SerializeField] private DialogSpeaker speaker = DialogSpeaker.Customer;
    [SerializeField, TextArea(2, 5)] private string text;

    public DialogSpeaker Speaker => speaker;
    public string Text => text ?? string.Empty;
}

[Serializable]
public sealed class PainDialogueSet
{
    [SerializeField] private List<DialogLine> minor = new List<DialogLine>();
    [SerializeField] private List<DialogLine> moderate = new List<DialogLine>();
    [SerializeField] private List<DialogLine> severe = new List<DialogLine>();

    public IReadOnlyList<DialogLine> Minor => minor;
    public IReadOnlyList<DialogLine> Moderate => moderate;
    public IReadOnlyList<DialogLine> Severe => severe;
}

[Serializable]
public sealed class AggressivePainDialogueSet
{
    [SerializeField] private List<DialogLine> mild = new List<DialogLine>();
    [SerializeField] private List<DialogLine> moderate = new List<DialogLine>();
    [SerializeField] private List<DialogLine> severe = new List<DialogLine>();

    public IReadOnlyList<DialogLine> Mild => mild;
    public IReadOnlyList<DialogLine> Moderate => moderate;
    public IReadOnlyList<DialogLine> Severe => severe;
}

[Serializable]
public sealed class SanityDialogueSet
{
    [SerializeField] private List<DialogLine> above20 = new List<DialogLine>();
    [SerializeField] private List<DialogLine> above50 = new List<DialogLine>();
    [SerializeField] private List<DialogLine> above80 = new List<DialogLine>();

    public IReadOnlyList<DialogLine> Above20 => above20;
    public IReadOnlyList<DialogLine> Above50 => above50;
    public IReadOnlyList<DialogLine> Above80 => above80;
}

[Serializable]
public sealed class AbandonedTreatmentDialogueSet
{
    [SerializeField] private List<DialogLine> above0 = new List<DialogLine>();
    [SerializeField] private List<DialogLine> above50 = new List<DialogLine>();
    [SerializeField] private List<DialogLine> above80 = new List<DialogLine>();

    public IReadOnlyList<DialogLine> Above0 => above0;
    public IReadOnlyList<DialogLine> Above50 => above50;
    public IReadOnlyList<DialogLine> Above80 => above80;
}

[CreateAssetMenu(fileName = "DialogData", menuName = "Pixel Forge/Dialog Data")]
public sealed class DialogData : ScriptableObject
{
    [Header("Display Names")]
    [SerializeField] private string playerDisplayName = "Player";
    [SerializeField] private string customerDisplayName = "Customer";

    [Header("Audio")]
    [SerializeField] private AudioClip customerBlipClip;
    [SerializeField] private List<AudioClip> customerBlipClips = new List<AudioClip>();

    [Header("Lines")]
    [SerializeField] private List<DialogLine> lines = new List<DialogLine>();

    [Header("Conversation")]
    [SerializeField] private List<DialogLine> normalConversation1 = new List<DialogLine>();
    [SerializeField] private List<DialogLine> normalConversation2 = new List<DialogLine>();
    [SerializeField] private List<DialogLine> aggressiveConversation = new List<DialogLine>();

    [Header("Treatment Dialogue")]
    [SerializeField] private PainDialogueSet painDialogue = new PainDialogueSet();
    [SerializeField] private AggressivePainDialogueSet aggressivePainDialogue = new AggressivePainDialogueSet();
    [SerializeField] private SanityDialogueSet sanityDialogue = new SanityDialogueSet();
    [SerializeField] private SanityDialogueSet aggressiveSanityDialogue = new SanityDialogueSet();
    [SerializeField] private AbandonedTreatmentDialogueSet abandonedTreatmentDialogue = new AbandonedTreatmentDialogueSet();
    [SerializeField] private AbandonedTreatmentDialogueSet aggressiveAbandonedTreatmentDialogue = new AbandonedTreatmentDialogueSet();

    public string PlayerDisplayName => playerDisplayName;
    public string CustomerDisplayName => customerDisplayName;
    public AudioClip CustomerBlipClip => customerBlipClip;
    public IReadOnlyList<AudioClip> CustomerBlipClips => customerBlipClips;
    public IReadOnlyList<DialogLine> Lines => PrimaryLines;
    public IReadOnlyList<DialogLine> LegacyLines => lines;
    public IReadOnlyList<DialogLine> NormalConversation1 => normalConversation1;
    public IReadOnlyList<DialogLine> NormalConversation2 => normalConversation2;
    public IReadOnlyList<DialogLine> AggressiveConversation => aggressiveConversation;
    public PainDialogueSet PainDialogue => painDialogue;
    public AggressivePainDialogueSet AggressivePainDialogue => aggressivePainDialogue;
    public SanityDialogueSet SanityDialogue => sanityDialogue;
    public SanityDialogueSet AggressiveSanityDialogue => aggressiveSanityDialogue;
    public AbandonedTreatmentDialogueSet AbandonedTreatmentDialogue => abandonedTreatmentDialogue;
    public AbandonedTreatmentDialogueSet AggressiveAbandonedTreatmentDialogue => aggressiveAbandonedTreatmentDialogue;
    public int Count => PrimaryLines.Count;

    private List<DialogLine> PrimaryLines
    {
        get
        {
            if (lines != null && lines.Count > 0)
            {
                return lines;
            }

            return normalConversation1 ?? EmptyLines;
        }
    }

    private static readonly List<DialogLine> EmptyLines = new List<DialogLine>();

    public string GetDisplayName(DialogSpeaker speaker)
    {
        string displayName = speaker == DialogSpeaker.Player
            ? playerDisplayName
            : customerDisplayName;

        return string.IsNullOrWhiteSpace(displayName) ? speaker.ToString() : displayName;
    }

    public AudioClip GetBlipClip(DialogSpeaker speaker, AudioClip playerFallback, AudioClip customerFallback, AudioClip previousCustomerClip = null)
    {
        if (speaker == DialogSpeaker.Player)
        {
            return playerFallback;
        }

        AudioClip clip = GetRandomCustomerBlipClip(previousCustomerClip);
        if (clip != null)
        {
            return clip;
        }

        return customerBlipClip != null ? customerBlipClip : customerFallback;
    }

    private AudioClip GetRandomCustomerBlipClip(AudioClip previousClip)
    {
        if (customerBlipClips == null || customerBlipClips.Count == 0)
        {
            return null;
        }

        int candidateCount = 0;
        AudioClip firstAvailableClip = null;

        foreach (AudioClip clip in customerBlipClips)
        {
            if (clip == null)
            {
                continue;
            }

            if (firstAvailableClip == null)
            {
                firstAvailableClip = clip;
            }

            if (clip != previousClip)
            {
                candidateCount++;
            }
        }

        if (firstAvailableClip == null)
        {
            return null;
        }

        if (candidateCount == 0)
        {
            return firstAvailableClip;
        }

        int selectedCandidate = UnityEngine.Random.Range(0, candidateCount);
        foreach (AudioClip clip in customerBlipClips)
        {
            if (clip == null || clip == previousClip)
            {
                continue;
            }

            if (selectedCandidate == 0)
            {
                return clip;
            }

            selectedCandidate--;
        }

        return firstAvailableClip;
    }

    public bool TryGetLine(int index, out DialogLine line)
    {
        List<DialogLine> primaryLines = PrimaryLines;
        if (index < 0 || index >= primaryLines.Count)
        {
            line = null;
            return false;
        }

        line = primaryLines[index];
        return true;
    }
}
