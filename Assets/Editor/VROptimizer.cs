using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class VROptimizer : EditorWindow
{
    [MenuItem("Tools/VR Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<VROptimizer>("VR Optimizer");
    }

    void OnGUI()
    {
        GUILayout.Label("Quest Optimization Tools", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("1. Выключить дубликаты уровня (full_level 1 и 2)", GUILayout.Height(35)))
            DisableDuplicateLevels();

        if (GUILayout.Button("2. Все Point Lights → Baked + уменьшить радиус", GUILayout.Height(35)))
            OptimizeLights();

        if (GUILayout.Button("3. Пометить full_level как Static", GUILayout.Height(35)))
            MarkLevelStatic();

        if (GUILayout.Button("4. Добавить LOD Group на full_level", GUILayout.Height(35)))
            AddLODToLevel();

        if (GUILayout.Button("5. Включить GPU Instancing на всех материалах", GUILayout.Height(35)))
            EnableGPUInstancing();

        if (GUILayout.Button("6. Добавить ProximityCanvas на все Canvas в сцене", GUILayout.Height(35)))
            AddProximityCanvases();

        EditorGUILayout.Space();
        EditorStyles.helpBox.fontSize = 11;
        EditorGUILayout.HelpBox("Запусти все шаги по порядку.\nПосле шага 2 — сделай Window → Rendering → Lighting → Generate Lighting.", MessageType.Info);

        EditorGUILayout.Space();
        if (GUILayout.Button("ЗАПУСТИТЬ ВСЁ СРАЗУ", GUILayout.Height(50)))
        {
            DisableDuplicateLevels();
            OptimizeLights();
            MarkLevelStatic();
            EnableGPUInstancing();
            AddProximityCanvases();
            Debug.Log("VR Optimizer: все оптимизации применены!");
        }
    }

    static void DisableDuplicateLevels()
    {
        string[] duplicates = { "full_level (1)", "full_level (2)", "copy", "copy (1)", "copy (2)" };
        int count = 0;
        foreach (string name in duplicates)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                obj.SetActive(false);
                EditorUtility.SetDirty(obj);
                count++;
                Debug.Log($"VR Optimizer: отключён '{name}'");
            }
        }
        Debug.Log($"VR Optimizer: отключено {count} дублей. Tris должны упасть на ~65%");
    }

    static void OptimizeLights()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int converted = 0;
        int removed = 0;

        List<Light> pointLights = new List<Light>();
        foreach (Light l in lights)
            if (l.type == LightType.Point) pointLights.Add(l);

        // Оставляем только 8 самых ярких Point Lights
        pointLights.Sort((a, b) => b.intensity.CompareTo(a.intensity));

        for (int i = 0; i < pointLights.Count; i++)
        {
            Light l = pointLights[i];
            if (i < 8)
            {
                // Топ-8 — переводим в Baked
                l.lightmapBakeType = LightmapBakeType.Baked;
                // Уменьшаем радиус если слишком большой
                if (l.range > 15f) l.range = 10f;
                EditorUtility.SetDirty(l);
                converted++;
            }
            else
            {
                // Остальные — отключаем
                l.gameObject.SetActive(false);
                EditorUtility.SetDirty(l.gameObject);
                removed++;
            }
        }

        // Directional и Spot тоже в Baked
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional || l.type == LightType.Spot)
            {
                l.lightmapBakeType = LightmapBakeType.Baked;
                EditorUtility.SetDirty(l);
                converted++;
            }
        }

        Debug.Log($"VR Optimizer: {converted} lights → Baked, {removed} лишних lights отключено");
        Debug.Log("Теперь запусти: Window → Rendering → Lighting → Generate Lighting");
    }

    static void MarkLevelStatic()
    {
        string[] levelNames = { "full_level", "PROB3" };
        int count = 0;
        foreach (string name in levelNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                SetStaticRecursive(obj);
                count++;
            }
        }
        Debug.Log($"VR Optimizer: {count} объектов уровня помечены как Static");
    }

    static void SetStaticRecursive(GameObject obj)
    {
        GameObjectUtility.SetStaticEditorFlags(obj,
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.NavigationStatic);
        EditorUtility.SetDirty(obj);
        foreach (Transform child in obj.transform)
            SetStaticRecursive(child.gameObject);
    }

    static void AddLODToLevel()
    {
        GameObject level = GameObject.Find("full_level");
        if (level == null)
        {
            Debug.LogWarning("VR Optimizer: full_level не найден в сцене");
            return;
        }

        if (level.GetComponent<LODGroup>() != null)
        {
            Debug.Log("VR Optimizer: LOD Group уже есть на full_level");
            return;
        }

        LODGroup lodGroup = level.AddComponent<LODGroup>();
        Renderer[] renderers = level.GetComponentsInChildren<Renderer>();

        LOD[] lods = new LOD[2];
        lods[0] = new LOD(0.15f, renderers); // Показывать при 15%+ экрана
        lods[1] = new LOD(0.02f, new Renderer[0]); // Скрыть при < 2%

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
        EditorUtility.SetDirty(level);

        Debug.Log("VR Optimizer: LOD Group добавлен на full_level");
    }

    static void AddProximityCanvases()
    {
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int added = 0;
        int skipped = 0;

        foreach (Canvas canvas in allCanvases)
        {
            // Пропускаем корневые системные Canvas (EventSystem и т.п.)
            if (canvas.renderMode != UnityEngine.RenderMode.WorldSpace)
            {
                skipped++;
                continue;
            }

            // Если уже есть ProximityCanvas — не добавлять
            if (canvas.GetComponent<ProximityCanvas>() != null)
            {
                skipped++;
                continue;
            }

            ProximityCanvas pc = canvas.gameObject.AddComponent<ProximityCanvas>();
            pc.activationDistance = 3f;
            pc.checkEveryFrames = 4;
            EditorUtility.SetDirty(canvas.gameObject);
            added++;
        }

        Debug.Log($"VR Optimizer: ProximityCanvas добавлен на {added} Canvas. Пропущено: {skipped}");
    }

    static void EnableGPUInstancing()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && !mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                count++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"VR Optimizer: GPU Instancing включён на {count} материалах");
    }
}
