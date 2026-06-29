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

    public string PlayerDisplayName => playerDisplayName;
    public string CustomerDisplayName => customerDisplayName;
    public AudioClip CustomerBlipClip => customerBlipClip;
    public IReadOnlyList<AudioClip> CustomerBlipClips => customerBlipClips;
    public IReadOnlyList<DialogLine> Lines => lines;
    public int Count => lines.Count;

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
        if (index < 0 || index >= lines.Count)
        {
            line = null;
            return false;
        }

        line = lines[index];
        return true;
    }
}
