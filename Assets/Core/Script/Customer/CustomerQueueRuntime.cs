using System.Collections.Generic;
using UnityEngine;

public sealed class CustomerQueueRuntime
{
    private readonly List<CustomerDefinition> queue = new List<CustomerDefinition>();
    private int currentIndex;

    public CustomerDefinition Current => currentIndex >= 0 && currentIndex < queue.Count ? queue[currentIndex] : null;
    public bool IsComplete => currentIndex >= queue.Count;
    public int CurrentIndex => currentIndex;
    public IReadOnlyList<CustomerDefinition> Queue => queue;

    public void SetQueue(IReadOnlyList<CustomerDefinition> definitions, int startIndex = 0)
    {
        queue.Clear();
        if (definitions != null)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].IsValid)
                {
                    queue.Add(definitions[i]);
                }
            }
        }

        currentIndex = Mathf.Clamp(startIndex, 0, queue.Count);
    }

    public bool Advance()
    {
        currentIndex++;
        return !IsComplete;
    }

    public void Clear()
    {
        queue.Clear();
        currentIndex = 0;
    }
}
