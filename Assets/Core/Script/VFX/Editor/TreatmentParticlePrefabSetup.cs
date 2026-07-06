using System.IO;
using UnityEditor;
using UnityEngine;

public static class TreatmentParticlePrefabSetup
{
    private const string PrefabFolder = "Assets/Core/Prefab/VFX";
    private const string MaterialFolder = "Assets/Core/Material/VFX";
    private const string MagicMaterialPath = MaterialFolder + "/CandleMagicRefillParticle.mat";
    private const string BloodMaterialPath = MaterialFolder + "/TreatmentBloodSpeckParticle.mat";
    private const string MagicPrefabPath = PrefabFolder + "/CandleMagicRefillParticle.prefab";
    private const string BloodPrefabPath = PrefabFolder + "/TreatmentBloodSpeckParticle.prefab";

    [MenuItem("Tools/Pixel Forge/VFX/Create Treatment Particles")]
    public static void CreateTreatmentParticles()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);

        Material magicMaterial = GetOrCreateParticleMaterial(MagicMaterialPath, new Color(0.45f, 0.9f, 1f, 0.85f));
        Material bloodMaterial = GetOrCreateParticleMaterial(BloodMaterialPath, new Color(0.55f, 0.02f, 0.025f, 1f));

        CreateCandleMagicPrefab(magicMaterial);
        CreateBloodSpeckPrefab(bloodMaterial);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Treatment particle prefabs created: CandleMagicRefillParticle and TreatmentBloodSpeckParticle.");
    }

    [MenuItem("Tools/Pixel Forge/VFX/Attach Blood Speck To Selected Lesions")]
    public static void AttachBloodSpeckToSelectedLesions()
    {
        GameObject bloodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BloodPrefabPath);
        if (bloodPrefab == null)
        {
            CreateTreatmentParticles();
            bloodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BloodPrefabPath);
        }

        int count = 0;
        foreach (GameObject selected in Selection.gameObjects)
        {
            Lesion lesion = selected.GetComponentInParent<Lesion>(true);
            if (lesion == null)
            {
                lesion = selected.GetComponentInChildren<Lesion>(true);
            }

            if (lesion == null)
            {
                continue;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(bloodPrefab, lesion.transform) as GameObject;
            if (instance == null)
            {
                continue;
            }

            instance.name = "TreatmentBloodSpeckParticle";
            instance.transform.localPosition = Vector3.zero;
            ParticleSystem particles = instance.GetComponent<ParticleSystem>();

            SerializedObject serializedLesion = new SerializedObject(lesion);
            serializedLesion.FindProperty("cutSpeckParticles").objectReferenceValue = particles;
            serializedLesion.FindProperty("woundOpenParticles").objectReferenceValue = particles;
            serializedLesion.ApplyModifiedProperties();

            EditorUtility.SetDirty(lesion);
            count++;
        }

        Debug.Log($"Attached TreatmentBloodSpeckParticle to {count} lesion(s).");
    }

    [MenuItem("Tools/Pixel Forge/VFX/Attach Candle Magic To Selected Candle")]
    public static void AttachCandleMagicToSelectedCandle()
    {
        GameObject magicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MagicPrefabPath);
        if (magicPrefab == null)
        {
            CreateTreatmentParticles();
            magicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MagicPrefabPath);
        }

        int count = 0;
        foreach (GameObject selected in Selection.gameObjects)
        {
            Candle candle = selected.GetComponentInParent<Candle>(true);
            if (candle == null)
            {
                candle = selected.GetComponentInChildren<Candle>(true);
            }

            if (candle == null)
            {
                continue;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(magicPrefab, candle.transform) as GameObject;
            if (instance == null)
            {
                continue;
            }

            instance.name = "CandleMagicRefillParticle";
            instance.transform.localPosition = Vector3.zero;

            SerializedObject serializedController = new SerializedObject(instance.GetComponent<CandleRefillParticleController>());
            serializedController.FindProperty("candle").objectReferenceValue = candle;
            serializedController.ApplyModifiedProperties();

            EditorUtility.SetDirty(candle);
            count++;
        }

        Debug.Log($"Attached CandleMagicRefillParticle to {count} candle(s).");
    }

    private static void CreateCandleMagicPrefab(Material material)
    {
        GameObject root = new GameObject("CandleMagicRefillParticle", typeof(ParticleSystem), typeof(ParticleSystemSorting), typeof(CandleRefillParticleController));
        ParticleSystem particles = root.GetComponent<ParticleSystem>();
        ConfigureCandleMagic(particles);
        ConfigureRenderer(root.GetComponent<ParticleSystemRenderer>(), material, "Customer", 70);
        ConfigureSorting(root.GetComponent<ParticleSystemSorting>(), "Customer", 70);

        SavePrefab(root, MagicPrefabPath);
    }

    private static void CreateBloodSpeckPrefab(Material material)
    {
        GameObject root = new GameObject("TreatmentBloodSpeckParticle", typeof(ParticleSystem), typeof(ParticleSystemSorting));
        ParticleSystem particles = root.GetComponent<ParticleSystem>();
        ConfigureBloodSpecks(particles);
        ConfigureRenderer(root.GetComponent<ParticleSystemRenderer>(), material, "Customer", 75);
        ConfigureSorting(root.GetComponent<ParticleSystemSorting>(), "Customer", 75);

        SavePrefab(root, BloodPrefabPath);
    }

    private static void ConfigureCandleMagic(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.duration = 1.2f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.9f, 1f, 0.65f), new Color(1f, 0.94f, 0.55f, 0.95f));
        main.gravityModifier = -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 22f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f;
        shape.arc = 360f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
        velocity.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.45f, 0.9f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.35f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.18f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.45f, 1f, 0f));
    }

    private static void ConfigureBloodSpecks(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.38f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.02f, 0.025f, 1f), new Color(0.22f, 0f, 0f, 1f));
        main.gravityModifier = 0.12f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 7, 13) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 42f;
        shape.radius = 0.025f;
        shape.rotation = new Vector3(0f, 0f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.2f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.8f, 0.02f, 0.025f), 0f),
                new GradientColorKey(new Color(0.18f, 0f, 0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));
    }

    private static void ConfigureRenderer(ParticleSystemRenderer renderer, Material material, string sortingLayerName, int sortingOrder)
    {
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder;
        renderer.minParticleSize = 0.001f;
        renderer.maxParticleSize = 0.2f;
    }

    private static void ConfigureSorting(ParticleSystemSorting sorting, string sortingLayerName, int sortingOrder)
    {
        SerializedObject serializedSorting = new SerializedObject(sorting);
        serializedSorting.FindProperty("sortingLayerName").stringValue = sortingLayerName;
        serializedSorting.FindProperty("sortingOrder").intValue = sortingOrder;
        serializedSorting.ApplyModifiedPropertiesWithoutUndo();
        sorting.Apply();
    }

    private static Material GetOrCreateParticleMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        material = new Material(shader)
        {
            name = Path.GetFileNameWithoutExtension(path)
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
