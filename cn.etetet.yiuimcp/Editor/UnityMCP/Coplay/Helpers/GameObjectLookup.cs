// ----------------------------------------------------------------------------
// Ported from MCP for Unity — https://github.com/CoplayDev/unity-mcp
// Copyright (c) 2025 CoplayDev. Licensed under the MIT License.
// Full license text: see THIRD-PARTY-NOTICES.md at the repository root.
// Vendored into YIUI-UnityMCP (cn.etetet.yiuimcp) with minimal changes.
// ----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MCPForUnity.Runtime.Helpers;

namespace MCPForUnity.Editor.Helpers
{
    /// <summary>
    /// Utility class for finding and looking up GameObjects in the scene.
    /// Provides search functionality by name, tag, layer, component, path, and instance ID.
    /// </summary>
    public static class GameObjectLookup
    {
        /// <summary>
        /// Supported search methods for finding GameObjects.
        /// </summary>
        public enum SearchMethod
        {
            ByName,
            ByTag,
            ByLayer,
            ByComponent,
            ByPath,
            ById
        }

        /// <summary>
        /// Parses a search method string into the enum value.
        /// </summary>
        public static SearchMethod ParseSearchMethod(string method)
        {
            if (string.IsNullOrEmpty(method))
                return SearchMethod.ByName;

            return method.ToLowerInvariant() switch
            {
                "by_name" => SearchMethod.ByName,
                "by_tag" => SearchMethod.ByTag,
                "by_layer" => SearchMethod.ByLayer,
                "by_component" => SearchMethod.ByComponent,
                "by_path" => SearchMethod.ByPath,
                "by_id" => SearchMethod.ById,
                _ => SearchMethod.ByName
            };
        }

        /// <summary>
        /// Finds a single GameObject based on the target and search method.
        /// </summary>
        public static GameObject FindByTarget(JToken target, string searchMethod, bool includeInactive = false)
        {
            if (target == null)
                return null;

            var results = SearchGameObjects(searchMethod, target.ToString(), includeInactive, 1);
            return results.Count > 0 ? FindById(results[0]) : null;
        }

        /// <summary>
        /// Resolves an instance ID to a UnityEngine.Object.
        /// </summary>
        public static UnityEngine.Object ResolveInstanceID(int instanceId)
        {
            return UnityObjectIdCompat.InstanceIDToObjectCompat(instanceId);
        }

        /// <summary>
        /// Finds a GameObject by its instance ID.
        /// </summary>
        public static GameObject FindById(int instanceId)
        {
            return ResolveInstanceID(instanceId) as GameObject;
        }

        /// <summary>
        /// Searches for GameObjects and returns their instance IDs.
        /// </summary>
        public static List<int> SearchGameObjects(string searchMethod, string searchTerm, bool includeInactive = false, int maxResults = 0)
        {
            var method = ParseSearchMethod(searchMethod);
            return SearchGameObjects(method, searchTerm, includeInactive, maxResults);
        }

        /// <summary>
        /// Searches for GameObjects and returns their instance IDs.
        /// </summary>
        public static List<int> SearchGameObjects(SearchMethod method, string searchTerm, bool includeInactive = false, int maxResults = 0)
        {
            var results = new List<int>();

            switch (method)
            {
                case SearchMethod.ById:
                    if (int.TryParse(searchTerm, out int instanceId))
                    {
                        var obj = ResolveInstanceID(instanceId) as GameObject;
                        if (obj != null && (includeInactive || obj.activeInHierarchy))
                        {
                            results.Add(instanceId);
                        }
                    }
                    break;

                case SearchMethod.ByName:
                    results.AddRange(SearchByName(searchTerm, includeInactive, maxResults));
                    break;

                case SearchMethod.ByPath:
                    results.AddRange(SearchByPath(searchTerm, includeInactive));
                    break;

                case SearchMethod.ByTag:
                    results.AddRange(SearchByTag(searchTerm, includeInactive, maxResults));
                    break;

                case SearchMethod.ByLayer:
                    results.AddRange(SearchByLayer(searchTerm, includeInactive, maxResults));
                    break;

                case SearchMethod.ByComponent:
                    results.AddRange(SearchByComponent(searchTerm, includeInactive, maxResults));
                    break;
            }

            return results;
        }

        private static IEnumerable<int> SearchByName(string name, bool includeInactive, int maxResults)
        {
            var allObjects = GetAllSceneObjects(includeInactive);
            var matching = allObjects.Where(go => go.name == name);

            if (maxResults > 0)
                matching = matching.Take(maxResults);

            return matching.Select(go => go.GetInstanceIDCompat());
        }

