using UnityEditor;
using UnityEngine;

public static class PhobiaSaveMenu
{
    [MenuItem("Phobia/Clear Save Data")]
    public static void ClearSaveData()
    {
        if (!EditorUtility.DisplayDialog(
                "Clear Save Data",
                "This wipes Courage, collection, auto-clickers, and missions.",
                "Clear",
                "Cancel"))
            return;

        SaveSystem.DeleteSavedData();

        if (Application.isPlaying)
        {
            var save = Object.FindAnyObjectByType<SaveSystem>();
            if (save != null)
                save.ResetProgress();
        }

        Debug.Log("Phobia save data cleared.");
    }
}
