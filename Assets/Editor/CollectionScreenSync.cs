using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CollectionScreenSync
{
    const string CatalogPath = "Assets/Clicker/ClickerCatalog.asset";
    const string RowPrefabPath = "Assets/Clicker/Prefabs/CollectionRow.prefab";
    const string PrefabFolder = "Assets/Clicker/Prefabs";

    static readonly Color Accent = new(0.95f, 0.42f, 0.55f);

    static CollectionScreenSync()
    {
        EditorApplication.delayCall += MigrateLoadedScenes;
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += MigrateLoadedScenes;
    }

    [MenuItem("Phobia/Sync Collection Screen")]
    public static void SyncFromCatalog()
    {
        var catalog = LoadCatalog();
        var screens = FindScreens();
        if (screens.Count == 0)
        {
            Debug.LogWarning("No CollectionScreen in a loaded scene. Open SampleScene and run Phobia/Sync Collection Screen.");
            return;
        }

        foreach (var screen in screens)
            SyncScreen(screen, catalog);

        Debug.Log($"Collection screen synced ({screens.Count} scene{(screens.Count == 1 ? "" : "s")}).");
    }

    public static void SyncFromCatalogAndSave()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        EditorSceneManager.OpenScene(scenePath);
        SyncFromCatalog();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    public static void SyncFromCatalogQuiet()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var catalog = LoadCatalog();
        foreach (var screen in FindScreens())
            SyncScreen(screen, catalog);
    }

    static void MigrateLoadedScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += MigrateLoadedScenes;
            return;
        }

        var catalog = LoadCatalog();
        foreach (var screen in FindScreens())
        {
            if (NeedsSync(screen, catalog))
                SyncScreen(screen, catalog);
        }
    }

    static bool NeedsSync(CollectionScreen screen, ClickerCatalog catalog)
    {
        var so = new SerializedObject(screen);
        var overlay = so.FindProperty("overlay")?.objectReferenceValue as GameObject;
        if (overlay == null)
            return true;

        var content = so.FindProperty("content")?.objectReferenceValue as RectTransform
            ?? overlay.transform.Find("Panel/Viewport/Content") as RectTransform;
        if (content == null)
            return true;

        var ids = new HashSet<string>();
        foreach (var row in content.GetComponentsInChildren<CollectionRow>(true))
        {
            if (row != null && !string.IsNullOrEmpty(row.phobiaId))
                ids.Add(row.phobiaId);
        }

        var wanted = SortedPhobias(catalog);
        if (ids.Count != wanted.Count)
            return true;
        foreach (var def in wanted)
        {
            if (!ids.Contains(def.id))
                return true;
        }

        return false;
    }

    static void SyncScreen(CollectionScreen screen, ClickerCatalog catalog)
    {
        if (screen == null)
            return;

        Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Sync Collection Screen");
        EnsureChrome(screen);
        EnsureOverlay(screen);
        EnsureRows(screen, catalog);
        EditorUtility.SetDirty(screen);
        EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
    }

    static void EnsureChrome(CollectionScreen screen)
    {
        var inventory = screen.transform.Find("Inventory");
        if (inventory == null)
            return;

        if (inventory.TryGetComponent<Image>(out var image))
            image.raycastTarget = true;

        var button = inventory.GetComponent<Button>();
        if (button == null)
            button = Undo.AddComponent<Button>(inventory.gameObject);
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;

        var so = new SerializedObject(screen);
        so.FindProperty("openButton").objectReferenceValue = button;
        var label = inventory.Find("RelicsLabel");
        if (label != null && label.TryGetComponent<Text>(out var tab))
        {
            so.FindProperty("collectionTab").objectReferenceValue = tab;
            if (tab.text == "RELICS" || string.IsNullOrEmpty(tab.text))
                tab.text = "COLLECTION";
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureOverlay(CollectionScreen screen)
    {
        var so = new SerializedObject(screen);
        var overlayProp = so.FindProperty("overlay");
        if (overlayProp == null)
            return;

        var overlay = overlayProp.objectReferenceValue as GameObject;
        if (overlay == null)
        {
            var existing = screen.transform.Find("CollectionOverlay");
            overlay = existing != null ? existing.gameObject : BuildOverlay(screen.transform);
        }

        overlay.transform.SetAsLastSibling();

        overlayProp.objectReferenceValue = overlay;
        AssignIfNull(so, "closeButton", FindNamed<Button>(overlay.transform, "Panel/Close"));
        AssignIfNull(so, "progressText", FindNamed<Text>(overlay.transform, "Panel/Progress"));
        var content = overlay.transform.Find("Panel/Viewport/Content") as RectTransform;
        if (so.FindProperty("content").objectReferenceValue == null)
            so.FindProperty("content").objectReferenceValue = content;
        AssignIfNull(so, "emptyText", FindNamed<Text>(overlay.transform, "Panel/Viewport/Empty"));
        AssignIfNull(so, "detailText", FindNamed<Text>(overlay.transform, "Panel/MissionBar/Detail"));
        var action = FindNamed<Button>(overlay.transform, "Panel/MissionBar/Action");
        AssignIfNull(so, "actionButton", action);
        if (action != null)
            AssignIfNull(so, "actionImage", action.GetComponent<Image>());
        AssignIfNull(so, "actionLabel", FindNamed<Text>(overlay.transform, "Panel/MissionBar/Action/Label"));
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureRows(CollectionScreen screen, ClickerCatalog catalog)
    {
        var so = new SerializedObject(screen);
        var overlay = so.FindProperty("overlay")?.objectReferenceValue as GameObject;
        var content = so.FindProperty("content")?.objectReferenceValue as RectTransform;
        var empty = so.FindProperty("emptyText")?.objectReferenceValue as Text;
        if (content == null && overlay != null)
            content = overlay.transform.Find("Panel/Viewport/Content") as RectTransform;
        if (content == null)
            return;

        if (catalog == null)
        {
            var game = so.FindProperty("game")?.objectReferenceValue as ClickerGame;
            catalog = game != null ? game.Catalog : LoadCatalog();
        }

        var wanted = SortedPhobias(catalog);
        var restoredOverlay = false;
        if (overlay != null && !overlay.activeSelf)
        {
            overlay.SetActive(true);
            restoredOverlay = true;
        }

        try
        {
            var rows = new List<CollectionRow>(content.GetComponentsInChildren<CollectionRow>(true));
            var byId = new Dictionary<string, CollectionRow>();
            foreach (var row in rows)
            {
                if (row == null)
                    continue;
                if (string.IsNullOrEmpty(row.phobiaId))
                    continue;
                if (byId.ContainsKey(row.phobiaId))
                {
                    DestroyRow(row);
                    continue;
                }

                byId[row.phobiaId] = row;
            }

            var prefab = EnsureRowPrefab();
            for (var i = 0; i < wanted.Count; i++)
            {
                var def = wanted[i];
                if (!byId.TryGetValue(def.id, out var row) || row == null)
                {
                    row = InstantiateRow(prefab, content);
                    if (row == null)
                    {
                        Debug.LogError($"Could not create collection row for '{def.id}'.", content);
                        continue;
                    }

                    byId[def.id] = row;
                }

                ApplyRow(row, def);
                row.transform.SetSiblingIndex(i);
            }

            foreach (var pair in byId)
            {
                if (wanted.Exists(def => def.id == pair.Key))
                    continue;
                DestroyRow(pair.Value);
            }

            if (empty != null)
                empty.gameObject.SetActive(wanted.Count == 0);
        }
        finally
        {
            if (restoredOverlay)
                overlay.SetActive(false);
        }
    }

    static CollectionRow InstantiateRow(CollectionRow prefab, RectTransform content)
    {
        if (prefab != null)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, content) as GameObject;
            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Collection Row");
                var row = instance.GetComponent<CollectionRow>();
                if (row != null)
                    return row;
            }
        }

        var created = BuildRowTemplate(content);
        if (created != null)
            Undo.RegisterCreatedObjectUndo(created.gameObject, "Add Collection Row");
        return created;
    }

    static void DestroyRow(CollectionRow row)
    {
        if (row == null)
            return;
        Undo.DestroyObjectImmediate(row.gameObject);
    }

    static void ApplyRow(CollectionRow row, PhobiaDefinition def)
    {
        if (row == null || def == null)
            return;

        row.gameObject.name = "Row_" + def.id;
        row.phobiaId = def.id;
        if (row.nameText != null)
            row.nameText.text = def.Label;
        if (row.rarityText != null)
        {
            row.rarityText.text = RarityStyle.Label(def.rarity);
            row.rarityText.color = RarityStyle.Color(def.rarity);
        }

        if (row.art != null)
        {
            row.art.texture = def.art;
            row.art.color = def.art != null ? Color.white : new Color(0.2f, 0.18f, 0.24f, 1f);
        }

        EditorUtility.SetDirty(row);
    }

    static CollectionRow EnsureRowPrefab()
    {
        EnsureFolder(PrefabFolder);
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
        if (existing != null)
        {
            var row = existing.GetComponent<CollectionRow>();
            if (row != null)
                return row;
        }

        var canvasGo = new GameObject("CollectionRowPrefabTemp", typeof(RectTransform), typeof(Canvas));
        try
        {
            var template = BuildRowTemplate(canvasGo.transform);
            template.gameObject.name = "CollectionRow";
            var prefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, RowPrefabPath);
            AssetDatabase.SaveAssets();
            return prefab != null ? prefab.GetComponent<CollectionRow>() : null;
        }
        finally
        {
            Object.DestroyImmediate(canvasGo);
        }
    }

    static GameObject BuildOverlay(Transform parent)
    {
        var overlay = Create("CollectionOverlay", parent);
        Stretch(overlay);
        var dim = overlay.gameObject.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.03f, 0.06f, 0.92f);
        dim.raycastTarget = true;

        var panel = Create("Panel", overlay);
        Stretch(panel, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.9f));
        var panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.06f, 0.12f, 0.98f);
        panelImage.raycastTarget = true;

        var title = CreateText("Title", panel, "COLLECTION", 36, TextAnchor.MiddleCenter, FontStyle.Bold);
        title.color = Color.white;
        Stretch(title.rectTransform, new Vector2(0.08f, 0.91f), new Vector2(0.78f, 1f));

        var progress = CreateText("Progress", panel, "0 / 0 unlocked", 22, TextAnchor.MiddleLeft);
        progress.color = new Color(0.7f, 0.68f, 0.76f, 1f);
        Stretch(progress.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.7f, 0.92f));

        var close = Create("Close", panel);
        Stretch(close, new Vector2(0.78f, 0.91f), new Vector2(0.98f, 0.99f), new Vector2(-8f, -8f), new Vector2(-8f, -8f));
        var closeImage = close.gameObject.AddComponent<Image>();
        closeImage.color = Accent;
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        var closeLabel = CreateText("Label", close, "CLOSE", 22, TextAnchor.MiddleCenter, FontStyle.Bold);
        closeLabel.color = Color.white;
        closeLabel.raycastTarget = false;
        Stretch(closeLabel.rectTransform);

        var viewport = Create("Viewport", panel);
        Stretch(viewport, new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.85f));
        viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var content = Create("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var empty = CreateText("Empty", viewport, "No phobias in the catalog yet.", 24, TextAnchor.MiddleCenter);
        empty.color = new Color(1f, 1f, 1f, 0.4f);
        Stretch(empty.rectTransform);
        empty.raycastTarget = false;

        var action = Create("MissionBar", panel);
        Stretch(action, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.22f));
        action.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.16f, 1f);

        var detail = CreateText("Detail", action, "Select an unlocked phobia to send gathering.", 20, TextAnchor.MiddleLeft);
        detail.color = new Color(0.86f, 0.84f, 0.9f, 1f);
        detail.horizontalOverflow = HorizontalWrapMode.Wrap;
        detail.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(detail.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.62f, 0.92f));

        var send = Create("Action", action);
        Stretch(send, new Vector2(0.66f, 0.16f), new Vector2(0.97f, 0.84f));
        var actionImage = send.gameObject.AddComponent<Image>();
        actionImage.color = Accent;
        var actionButton = send.gameObject.AddComponent<Button>();
        actionButton.targetGraphic = actionImage;
        var actionLabel = CreateText("Label", send, "SEND", 24, TextAnchor.MiddleCenter, FontStyle.Bold);
        actionLabel.color = Color.white;
        actionLabel.raycastTarget = false;
        Stretch(actionLabel.rectTransform);

        overlay.gameObject.SetActive(false);
        Undo.RegisterCreatedObjectUndo(overlay.gameObject, "Create Collection Overlay");
        return overlay.gameObject;
    }

    static CollectionRow BuildRowTemplate(Transform parent)
    {
        var row = Create("CollectionRow", parent);
        row.sizeDelta = new Vector2(860f, 92f);
        var layout = row.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 92f;
        layout.preferredWidth = 860f;
        layout.preferredHeight = 92f;
        var bg = row.gameObject.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.1f, 0.16f, 0.9f);
        bg.raycastTarget = false;

        var select = Create("Select", row);
        Stretch(select, new Vector2(0f, 0f), new Vector2(0.7f, 1f));
        var selectImage = select.gameObject.AddComponent<Image>();
        selectImage.color = Color.clear;
        var selectButton = select.gameObject.AddComponent<Button>();
        selectButton.transition = Selectable.Transition.None;
        selectButton.targetGraphic = selectImage;

        var artRt = Create("Art", row);
        Stretch(artRt, new Vector2(0.03f, 0.12f), new Vector2(0.18f, 0.88f));
        var art = artRt.gameObject.AddComponent<RawImage>();
        art.color = new Color(0.2f, 0.18f, 0.24f, 1f);
        art.raycastTarget = false;

        var name = CreateText("Name", row, "Phobia", 26, TextAnchor.MiddleLeft, FontStyle.Bold);
        Stretch(name.rectTransform, new Vector2(0.19f, 0.46f), new Vector2(0.56f, 0.9f));
        name.raycastTarget = false;

        var rarity = CreateText("Rarity", row, "COMMON", 18, TextAnchor.MiddleLeft);
        Stretch(rarity.rectTransform, new Vector2(0.19f, 0.1f), new Vector2(0.56f, 0.5f));
        rarity.raycastTarget = false;

        var count = CreateText("Count", row, "×0", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(count.rectTransform, new Vector2(0.56f, 0.12f), new Vector2(0.7f, 0.88f));
        count.raycastTarget = false;

        var inspect = Create("Inspect", row);
        Stretch(inspect, new Vector2(0.72f, 0.18f), new Vector2(0.97f, 0.82f));
        var inspectImage = inspect.gameObject.AddComponent<Image>();
        inspectImage.color = Accent;
        var inspectButton = inspect.gameObject.AddComponent<Button>();
        inspectButton.targetGraphic = inspectImage;
        inspectButton.transition = Selectable.Transition.None;
        var inspectLabel = CreateText("Label", inspect, "INSPECT", 16, TextAnchor.MiddleCenter, FontStyle.Bold);
        inspectLabel.color = Color.white;
        inspectLabel.raycastTarget = false;
        Stretch(inspectLabel.rectTransform);

        var rowComp = row.gameObject.AddComponent<CollectionRow>();
        rowComp.background = bg;
        rowComp.art = art;
        rowComp.nameText = name;
        rowComp.rarityText = rarity;
        rowComp.countText = count;
        rowComp.selectButton = selectButton;
        rowComp.inspectButton = inspectButton;
        rowComp.inspectImage = inspectImage;
        rowComp.inspectLabel = inspectLabel;
        return rowComp;
    }

    static List<PhobiaDefinition> SortedPhobias(ClickerCatalog catalog)
    {
        var defs = new List<PhobiaDefinition>();
        if (catalog == null || catalog.phobias == null)
            return defs;

        foreach (var def in catalog.phobias)
        {
            if (def != null && !string.IsNullOrEmpty(def.id))
                defs.Add(def);
        }

        defs.Sort((a, b) =>
        {
            var rarity = b.rarity.CompareTo(a.rarity);
            return rarity != 0 ? rarity : string.CompareOrdinal(a.Label, b.Label);
        });
        return defs;
    }

    static ClickerCatalog LoadCatalog()
    {
        return AssetDatabase.LoadAssetAtPath<ClickerCatalog>(CatalogPath);
    }

    static List<CollectionScreen> FindScreens()
    {
        var screens = new List<CollectionScreen>();
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;
            foreach (var root in scene.GetRootGameObjects())
                screens.AddRange(root.GetComponentsInChildren<CollectionScreen>(true));
        }

        return screens;
    }

    static void AssignIfNull(SerializedObject so, string property, Object value)
    {
        if (value == null)
            return;
        var prop = so.FindProperty(property);
        if (prop != null && prop.objectReferenceValue == null)
            prop.objectReferenceValue = value;
    }

    static T FindNamed<T>(Transform root, string path) where T : Component
    {
        var found = root.Find(path);
        return found != null ? found.GetComponent<T>() : null;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        var name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static RectTransform Create(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
            go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static Text CreateText(string name, Transform parent, string value, int size, TextAnchor align, FontStyle style = FontStyle.Normal)
    {
        var rt = Create(name, parent);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = UiFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = align;
        text.color = Color.white;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static Font UiFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    static void Stretch(RectTransform rt)
    {
        Stretch(rt, Vector2.zero, Vector2.one);
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        Stretch(rt, min, max, Vector2.zero, Vector2.zero);
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
