using System.Collections.Generic;
using UnityEngine;

public sealed class CustomerQueueBuilder : MonoBehaviour
{
    [SerializeField] private CustomerDefinition fixedStarter;
    [SerializeField] private List<CustomerDefinition> customerPool = new List<CustomerDefinition>();
    [SerializeField] private bool useFixedStarter = true;
    [Header("Duplicates")]
    [Tooltip("Off: each customer appears at most once (shuffled). On: customers can repeat (random with replacement).")]
    [SerializeField] private bool allowDuplicateCustomers = false;
    [Tooltip("How many customers the queue holds when duplicates are allowed.")]
    [SerializeField, Min(1)] private int duplicateQueueLength = 8;
    [Tooltip("When duplicates are allowed, avoid the same customer appearing in two consecutive slots.")]
    [SerializeField] private bool avoidBackToBackRepeat = true;
    [SerializeField] private bool logQueueOrder = true;

    public List<CustomerDefinition> BuildQueue()
    {
        List<CustomerDefinition> queue = allowDuplicateCustomers
            ? BuildDuplicateQueue()
            : BuildUniqueQueue();

        LogQueue(queue);
        return queue;
    }

    private List<CustomerDefinition> BuildUniqueQueue()
    {
        List<CustomerDefinition> queue = new List<CustomerDefinition>();
        List<CustomerDefinition> pool = new List<CustomerDefinition>();

        if (useFixedStarter && fixedStarter != null)
        {
            AddIfValid(queue, fixedStarter);
        }

        if (customerPool != null)
        {
            for (int i = 0; i < customerPool.Count; i++)
            {
                CustomerDefinition definition = customerPool[i];
                if (definition != null && definition != fixedStarter && !pool.Contains(definition))
                {
                    AddIfValid(pool, definition);
                }
            }
        }

        Shuffle(pool);
        queue.AddRange(pool);
        return queue;
    }

    private List<CustomerDefinition> BuildDuplicateQueue()
    {
        List<CustomerDefinition> queue = new List<CustomerDefinition>();

        // Unique, valid candidates to sample from (duplicates come from repeated sampling, not the pool list itself).
        List<CustomerDefinition> candidates = new List<CustomerDefinition>();
        if (customerPool != null)
        {
            for (int i = 0; i < customerPool.Count; i++)
            {
                CustomerDefinition definition = customerPool[i];
                if (definition != null && !candidates.Contains(definition))
                {
                    AddIfValid(candidates, definition);
                }
            }
        }

        CustomerDefinition previous = null;

        if (useFixedStarter && fixedStarter != null && AddIfValid(queue, fixedStarter))
        {
            previous = fixedStarter;
        }

        if (candidates.Count == 0)
        {
            return queue;
        }

        while (queue.Count < duplicateQueueLength)
        {
            CustomerDefinition pick = PickRandom(candidates, avoidBackToBackRepeat ? previous : null);
            queue.Add(pick);
            previous = pick;
        }

        return queue;
    }

    private static CustomerDefinition PickRandom(List<CustomerDefinition> candidates, CustomerDefinition avoid)
    {
        // Only avoid a repeat when there is another option to pick instead.
        if (avoid == null || candidates.Count < 2)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        CustomerDefinition pick;
        do
        {
            pick = candidates[Random.Range(0, candidates.Count)];
        }
        while (pick == avoid);

        return pick;
    }

    private bool AddIfValid(List<CustomerDefinition> target, CustomerDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        if (!definition.IsValid)
        {
            Debug.LogWarning($"[CustomerQueue] Skipped invalid customer definition '{definition.name}'.", definition);
            return false;
        }

        target.Add(definition);
        return true;
    }

    private static void Shuffle(List<CustomerDefinition> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    private void LogQueue(IReadOnlyList<CustomerDefinition> queue)
    {
        if (!logQueueOrder)
        {
            return;
        }

        if (queue == null || queue.Count == 0)
        {
            Debug.LogWarning("[CustomerQueue] Built an empty customer queue.", this);
            return;
        }

        List<string> labels = new List<string>();
        for (int i = 0; i < queue.Count; i++)
        {
            CustomerDefinition definition = queue[i];
            labels.Add(definition != null ? definition.CustomerId : "null");
        }

        Debug.Log($"[CustomerQueue] Built queue: {string.Join(" -> ", labels)}", this);
    }
}
