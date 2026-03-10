using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using YukariLauncher;

public partial class ResourceHelper : Node
{
    private static readonly Dictionary<Type, List<Resource>> Resources = new();

    public override void _Ready()
    {
        base._Ready();
        Resources.Clear();
        PreloadResource<GameEntryResource>();
    }

    private static void PreloadResource<T>() where T : Resource
    {
        var type = typeof(T);
        if (!Resources.ContainsKey(type))
        {
            Resources[type] = [];
        }

        LoadResourcesRecursive<T>("res://");
    }

    private static void LoadResourcesRecursive<T>(string folderPath) where T : Resource
    {
        if (folderPath.Contains(".import"))
        {
            return;
        }

        var dir = DirAccess.Open(folderPath);
        if (dir == null)
        {
            GD.Print($"Could not open directory: {folderPath}");
            return;
        }

        dir.ListDirBegin();
        while (true)
        {
            var fileName = dir.GetNext();
            if (string.IsNullOrEmpty(fileName))
            {
                break;
            }

            if (fileName == "." || fileName == "..")
            {
                continue;
            }

            if (fileName.EndsWith(".remap"))
            {
                fileName = fileName.Replace(".remap", "");
            }

            var fullPath = $"{folderPath}/{fileName}";

            var isDir = dir.CurrentIsDir();
            if (isDir)
            {
                LoadResourcesRecursive<T>(fullPath);
            }
            else
            {
                if (!fileName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Resource res;
                try
                {
                    res = GD.Load<Resource>(fullPath);
                }
                catch (Exception e)
                {
                    GD.Print($"Exception loading resource at {fullPath}: {e.Message}");
                    continue;
                }

                if (res == null)
                {
                    GD.Print($"Failed to load resource at {fullPath}");
                    continue;
                }

                if (res is not T typedRes)
                {
                    continue;
                }

                if (!Resources.ContainsKey(typeof(T)))
                {
                    Resources[typeof(T)] = [];
                }

                Resources[typeof(T)].Add(typedRes);
                // GD.Print( $"Loaded: {typedRes.ResourceName} ({fullPath})" );
            }
        }

        dir.ListDirEnd();
    }

    public static List<T> GetAll<T>() where T : Resource
    {
        return Resources.ContainsKey(typeof(T)) ? Resources[typeof(T)].Cast<T>().ToList() : [];
    }


    public static T GetByPath<T>(string path) where T : Resource
    {
        var res = GetAll<T>().FirstOrDefault(r => ResourceUid.PathToUid(r.ResourcePath) == path);
        return res ?? GetAll<T>().FirstOrDefault(r => r.ResourcePath == path);
    }
}