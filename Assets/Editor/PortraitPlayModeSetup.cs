using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PortraitPlayModeSetup
{
    const int Width = 1080;
    const int Height = 1920;
    const string SizeName = "Phobia Portrait 1080x1920";

    static PortraitPlayModeSetup()
    {
        EditorApplication.delayCall += EnsurePortraitGameView;
    }

    static void EnsurePortraitGameView()
    {
        if (Application.isBatchMode)
            return;

        try
        {
            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singletonType.GetProperty("instance")?.GetValue(null, null);
            if (instance == null)
                return;

            var getGroup = sizesType.GetMethod("GetGroup");
            var group = getGroup?.Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });
            if (group == null)
                return;

            var groupType = group.GetType();
            var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
            var getCustomCount = groupType.GetMethod("GetCustomCount");
            var getGameViewSize = groupType.GetMethod("GetGameViewSize");
            var builtin = (int)getBuiltinCount.Invoke(group, null);
            var custom = (int)getCustomCount.Invoke(group, null);

            var foundIndex = -1;
            for (var i = 0; i < builtin + custom; i++)
            {
                var size = getGameViewSize.Invoke(group, new object[] { i });
                var width = (int)size.GetType().GetProperty("width").GetValue(size, null);
                var height = (int)size.GetType().GetProperty("height").GetValue(size, null);
                var display = (string)size.GetType().GetProperty("baseText").GetValue(size, null);
                if (width == Width && height == Height && display == SizeName)
                {
                    foundIndex = i;
                    break;
                }
            }

            if (foundIndex < 0)
            {
                var sizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
                var sizeTypeEnum = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
                var size = ctor.Invoke(new object[] { 1, Width, Height, SizeName });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                foundIndex = builtin + custom;
            }

            var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var gameView = EditorWindow.GetWindow(gameViewType);
            var select = gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            select?.Invoke(gameView, new object[] { foundIndex, null });
        }
        catch (Exception)
        {
            // Game view internals can change; play mode still letterboxes to 9:16.
        }
    }
}
