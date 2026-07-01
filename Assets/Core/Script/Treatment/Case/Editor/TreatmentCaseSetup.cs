using UnityEditor;
using UnityEngine;

public static class TreatmentCaseSetup
{
    private const string SampleCasePath = "Assets/Core/Data/Treatment/Cases/Case_Test_MixedTreatment.asset";

    [MenuItem("Tools/Pixel Forge/Treatment/Assign Test Treatment Case To Selected Customer")]
    public static void AssignTestTreatmentCaseToSelectedCustomer()
    {
        CustomerAgent customer = FindSelectedOrSceneCustomer();
        if (customer == null)
        {
            Debug.LogWarning("Treatment case setup skipped because no CustomerAgent was selected or found in the open scene.");
            return;
        }

        TreatmentCaseData caseData = AssetDatabase.LoadAssetAtPath<TreatmentCaseData>(SampleCasePath);
        if (caseData == null)
        {
            Debug.LogWarning($"Treatment case setup skipped because the sample case asset was not found at {SampleCasePath}.");
            return;
        }

        CustomerCaseProvider provider = customer.GetComponent<CustomerCaseProvider>();
        if (provider == null)
        {
            provider = Undo.AddComponent<CustomerCaseProvider>(customer.gameObject);
        }

        SerializedObject serializedObject = new SerializedObject(provider);
        serializedObject.FindProperty("caseData").objectReferenceValue = caseData;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(provider);
        Selection.activeGameObject = customer.gameObject;
        Debug.Log($"Assigned {caseData.name} to {customer.name}.");
    }

    private static CustomerAgent FindSelectedOrSceneCustomer()
    {
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            GameObject selected = Selection.gameObjects[i];
            if (selected == null)
            {
                continue;
            }

            CustomerAgent inParent = selected.GetComponentInParent<CustomerAgent>(true);
            if (inParent != null)
            {
                return inParent;
            }

            CustomerAgent inChildren = selected.GetComponentInChildren<CustomerAgent>(true);
            if (inChildren != null)
            {
                return inChildren;
            }
        }

        CustomerAgent[] customers = Object.FindObjectsByType<CustomerAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return customers.Length > 0 ? customers[0] : null;
    }
}
