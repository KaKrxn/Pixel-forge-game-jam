using System.Collections.Generic;
using UnityEngine;

public sealed class CustomerQueueBuilder : MonoBehaviour
{
    [SerializeField] private CustomerDefinition fixedStarter;
    [SerializeField] private List<CustomerDefinition> customerPool = new List<CustomerDefinition>();
    [SerializeField] private bool useFixedStarter = true;
    [SerializeField] private bool logQueueOrder = true;

    public List<CustomerDefinition> BuildQueue()
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
        LogQueue(queue);
        return queue;
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
