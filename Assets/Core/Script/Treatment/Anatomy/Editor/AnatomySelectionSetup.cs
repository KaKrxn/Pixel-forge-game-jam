using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class AnatomySelectionSetup
{
    [MenuItem("Tools/Pixel Forge/Treatment/Create Anatomy Selection Screen")]
    public static void CreateAnatomySelectionScreen()
    {
        Treatment treatment = FindSelectedTreatment();
        Transform parent = FindAnatomyParent(treatment);
        if (parent == null)
        {
            Debug.LogWarning("Select TreatmentRoot, a Treatment component, or a child of the Treatment UI before creating the Anatomy Selection Screen.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        GameObject screen = CreateUiObject("AnatomyScreen", parent);
        StretchFull(screen.GetComponent<RectTransform>());

        AnatomyController controller = Undo.AddComponent<AnatomyController>(screen);

        GameObject bodyRoot = CreateUiObject("BodyRoot", screen.transform);
        RectTransform bodyRect = bodyRoot.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = new Vector2(0f, -10f);
        bodyRect.sizeDelta = new Vector2(420f, 620f);

        List<BodyPartButton> parts = new List<BodyPartButton>
        {
            CreateBodyPart(bodyRoot.transform, "HeadPart", BodyArea.Head, new Vector2(0f, 210f), new Vector2(120f, 120f), new Color(0.84f, 0.72f, 0.64f, 1f)),
            CreateBodyPart(bodyRoot.transform, "TorsoPart", BodyArea.Torso, new Vector2(0f, 60f), new Vector2(150f, 210f), new Color(0.76f, 0.61f, 0.55f, 1f)),
            CreateBodyPart(bodyRoot.transform, "ArmLeftPart", BodyArea.Arm, new Vector2(-125f, 55f), new Vector2(80f, 240f), new Color(0.70f, 0.48f, 0.44f, 1f)),
            CreateBodyPart(bodyRoot.transform, "ArmRightPart", BodyArea.Arm, new Vector2(125f, 55f), new Vector2(80f, 240f), new Color(0.70f, 0.48f, 0.44f, 1f)),
            CreateBodyPart(bodyRoot.transform, "LegLeftPart", BodyArea.Leg, new Vector2(-45f, -165f), new Vector2(75f, 260f), new Color(0.60f, 0.43f, 0.40f, 1f)),
            CreateBodyPart(bodyRoot.transform, "LegRightPart", BodyArea.Leg, new Vector2(45f, -165f), new Vector2(75f, 260f), new Color(0.60f, 0.43f, 0.40f, 1f))
        };

        TMP_Text statusText = CreateText(screen.transform, "AnatomyStatusText", "Select the treatment area from the patient's symptoms.", new Vector2(0.5f, 0.9f), new Vector2(760f, 60f), 24);

        GameObject partMessageRoot = CreateUiObject("PartMessageRoot", screen.transform);
        RectTransform messageRect = partMessageRoot.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.anchoredPosition = Vector2.zero;
        messageRect.sizeDelta = new Vector2(720f, 260f);

        Image messagePanel = Undo.AddComponent<Image>(partMessageRoot);
        messagePanel.color = new Color(0f, 0f, 0f, 0.72f);

        TMP_Text messageText = CreateText(partMessageRoot.transform, "PartMessageText", "Nothing to treat here.", new Vector2(0.5f, 0.62f), new Vector2(620f, 100f), 26);
        Button exitPartButton = CreateButton(partMessageRoot.transform, "ExitPartButton", "Back", new Vector2(0.5f, 0.22f), new Vector2(220f, 58f));
        partMessageRoot.SetActive(false);

        AssignController(controller, screen, bodyRoot, partMessageRoot, statusText, messageText, exitPartButton, parts, FindTongsMiniGame(parent));
        AssignTreatment(treatment, controller);

        EditorUtility.SetDirty(screen);
        if (treatment != null)
        {
            EditorUtility.SetDirty(treatment);
        }

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = screen;
        Debug.Log("Created Anatomy Selection Screen. Replace placeholder body part Images with cut sprites and enable Read/Write on those textures for alpha hit testing.");
    }

    private static Treatment FindSelectedTreatment()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            return Object.FindFirstObjectByType<Treatment>(FindObjectsInactive.Include);
        }

        Treatment treatment = selected.GetComponentInParent<Treatment>(true);
        if (treatment != null)
        {
            return treatment;
        }

        treatment = selected.GetComponentInChildren<Treatment>(true);
        return treatment != null ? treatment : Object.FindFirstObjectByType<Treatment>(FindObjectsInactive.Include);
    }

    private static Transform FindAnatomyParent(Treatment treatment)
    {
        if (Selection.activeGameObject != null && Selection.activeGameObject.transform is RectTransform)
        {
            return Selection.activeGameObject.transform;
        }

        if (treatment == null)
        {
            return null;
        }

        SerializedObject serializedTreatment = new SerializedObject(treatment);
        GameObject root = serializedTreatment.FindProperty("root").objectReferenceValue as GameObject;
        return root != null ? root.transform : treatment.transform;
    }

    private static TongsMiniGame FindTongsMiniGame(Transform parent)
    {
        if (parent != null)
        {
            TongsMiniGame local = parent.GetComponentInParent<TongsMiniGame>(true);
            if (local != null)
            {
                return local;
            }

            local = parent.root.GetComponentInChildren<TongsMiniGame>(true);
            if (local != null)
            {
                return local;
            }
        }

        return Object.FindFirstObjectByType<TongsMiniGame>(FindObjectsInactive.Include);
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static BodyPartButton CreateBodyPart(Transform parent, string name, BodyArea area, Vector2 position, Vector2 size, Color color)
    {
        GameObject part = CreateUiObject(name, parent);
        RectTransform rectTransform = part.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        Image image = Undo.AddComponent<Image>(part);
        image.color = color;
        image.raycastTarget = true;

        BodyPartButton button = Undo.AddComponent<BodyPartButton>(part);
        SerializedObject serializedButton = new SerializedObject(button);
        serializedButton.FindProperty("area").enumValueIndex = (int)area;
        serializedButton.FindProperty("normalColor").colorValue = color;
        serializedButton.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, Vector2 anchor, Vector2 size, float fontSize)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI label = Undo.AddComponent<TextMeshProUGUI>(textObject);
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        return label;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = size;

        Image image = Undo.AddComponent<Image>(buttonObject);
        image.color = new Color(0.55f, 0.12f, 0.18f, 1f);

        Button button = Undo.AddComponent<Button>(buttonObject);
        CreateText(buttonObject.transform, "Text", label, new Vector2(0.5f, 0.5f), size, 22f);
        return button;
    }

    private static void StretchFull(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void AssignController(
        AnatomyController controller,
        GameObject root,
        GameObject bodyRoot,
        GameObject partMessageRoot,
        TMP_Text statusText,
        TMP_Text partMessageText,
        Button exitPartButton,
        List<BodyPartButton> parts,
        TongsMiniGame tongsMiniGame)
    {
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("root").objectReferenceValue = root;
        serializedController.FindProperty("bodyRoot").objectReferenceValue = bodyRoot;
        serializedController.FindProperty("partMessageRoot").objectReferenceValue = partMessageRoot;
        serializedController.FindProperty("statusText").objectReferenceValue = statusText;
        serializedController.FindProperty("partMessageText").objectReferenceValue = partMessageText;
        serializedController.FindProperty("exitPartButton").objectReferenceValue = exitPartButton;
        serializedController.FindProperty("armInfected").boolValue = true;
        serializedController.FindProperty("tongsMiniGame").objectReferenceValue = tongsMiniGame;

        SerializedProperty partList = serializedController.FindProperty("partButtons");
        partList.arraySize = parts.Count;
        for (int i = 0; i < parts.Count; i++)
        {
            partList.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignTreatment(Treatment treatment, AnatomyController controller)
    {
        if (treatment == null)
        {
            return;
        }

        SerializedObject serializedTreatment = new SerializedObject(treatment);
        serializedTreatment.FindProperty("anatomyController").objectReferenceValue = controller;
        serializedTreatment.ApplyModifiedPropertiesWithoutUndo();
    }
}

