using UnityEngine;

public sealed class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private Transform customerParent;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform outsideDoorPoint;
    [SerializeField] private Transform insideDoorPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Door door;
    [SerializeField] private bool logWarnings = true;

    public CustomerAgent Spawn(CustomerDefinition definition)
    {
        if (definition == null)
        {
            LogWarning("Cannot spawn customer because definition is null.");
            return null;
        }

        if (definition.CustomerPrefab == null)
        {
            LogWarning($"Cannot spawn customer '{definition.CustomerId}' because no prefab is assigned.");
            return null;
        }

        Transform parent = customerParent != null ? customerParent : transform;
        CustomerAgent customer = Instantiate(definition.CustomerPrefab, parent);
        customer.name = string.IsNullOrWhiteSpace(definition.CustomerId)
            ? definition.CustomerPrefab.name
            : definition.CustomerId;

        CustomerCaseProvider caseProvider = customer.GetComponent<CustomerCaseProvider>();
        if (caseProvider != null)
        {
            caseProvider.SetCase(definition.CaseData);
        }
        else
        {
            LogWarning($"Spawned customer '{customer.name}' has no CustomerCaseProvider.");
        }

        customer.ConfigurePath(spawnPoint, outsideDoorPoint, insideDoorPoint, counterPoint, exitPoint);
        customer.ConfigureDoor(door);
        customer.gameObject.SetActive(true);
        return customer;
    }

    private void LogWarning(string message)
    {
        if (logWarnings)
        {
            Debug.LogWarning($"[CustomerSpawner] {message}", this);
        }
    }
}
