using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CollectionScreenSync
{
    const string CatalogPath = "Assets/Clicker/ClickerCatalog.asset";
    const string RowPrefabPath = "Assets/Clicker/Prefabs/CollectionRow.prefab";
    const string JobPrefabPath = "Assets/Clicker/Prefabs/WorkJobOption.prefab";

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

    public static void SyncFromCatalogQuiet()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var catalog = LoadCatalog();
        foreach (var screen in FindScreens())
            SyncScreen(screen, catalog);
    }

    static void SyncScreen(CollectionScreen screen, ClickerCatalog catalog)
    {
        if (screen == null)
            return;

        Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Sync Collection Screen");
        EnsureRows(screen, catalog);
        EnsureWorkRows(screen, catalog);
        EditorUtility.SetDirty(screen);
        EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
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

        WithActive(overlay, () => FillPhobiaRows(content, empty, SortedPhobias(catalog)));
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

        WithActive(overlay, () =>
        {
            FillPhobiaRows(content, empty, SortedPhobias(catalog));
            EnsureJobCards(overlay.transform.Find("Panel/Jobs"));
            BindJobOptions(work);
        });
        EditorUtility.SetDirty(work);
    }

    static void FillPhobiaRows(RectTransform content, Text empty, List<PhobiaDefinition> wanted)
    {
        var rows = new List<CollectionRow>(content.GetComponentsInChildren<CollectionRow>(true));
        var byId = new Dictionary<string, CollectionRow>();
        foreach (var row in rows)
        {
            if (row == null || string.IsNullOrEmpty(row.phobiaId))
                continue;
            if (byId.ContainsKey(row.phobiaId))
            {
                DestroyRow(row);
                continue;
            }

            byId[row.phobiaId] = row;
        }

        var prefab = LoadRowPrefab();
        for (var i = 0; i < wanted.Count; i++)
        {
            var def = wanted[i];
            if (!byId.TryGetValue(def.id, out var row) || row == null)
            {
                row = InstantiateRow(prefab, content);
                if (row == null)
                {
                    Debug.LogError($"Could not create collection row for '{def.id}'. Missing {RowPrefabPath}.", content);
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
        if (prefab == null)
            return null;

        var instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, content) as GameObject;
        if (instance == null)
            return null;

        Undo.RegisterCreatedObjectUndo(instance, "Add Collection Row");
        return instance.GetComponent<CollectionRow>();
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

    static void EnsureJobCards(Transform jobsRoot)
    {
        if (jobsRoot == null)
            return;

        var prefab = LoadJobPrefab();
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
                if (option == null)
                {
                    Debug.LogError($"Could not create job card for '{def.id}'. Missing {JobPrefabPath}.", jobsRoot);
                    continue;
                }

                byId[def.id] = option;
            }

            ApplyJob(option, def);
            option.transform.SetSiblingIndex(i);
        }
    }

    static WorkJobOption InstantiateJob(WorkJobOption prefab, Transform parent)
    {
        if (prefab == null)
            return null;

        var instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, parent) as GameObject;
        if (instance == null)
            return null;

        Undo.RegisterCreatedObjectUndo(instance, "Add Work Job");
        return instance.GetComponent<WorkJobOption>();
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

    static void BindJobOptions(WorkScreen work)
    {
        var so = new SerializedObject(work);
        var jobProp = so.FindProperty("jobs");
        if (jobProp == null)
            return;

        var options = work.GetComponentsInChildren<WorkJobOption>(true);
        jobProp.arraySize = options.Length;
        for (var i = 0; i < options.Length; i++)
            jobProp.GetArrayElementAtIndex(i).objectReferenceValue = options[i];
        so.ApplyModifiedPropertiesWithoutUndo();
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

    static CollectionRow LoadRowPrefab()
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
        return go != null ? go.GetComponent<CollectionRow>() : null;
    }

    static WorkJobOption LoadJobPrefab()
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(JobPrefabPath);
        return go != null ? go.GetComponent<WorkJobOption>() : null;
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

    static T FindNamed<T>(Transform root, string path) where T : Component
    {
        var found = root.Find(path);
        return found != null ? found.GetComponent<T>() : null;
    }

    static void WithActive(GameObject go, System.Action work)
    {
        var restored = go != null && !go.activeSelf;
        if (restored)
            go.SetActive(true);
        try
        {
            work();
        }
        finally
        {
            if (restored)
                go.SetActive(false);
        }
    }
}
