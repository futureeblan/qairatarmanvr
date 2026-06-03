using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class PolyCounter : EditorWindow
{
    class MeshInfo
    {
        public string name;
        public string path;
        public int triangles;
        public int vertices;
        public bool hasLOD;
        public bool isStatic;
        public GameObject go;
    }

    List<MeshInfo> _results = new List<MeshInfo>();
    Vector2 _scroll;
    int _totalTris;
    int _limit = 30;
    bool _onlyActive = true;

    [MenuItem("Tools/Poly Counter")]
    public static void ShowWindow() => GetWindow<PolyCounter>("Poly Counter");

    void OnGUI()
    {
        GUILayout.Label("Поиск тяжёлых объектов в сцене", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        _limit = EditorGUILayout.IntField("Показать топ:", _limit, GUILayout.Width(180));
        _onlyActive = EditorGUILayout.ToggleLeft("Только активные", _onlyActive);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (GUILayout.Button("СКАНИРОВАТЬ СЦЕНУ", GUILayout.Height(36)))
            ScanScene();

        if (_results.Count == 0) return;

        EditorGUILayout.Space(6);

        // Итог
        Color prev = GUI.color;
        GUI.color = _totalTris > 1_500_000 ? Color.red : _totalTris > 800_000 ? Color.yellow : Color.green;
        EditorGUILayout.HelpBox(
            $"Всего треугольников в сцене: {_totalTris:N0}  |  Цель для Quest 3: < 1 500 000",
            _totalTris > 1_500_000 ? MessageType.Error : MessageType.Warning);
        GUI.color = prev;

        EditorGUILayout.Space(4);

        // Заголовок таблицы
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#",       GUILayout.Width(28));
        GUILayout.Label("Объект",  GUILayout.Width(200));
        GUILayout.Label("Tris",    GUILayout.Width(80));
        GUILayout.Label("Verts",   GUILayout.Width(70));
        GUILayout.Label("LOD",     GUILayout.Width(36));
        GUILayout.Label("Static",  GUILayout.Width(46));
        GUILayout.Label("Действие");
        EditorGUILayout.EndHorizontal();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        for (int i = 0; i < Mathf.Min(_results.Count, _limit); i++)
        {
            var info = _results[i];

            // Цвет строки по весу
            GUI.color = info.triangles > 500_000 ? Color.red
                      : info.triangles > 100_000 ? new Color(1f, 0.6f, 0f)
                      : info.triangles > 30_000  ? Color.yellow
                      : Color.white;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"{i+1}",                         GUILayout.Width(28));
            GUILayout.Label(info.name,                         GUILayout.Width(200));
            GUILayout.Label($"{info.triangles:N0}",            GUILayout.Width(80));
            GUILayout.Label($"{info.vertices:N0}",             GUILayout.Width(70));
            GUILayout.Label(info.hasLOD ? "✓" : "✗",          GUILayout.Width(36));
            GUILayout.Label(info.isStatic ? "✓" : "✗",        GUILayout.Width(46));

            GUI.color = Color.white;

            // Кнопка выбора в сцене
            if (GUILayout.Button("Выбрать", GUILayout.Width(65)))
            {
                Selection.activeGameObject = info.go;
                SceneView.FrameLastActiveSceneView();
            }

            // Кнопка добавить LOD если нет
            if (!info.hasLOD && GUILayout.Button("+ LOD", GUILayout.Width(55)))
                AddSimpleLOD(info.go);

            // Кнопка пометить Static
            if (!info.isStatic && GUILayout.Button("Static", GUILayout.Width(52)))
            {
                GameObjectUtility.SetStaticEditorFlags(info.go,
                    StaticEditorFlags.ContributeGI |
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic |
                    StaticEditorFlags.BatchingStatic);
                EditorUtility.SetDirty(info.go);
            }

            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "🔴 > 500k tris  — обязательно LOD или Decimate в Blender\n" +
            "🟠 > 100k tris  — добавить LOD Group\n" +
            "🟡 > 30k tris   — пометить Static для батчинга",
            MessageType.None);
    }

    void ScanScene()
    {
        _results.Clear();
        _totalTris = 0;

        var flags = _onlyActive
            ? FindObjectsInactive.Exclude
            : FindObjectsInactive.Include;

        // Собираем MeshFilter (статичные меши)
        foreach (var mf in FindObjectsByType<MeshFilter>(flags, FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null) continue;
            int tris = mf.sharedMesh.triangles.Length / 3;
            int verts = mf.sharedMesh.vertexCount;
            _totalTris += tris;
            _results.Add(new MeshInfo
            {
                name     = mf.gameObject.name,
                path     = GetPath(mf.transform),
                triangles = tris,
                vertices  = verts,
                hasLOD   = mf.GetComponentInParent<LODGroup>() != null,
                isStatic  = mf.gameObject.isStatic,
                go        = mf.gameObject
            });
        }

        // Собираем SkinnedMeshRenderer (анимированные меши)
        foreach (var smr in FindObjectsByType<SkinnedMeshRenderer>(flags, FindObjectsSortMode.None))
        {
            if (smr.sharedMesh == null) continue;
            int tris = smr.sharedMesh.triangles.Length / 3;
            int verts = smr.sharedMesh.vertexCount;
            _totalTris += tris;
            _results.Add(new MeshInfo
            {
                name      = smr.gameObject.name,
                path      = GetPath(smr.transform),
                triangles = tris,
                vertices  = verts,
                hasLOD    = smr.GetComponentInParent<LODGroup>() != null,
                isStatic  = false,
                go        = smr.gameObject
            });
        }

        _results = _results.OrderByDescending(r => r.triangles).ToList();
    }

    void AddSimpleLOD(GameObject go)
    {
        if (go.GetComponentInParent<LODGroup>() != null) return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        LODGroup group = go.AddComponent<LODGroup>();
        LOD[] lods = new LOD[]
        {
            new LOD(0.15f, renderers),
            new LOD(0.02f, new Renderer[0])
        };
        group.SetLODs(lods);
        group.RecalculateBounds();
        EditorUtility.SetDirty(go);
        Debug.Log($"LOD Group добавлен на {go.name}");
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
