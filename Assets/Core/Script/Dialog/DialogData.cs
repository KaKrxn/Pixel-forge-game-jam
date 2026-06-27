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
    public string Text => text;
}

[CreateAssetMenu(fileName = "DialogData", menuName = "Pixel Forge/Dialog Data")]
public sealed class DialogData : ScriptableObject
{
    [SerializeField] private List<DialogLine> lines = new List<DialogLine>();

    public IReadOnlyList<DialogLine> Lines => lines;
    public int Count => lines.Count;

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