        private static IEnumerable<int> SearchByPath(string path, bool includeInactive)
        {
            // Check Prefab Stage first - GameObject.Find() doesn't work in Prefab Stage
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                // Use GetAllSceneObjects which already handles Prefab Stage
                var allObjects = GetAllSceneObjects(includeInactive);
                foreach (var go in allObjects)
                {
                    if (MatchesPath(go, path))
                    {
                        yield return go.GetInstanceIDCompat();
                    }
                }
                yield break;
            }

            // Normal scene mode
            // NOTE: Unity's GameObject.Find(path) only finds ACTIVE GameObjects.
            // If includeInactive=true, we need to search manually to find inactive objects.
            if (includeInactive)
            {
                var allObjects = GetAllSceneObjects(true);
                foreach (var go in allObjects)
                {
                    if (MatchesPath(go, path))
                    {
                        yield return go.GetInstanceIDCompat();
                    }
                }
            }
            else
            {
                var found = GameObject.Find(path);
                if (found != null)
                {
                    yield return found.GetInstanceIDCompat();
                }
            }
        }

        private static IEnumerable<int> SearchByTag(string tag, bool includeInactive, int maxResults)
        {
            GameObject[] taggedObjects;
            try
            {
                if (includeInactive)
                {
                    var allObjects = GetAllSceneObjects(true);
                    taggedObjects = allObjects.Where(go => go.CompareTag(tag)).ToArray();
                }
                else
                {
                    taggedObjects = GameObject.FindGameObjectsWithTag(tag);
                }
            }
            catch (UnityException)
            {
                // Tag doesn't exist
                yield break;
            }

            var results = taggedObjects.AsEnumerable();
            if (maxResults > 0)
                results = results.Take(maxResults);

            foreach (var go in results)
            {
                yield return go.GetInstanceIDCompat();
            }
        }

        private static IEnumerable<int> SearchByLayer(string layerName, bool includeInactive, int maxResults)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer == -1)
            {
                if (!int.TryParse(layerName, out layer) || layer < 0 || layer > 31)
                {
                    yield break;
                }
            }

            var allObjects = GetAllSceneObjects(includeInactive);
            var matching = allObjects.Where(go => go.layer == layer);

            if (maxResults > 0)
                matching = matching.Take(maxResults);

            foreach (var go in matching)
            {
                yield return go.GetInstanceIDCompat();
            }
        }

        private static IEnumerable<int> SearchByComponent(string componentTypeName, bool includeInactive, int maxResults)
        {
            Type componentType = FindComponentType(componentTypeName);
            if (componentType == null)
            {
                McpLog.Warn($"[GameObjectLookup] Component type '{componentTypeName}' not found.");
                yield break;
            }

            var allObjects = GetAllSceneObjects(includeInactive);
            var count = 0;

            foreach (var go in allObjects)
            {
                if (go.GetComponent(componentType) != null)
                {
                    yield return go.GetInstanceIDCompat();
                    count++;

                    if (maxResults > 0 && count >= maxResults)
                        yield break;
                }
            }
        }

        /// <summary>
        /// Gets all GameObjects in the current scene.
        /// </summary>
        public static IEnumerable<GameObject> GetAllSceneObjects(bool includeInactive)
        {
            // Check Prefab Stage first
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.prefabContentsRoot != null)
            {
                foreach (var go in GetObjectAndDescendants(prefabStage.prefabContentsRoot, includeInactive))
                {
                    yield return go;
                }
                yield break;
            }

            // Normal scene mode
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                yield break;

            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                foreach (var go in GetObjectAndDescendants(root, includeInactive))
                {
                    yield return go;
                }
            }
        }

        private static IEnumerable<GameObject> GetObjectAndDescendants(GameObject obj, bool includeInactive)
        {
            if (!includeInactive && !obj.activeInHierarchy)
                yield break;

            yield return obj;

            foreach (Transform child in obj.transform)
            {
                foreach (var descendant in GetObjectAndDescendants(child.gameObject, includeInactive))
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Finds a component type by name, searching loaded assemblies.
        /// Delegates to UnityTypeResolver.ResolveComponent() for unified type resolution.
        /// </summary>
        public static Type FindComponentType(string typeName)
        {
            return UnityTypeResolver.ResolveComponent(typeName);
        }

        /// <summary>
        /// Checks whether a GameObject matches a path or trailing path segment.
        /// </summary>
        internal static bool MatchesPath(GameObject go, string path)
        {
            if (go == null || string.IsNullOrEmpty(path))
                return false;

            var goPath = GetGameObjectPath(go);
            return goPath == path || goPath.EndsWith("/" + path);
        }

        /// <summary>
        /// Gets the hierarchical path of a GameObject.
        /// </summary>
        public static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null)
                return string.Empty;

            var path = obj.name;
            var parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
