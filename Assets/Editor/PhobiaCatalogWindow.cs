using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class PhobiaCatalogWindow : EditorWindow
{
    const string CatalogPath = "Assets/Clicker/ClickerCatalog.asset";
    const string RootFolder = "Assets/Clicker/Phobias";
    const string TemplateMaterial = "Assets/Clicker/Materials/Relic_Common.mat";

    ClickerCatalog _catalog;
    Vector2 _listScroll;
    Vector2 _formScroll;
    string _displayName = "";
    string _id = "";
    bool _idLocked;
    Rarity _rarity = Rarity.Common;
    Texture _art;
    GameObject _existingPrefab;
    string _artist = "";
    string _description = "";
    string _notes = "";
    string _status = "";
    MessageType _statusType = MessageType.Info;

    [MenuItem("Phobia/Phobia Catalog")]
    public static void Open()
    {
        var window = GetWindow<PhobiaCatalogWindow>("Phobia Catalog");
        window.minSize = new Vector2(480, 680);
        window.Show();
    }

    void OnEnable()
    {
        if (_catalog == null)
            _catalog = AssetDatabase.LoadAssetAtPath<ClickerCatalog>(CatalogPath);
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Add Phobia Characters", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Add a named phobia to the catalog. Use 2D art, a 3D model, or both. The 3D model is what appears on the shelf and in inspect. If you only add 2D art, a double-sided plane is created from it. 2D art is also used in the collection list.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        _catalog = (ClickerCatalog)EditorGUILayout.ObjectField("Catalog", _catalog, typeof(ClickerCatalog), false);
        if (EditorGUI.EndChangeCheck())
            _status = "";

        if (_catalog == null)
        {
            EditorGUILayout.HelpBox($"Assign {CatalogPath} to continue.", MessageType.Warning);
            if (GUILayout.Button("Find Clicker Catalog"))
            {
                _catalog = AssetDatabase.LoadAssetAtPath<ClickerCatalog>(CatalogPath);
                if (_catalog == null)
                    SetStatus("No ClickerCatalog found at " + CatalogPath, MessageType.Error);
            }

            return;
        }

        DrawExisting();
        EditorGUILayout.Space(10);
        DrawAddForm();

        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, _statusType);
    }

    void DrawExisting()
    {
        var count = _catalog.phobias != null ? _catalog.phobias.Length : 0;
        EditorGUILayout.LabelField($"In catalog  ({count})", EditorStyles.boldLabel);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(240));
        if (count == 0)
            EditorGUILayout.LabelField("None yet.", EditorStyles.miniLabel);

        for (var i = 0; i < count; i++)
        {
            var def = _catalog.phobias[i];
            if (def == null)
                continue;

            EditorGUILayout.BeginHorizontal("box");
            var swatchRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(swatchRect, RarityStyle.Color(def.rarity));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(string.IsNullOrEmpty(def.displayName) ? "(unnamed)" : def.displayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{def.rarity}    id: {(string.IsNullOrEmpty(def.id) ? "—" : def.id)}", EditorStyles.miniLabel);
            EditorGUI.BeginChangeCheck();
            var artist = EditorGUILayout.TextField("Artist", def.artist ?? "");
            var description = EditorGUILayout.TextArea(
                string.IsNullOrEmpty(def.description) ? "" : def.description,
                GUILayout.MinHeight(36));
            var art = (Texture)EditorGUILayout.ObjectField("2D Art", def.art, typeof(Texture), false);
            var model = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("3D Model", "Used on the shelf and in inspect. Replaces the generated plane if you assign one."),
                def.prefab,
                typeof(GameObject),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_catalog, "Edit Phobia Details");
                def.artist = artist;
                def.description = description;
                def.art = art;
                def.prefab = model;
                EditorUtility.SetDirty(_catalog);
            }
            EditorGUILayout.EndVertical();

            GUI.enabled = def.prefab != null;
            if (GUILayout.Button("Ping", GUILayout.Width(48)))
                EditorGUIUtility.PingObject(def.prefab != null ? def.prefab : (Object)def.art);
            GUI.enabled = true;

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                if (EditorUtility.DisplayDialog(
                        "Remove phobia",
                        $"Remove '{def.Label}' from the catalog?\nAssets on disk are kept.",
                        "Remove",
                        "Cancel"))
                    RemoveAt(i);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawAddForm()
    {
        EditorGUILayout.LabelField("New phobia", EditorStyles.boldLabel);
        _formScroll = EditorGUILayout.BeginScrollView(_formScroll);

        EditorGUI.BeginChangeCheck();
        _displayName = EditorGUILayout.TextField("Name", _displayName);
        if (EditorGUI.EndChangeCheck() && !_idLocked)
            _id = Slug(_displayName);

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(_idLocked);
        _id = EditorGUILayout.TextField("Id", _id);
        EditorGUI.EndDisabledGroup();
        _idLocked = GUILayout.Toggle(_idLocked, "Lock", "Button", GUILayout.Width(48));
        EditorGUILayout.EndHorizontal();

        _rarity = (Rarity)EditorGUILayout.EnumPopup("Rarity", _rarity);
        var artObj = EditorGUILayout.ObjectField(
            new GUIContent("2D Art", "Used in the collection list. If no 3D model is set, a double-sided plane is generated from this."),
            _art,
            typeof(Object),
            false);
        if (artObj is Texture texture)
            _art = texture;
        else if (artObj is Sprite sprite)
            _art = sprite.texture;
        else if (artObj == null)
            _art = null;
        _existingPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("3D Model", "Prefab or FBX for the shelf and inspect popup. Leave empty to use a plane made from the 2D art."),
            _existingPrefab,
            typeof(GameObject),
            false);
        _artist = EditorGUILayout.TextField("Artist", _artist);
        EditorGUILayout.LabelField("Description");
        _description = EditorGUILayout.TextArea(_description ?? "", GUILayout.MinHeight(56));
        _notes = EditorGUILayout.TextField("Notes (internal)", _notes);

        if (_art != null)
        {
            var preview = AssetPreview.GetAssetPreview(_art) ?? _art;
            GUILayout.Label(preview, GUILayout.Height(72));
        }

        EditorGUILayout.Space(6);
        GUI.enabled = CanCreate(out _);
        if (GUILayout.Button("Add to catalog", GUILayout.Height(32)))
            CreatePhobia();
        GUI.enabled = true;

        if (!CanCreate(out var reason) && !string.IsNullOrWhiteSpace(_displayName))
            EditorGUILayout.HelpBox(reason, MessageType.Warning);

        EditorGUILayout.EndScrollView();
    }

    bool CanCreate(out string reason)
    {
        reason = "";
        if (string.IsNullOrWhiteSpace(_displayName))
        {
            reason = "Enter a name.";
            return false;
        }

        var id = string.IsNullOrWhiteSpace(_id) ? Slug(_displayName) : Slug(_id);
        if (string.IsNullOrEmpty(id))
        {
            reason = "Id needs at least one letter or number.";
            return false;
        }

        if (FindIndex(id) >= 0)
        {
            reason = $"Id '{id}' is already in the catalog.";
            return false;
        }

        if (_existingPrefab == null && _art == null)
        {
            reason = "Add 2D art or a 3D model.";
            return false;
        }

        return true;
    }

    void CreatePhobia()
    {
        if (!CanCreate(out var reason))
        {
            SetStatus(reason, MessageType.Error);
            return;
        }

        var id = string.IsNullOrWhiteSpace(_id) ? Slug(_displayName) : Slug(_id);
        GameObject prefab = _existingPrefab;
        if (prefab == null)
        {
            prefab = BuildPrefab(id, _displayName.Trim(), _rarity, _art);
            if (prefab == null)
            {
                SetStatus("Could not create prefab.", MessageType.Error);
                return;
            }
        }

        var so = new SerializedObject(_catalog);
        var list = so.FindProperty("phobias");
        if (list == null)
            list = so.FindProperty("relics");
        if (list == null)
        {
            SetStatus("Catalog is missing the phobias list.", MessageType.Error);
            return;
        }

        Undo.RecordObject(_catalog, "Add Phobia");
        list.arraySize++;
        var entry = list.GetArrayElementAtIndex(list.arraySize - 1);
        SetString(entry, "id", id);
        SetString(entry, "displayName", _displayName.Trim());
        entry.FindPropertyRelative("rarity").enumValueIndex = (int)_rarity;
        entry.FindPropertyRelative("art").objectReferenceValue = _art;
        entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        SetString(entry, "artist", _artist);
        SetString(entry, "description", _description);
        SetString(entry, "notes", _notes);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(_catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SetStatus($"Added {_displayName.Trim()} ({_rarity}).", MessageType.Info);
        EditorGUIUtility.PingObject(prefab);
        ClearForm();
    }

    GameObject BuildPrefab(string id, string displayName, Rarity rarity, Texture art)
    {
        EnsureFolder(RootFolder);
        var folder = $"{RootFolder}/{id}";
        EnsureFolder(folder);

        var matPath = $"{folder}/{id}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            if (!AssetDatabase.CopyAsset(TemplateMaterial, matPath))
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    return null;
                AssetDatabase.CreateAsset(new Material(shader), matPath);
            }

            mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        }

        if (mat == null)
            return null;

        ApplyArtMaterial(mat, art, rarity);
        EditorUtility.SetDirty(mat);

        var prefabPath = $"{folder}/{id}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject contents;
        if (existing != null)
            contents = PrefabUtility.LoadPrefabContents(prefabPath);
        else
            contents = CreateCardObject();

        contents.name = displayName;
        ApplyCardShape(contents, rarity);
        if (contents.TryGetComponent<Renderer>(out var renderer))
            renderer.sharedMaterial = mat;

        if (existing != null)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }
        else
        {
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            Object.DestroyImmediate(contents);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    static GameObject CreateCardObject()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        if (go.TryGetComponent<Collider>(out var collider))
            Object.DestroyImmediate(collider);
        return go;
    }

    static void ApplyCardShape(GameObject go, Rarity rarity)
    {
        var height = rarity == Rarity.Legendary ? 0.78f : 0.64f;
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        go.transform.localScale = new Vector3(height * 0.75f, height, 1f);

        var mesh = BuiltinQuad();
        if (mesh != null && go.TryGetComponent<MeshFilter>(out var filter))
            filter.sharedMesh = mesh;
        if (go.TryGetComponent<Collider>(out var collider))
            Object.DestroyImmediate(collider);
    }

    static Mesh BuiltinQuad()
    {
        var temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var mesh = temp.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
        Object.DestroyImmediate(temp);
        return mesh;
    }

    static void ApplyArtMaterial(Material mat, Texture art, Rarity rarity)
    {
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit != null)
            mat.shader = unlit;

        var color = art != null ? Color.white : RarityStyle.Color(rarity);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", art);
        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", art);
        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", 0f);
        mat.doubleSidedGI = true;

        if (art != null)
        {
            if (mat.HasProperty("_AlphaClip"))
                mat.SetFloat("_AlphaClip", 1f);
            if (mat.HasProperty("_Cutoff"))
                mat.SetFloat("_Cutoff", 0.2f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = 2450;
        }
    }

    void RemoveAt(int index)
    {
        var so = new SerializedObject(_catalog);
        var list = so.FindProperty("phobias") ?? so.FindProperty("relics");
        if (list == null || index < 0 || index >= list.arraySize)
            return;

        Undo.RecordObject(_catalog, "Remove Phobia");
        list.DeleteArrayElementAtIndex(index);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(_catalog);
        AssetDatabase.SaveAssets();
        SetStatus("Removed from catalog. Prefab/material files were kept.", MessageType.Info);
    }

    int FindIndex(string id)
    {
        if (_catalog.phobias == null)
            return -1;
        for (var i = 0; i < _catalog.phobias.Length; i++)
        {
            if (_catalog.phobias[i] != null && _catalog.phobias[i].id == id)
                return i;
        }

        return -1;
    }

    void ClearForm()
    {
        _displayName = "";
        _id = "";
        _idLocked = false;
        _art = null;
        _existingPrefab = null;
        _artist = "";
        _description = "";
        _notes = "";
    }

    void SetStatus(string message, MessageType type)
    {
        _status = message;
        _statusType = type;
    }

    static void SetString(SerializedProperty entry, string field, string value)
    {
        var prop = entry.FindPropertyRelative(field);
        if (prop != null)
            prop.stringValue = value ?? "";
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            return;
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static string Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var sb = new StringBuilder();
        foreach (var c in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c is ' ' or '-' or '_' && sb.Length > 0 && sb[^1] != '_')
                sb.Append('_');
        }

        return sb.ToString().Trim('_');
    }
}

[CustomEditor(typeof(ClickerCatalog))]
public class ClickerCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Phobia Catalog Tool", GUILayout.Height(28)))
            PhobiaCatalogWindow.Open();

        EditorGUILayout.Space(6);
        DrawDefaultInspector();
    }
}
