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
    const string JobPrefabPath = "Assets/Clicker/Prefabs/WorkJobOption.prefab";
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
            SyncScreen(screen, catalog, warn: true);

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
        var wanted = SortedPhobias(catalog);
        var overlay = FindChild(screen.transform, "CollectionOverlay");
        var content = FindContent(screen, overlay);
        if (content != null && !IdsMatch(content, wanted))
            return true;

        var work = screen.GetComponentInChildren<WorkScreen>(true);
        if (work == null)
            return false;

        var workContent = work.transform.Find("Panel/Viewport/Content") as RectTransform;
        if (workContent != null && !IdsMatch(workContent, wanted))
            return true;

        var jobsRoot = work.transform.Find("Panel/Jobs");
        return jobsRoot != null
            && jobsRoot.GetComponentsInChildren<WorkJobOption>(true).Length != MissionRules.Jobs.Length;
    }

    static bool IdsMatch(RectTransform content, List<PhobiaDefinition> wanted)
    {
        var ids = new HashSet<string>();
        foreach (var row in content.GetComponentsInChildren<CollectionRow>(true))
        {
            if (row != null && !string.IsNullOrEmpty(row.phobiaId))
                ids.Add(row.phobiaId);
        }

        if (ids.Count != wanted.Count)
            return false;
        foreach (var def in wanted)
        {
            if (!ids.Contains(def.id))
                return false;
        }

        return true;
    }

    static void SyncScreen(CollectionScreen screen, ClickerCatalog catalog, bool warn = false)
    {
        if (screen == null)
            return;

        Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Sync Collection Screen");
        BindSceneRefs(screen, warn);
        EnsureRows(screen, catalog);
        EnsureWorkRows(screen, catalog);
        EditorUtility.SetDirty(screen);
        EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
    }

    static void BindSceneRefs(CollectionScreen screen, bool warn)
    {
        BindCollection(screen, warn);
        BindWork(screen, warn);
        BindExit(screen, warn);
        BindReveal(screen, warn);
    }

    static void BindCollection(CollectionScreen screen, bool warn)
    {
        var overlay = FindChild(screen.transform, "CollectionOverlay");
        if (overlay == null)
        {
            if (warn)
                Debug.LogWarning("HUD is missing CollectionOverlay. Edit it in the scene; sync will not recreate chrome.", screen);
            return;
        }

        var so = new SerializedObject(screen);
        so.FindProperty("overlay").objectReferenceValue = overlay;

        var inventory = screen.transform.Find("Inventory");
        if (inventory != null)
        {
            AssignIfNull(so, "openButton", inventory.GetComponent<Button>());
            var label = inventory.Find("RelicsLabel");
            if (label != null)
                AssignIfNull(so, "collectionTab", label.GetComponent<Text>());
        }

        AssignIfNull(so, "closeButton", FindNamed<Button>(overlay.transform, "Panel/Close"));
        AssignIfNull(so, "progressText", FindNamed<Text>(overlay.transform, "Panel/Progress"));
        var content = overlay.transform.Find("Panel/Viewport/Content") as RectTransform;
        if (so.FindProperty("content").objectReferenceValue == null)
            so.FindProperty("content").objectReferenceValue = content;
        AssignIfNull(so, "emptyText", FindNamed<Text>(overlay.transform, "Panel/Viewport/Empty"));

        var work = overlay.transform.Find("Panel/Work") ?? overlay.transform.Find("Panel/MissionBar/Action");
        if (work != null)
            AssignIfNull(so, "workButton", work.GetComponent<Button>());

        var merge = overlay.transform.Find("Panel/Merge");
        if (merge != null)
        {
            AssignIfNull(so, "mergeButton", merge.GetComponent<Button>());
            AssignIfNull(so, "mergeImage", merge.GetComponent<Image>());
            AssignIfNull(so, "mergeLabel", FindNamed<Text>(merge, "Label"));
        }
        else if (warn)
        {
            Debug.LogWarning("CollectionOverlay is missing Panel/Merge.", overlay);
        }

        var confirm = overlay.transform.Find("MergeConfirm");
        if (confirm != null)
        {
            AssignIfNull(so, "mergeConfirm", confirm.gameObject);
            AssignIfNull(so, "mergeConfirmMessage", FindNamed<Text>(confirm, "Panel/Message"));
            AssignIfNull(so, "mergeConfirmButton", FindNamed<Button>(confirm, "Panel/Confirm"));
            AssignIfNull(so, "mergeCancelButton", FindNamed<Button>(confirm, "Panel/Cancel"));
        }
        else if (warn)
        {
            Debug.LogWarning("CollectionOverlay is missing MergeConfirm.", overlay);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BindWork(CollectionScreen screen, bool warn)
    {
        var existing = screen.transform.Find("WorkOverlay");
        if (existing == null)
        {
            if (warn)
                Debug.LogWarning("HUD is missing WorkOverlay. Edit it in the scene; sync will not recreate chrome.", screen);
            return;
        }

        var overlay = existing.gameObject;
        var work = overlay.GetComponent<WorkScreen>();
        if (work == null)
        {
            if (warn)
                Debug.LogWarning("WorkOverlay has no WorkScreen component.", overlay);
            return;
        }

        var restored = false;
        if (!overlay.activeSelf)
        {
            overlay.SetActive(true);
            restored = true;
        }

        try
        {
            EnsureJobCards(overlay.transform.Find("Panel/Jobs"));
        }
        finally
        {
            if (restored)
                overlay.SetActive(false);
        }

        var collectionSo = new SerializedObject(screen);
        var workSo = new SerializedObject(work);
        if (workSo.FindProperty("game").objectReferenceValue == null)
            workSo.FindProperty("game").objectReferenceValue = collectionSo.FindProperty("game")?.objectReferenceValue;
        workSo.FindProperty("overlay").objectReferenceValue = overlay;
        AssignIfNull(workSo, "closeButton", FindNamed<Button>(overlay.transform, "Panel/Close"));
        AssignIfNull(workSo, "statusText", FindNamed<Text>(overlay.transform, "Panel/Status"));
        var content = overlay.transform.Find("Panel/Viewport/Content") as RectTransform;
        if (workSo.FindProperty("content").objectReferenceValue == null)
            workSo.FindProperty("content").objectReferenceValue = content;
        AssignIfNull(workSo, "emptyText", FindNamed<Text>(overlay.transform, "Panel/Viewport/Empty"));
        var jobProp = workSo.FindProperty("jobs");
        if (jobProp != null)
        {
            var options = overlay.GetComponentsInChildren<WorkJobOption>(true);
            jobProp.arraySize = options.Length;
            for (var i = 0; i < options.Length; i++)
                jobProp.GetArrayElementAtIndex(i).objectReferenceValue = options[i];
        }

        workSo.ApplyModifiedPropertiesWithoutUndo();
        AssignIfNull(collectionSo, "workScreen", work);
        collectionSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(work);
    }

    static void BindExit(CollectionScreen screen, bool warn)
    {
        var exit = screen.transform.Find("Exit");
        if (exit == null)
        {
            if (warn)
                Debug.LogWarning("HUD is missing Exit. Edit it in the scene; sync will not recreate chrome.", screen);
            return;
        }

        var hud = screen.GetComponent<ClickerHud>();
        if (hud == null)
            return;

        var so = new SerializedObject(hud);
        AssignIfNull(so, "exitButton", exit.GetComponent<Button>());
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(hud);
    }

    static void BindReveal(CollectionScreen screen, bool warn)
    {
        var reveal = screen.GetComponent<PhobiaReveal>();
        var overlay = FindChild(screen.transform, "PhobiaReveal");
        if (reveal == null || overlay == null)
        {
            if (warn)
                Debug.LogWarning("HUD is missing PhobiaReveal overlay. Edit it in the scene; sync will not recreate chrome.", screen);
            return;
        }

        var so = new SerializedObject(reveal);
        var gameSo = new SerializedObject(screen);
        if (so.FindProperty("game") != null && so.FindProperty("game").objectReferenceValue == null)
            so.FindProperty("game").objectReferenceValue = gameSo.FindProperty("game")?.objectReferenceValue;
        so.FindProperty("overlay").objectReferenceValue = overlay;
        AssignIfNull(so, "bannerText", FindNamed<Text>(overlay.transform, "Banner"));
        AssignIfNull(so, "nameText", FindNamed<Text>(overlay.transform, "Name"));
        AssignIfNull(so, "rarityText", FindNamed<Text>(overlay.transform, "Rarity"));
        AssignIfNull(so, "artistText", FindNamed<Text>(overlay.transform, "Artist"));
        AssignIfNull(so, "descriptionText", FindNamed<Text>(overlay.transform, "Description"));
        AssignIfNull(so, "modelView", FindNamed<RawImage>(overlay.transform, "ModelView"));
        AssignIfNull(so, "artView", FindNamed<RawImage>(overlay.transform, "Art"));
        AssignIfNull(so, "closeButton", FindNamed<Button>(overlay.transform, "Close"));
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(reveal);
    }

    static void EnsureRows(CollectionScreen screen, ClickerCatalog catalog)
    {
        var so = new SerializedObject(screen);
        var overlay = so.FindProperty("overlay")?.objectReferenceValue as GameObject
            ?? FindChild(screen.transform, "CollectionOverlay");
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
            FillPhobiaRows(content, empty, wanted);
        }
        finally
        {
            if (restoredOverlay)
                overlay.SetActive(false);
        }
    }

    static void EnsureWorkRows(CollectionScreen screen, ClickerCatalog catalog)
    {
        var work = screen.GetComponentInChildren<WorkScreen>(true);
        if (work == null)
            return;

        var overlay = work.gameObject;
        var content = overlay.transform.Find("Panel/Viewport/Content") as RectTransform;
        var empty = FindNamed<Text>(overlay.transform, "Panel/Viewport/Empty");
        if (content == null)
            return;

        if (catalog == null)
            catalog = LoadCatalog();

        var restored = false;
        if (!overlay.activeSelf)
        {
            overlay.SetActive(true);
            restored = true;
        }

        try
        {
            FillPhobiaRows(content, empty, SortedPhobias(catalog));
        }
        finally
        {
            if (restored)
                overlay.SetActive(false);
        }
    }

    static void FillPhobiaRows(RectTransform content, Text empty, List<PhobiaDefinition> wanted)
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

    static void EnsureJobCards(Transform jobsRoot)
    {
        if (jobsRoot == null)
            return;

        var prefab = EnsureJobPrefab();
        var existing = new List<WorkJobOption>(jobsRoot.GetComponentsInChildren<WorkJobOption>(true));
        var byId = new Dictionary<string, WorkJobOption>();
        foreach (var option in existing)
        {
            if (option == null || string.IsNullOrEmpty(option.jobId) || byId.ContainsKey(option.jobId))
            {
                if (option != null)
                    Undo.DestroyObjectImmediate(option.gameObject);
                continue;
            }

            byId[option.jobId] = option;
        }

        for (var i = 0; i < MissionRules.Jobs.Length; i++)
        {
            var def = MissionRules.Jobs[i];
            if (!byId.TryGetValue(def.id, out var option) || option == null)
            {
                option = InstantiateJob(prefab, jobsRoot);
                byId[def.id] = option;
            }

            ApplyJob(option, def);
            option.transform.SetSiblingIndex(i);
        }
    }

    static WorkJobOption InstantiateJob(WorkJobOption prefab, Transform parent)
    {
        if (prefab != null)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, parent) as GameObject;
            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Work Job");
                var option = instance.GetComponent<WorkJobOption>();
                if (option != null)
                    return option;
            }
        }

        var created = BuildJobTemplate(parent);
        Undo.RegisterCreatedObjectUndo(created.gameObject, "Add Work Job");
        return created;
    }

    static void ApplyJob(WorkJobOption option, JobDefinition def)
    {
        if (option == null || def == null)
            return;

        option.gameObject.name = "Job_" + def.id;
        option.jobId = def.id;
        if (option.titleText != null)
            option.titleText.text = def.displayName;
        if (option.durationText != null)
            option.durationText.text = def.DurationLabel;
        if (option.rewardText != null)
            option.rewardText.text = $"+{Mathf.FloorToInt((float)MissionRules.Reward(Rarity.Common, 1, def.id))}";
        if (option.sendLabel != null)
            option.sendLabel.text = "SEND";
        EditorUtility.SetDirty(option);
    }

    static WorkJobOption EnsureJobPrefab()
    {
        EnsureFolder(PrefabFolder);
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(JobPrefabPath);
        if (existing != null)
        {
            var option = existing.GetComponent<WorkJobOption>();
            if (option != null)
                return option;
        }

        var canvasGo = new GameObject("WorkJobPrefabTemp", typeof(RectTransform), typeof(Canvas));
        try
        {
            var template = BuildJobTemplate(canvasGo.transform);
            template.gameObject.name = "WorkJobOption";
            var prefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, JobPrefabPath);
            AssetDatabase.SaveAssets();
            return prefab != null ? prefab.GetComponent<WorkJobOption>() : null;
        }
        finally
        {
            Object.DestroyImmediate(canvasGo);
        }
    }

    static WorkJobOption BuildJobTemplate(Transform parent)
    {
        var card = Create("WorkJobOption", parent);
        card.sizeDelta = new Vector2(260f, 180f);
        var layout = card.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 160f;
        layout.preferredHeight = 180f;
        layout.minWidth = 200f;
        var bg = card.gameObject.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.1f, 0.16f, 1f);

        var title = CreateText("Title", card, "Quick Job", 22, TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(title.rectTransform, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.96f));
        title.raycastTarget = false;

        var duration = CreateText("Duration", card, "30s", 18, TextAnchor.MiddleCenter);
        duration.color = new Color(0.7f, 0.68f, 0.76f, 1f);
        Stretch(duration.rectTransform, new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.74f));
        duration.raycastTarget = false;

        var reward = CreateText("Reward", card, "+48", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
        reward.color = Accent;
        Stretch(reward.rectTransform, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.58f));
        reward.raycastTarget = false;

        var send = Create("Send", card);
        Stretch(send, new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.32f));
        var sendImage = send.gameObject.AddComponent<Image>();
        sendImage.color = Accent;
        var sendButton = send.gameObject.AddComponent<Button>();
        sendButton.targetGraphic = sendImage;
        sendButton.transition = Selectable.Transition.None;
        var sendLabel = CreateText("Label", send, "SEND", 20, TextAnchor.MiddleCenter, FontStyle.Bold);
        sendLabel.color = Color.white;
        sendLabel.raycastTarget = false;
        Stretch(sendLabel.rectTransform);

        var option = card.gameObject.AddComponent<WorkJobOption>();
        option.background = bg;
        option.titleText = title;
        option.durationText = duration;
        option.rewardText = reward;
        option.sendButton = sendButton;
        option.sendImage = sendImage;
        option.sendLabel = sendLabel;
        return option;
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

    static GameObject FindChild(Transform parent, string name)
    {
        var found = parent.Find(name);
        return found != null ? found.gameObject : null;
    }

    static RectTransform FindContent(CollectionScreen screen, GameObject overlay)
    {
        var so = new SerializedObject(screen);
        var content = so.FindProperty("content")?.objectReferenceValue as RectTransform;
        if (content != null)
            return content;
        if (overlay == null)
            return null;
        return overlay.transform.Find("Panel/Viewport/Content") as RectTransform;
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
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
