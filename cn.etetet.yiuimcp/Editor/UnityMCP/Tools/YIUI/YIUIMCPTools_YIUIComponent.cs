#if UNITY_EDITOR

using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace YIUIFramework.Editor.MCP
{
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUIAddComponentParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath = ".";
        public string componentType;
        public object properties;
    }

    [YIUIMCPTools("YIUIAddComponent", "添加Unity组件到Prefab节点")]
    public class YIUIMCPTools_YIUIAddComponent : YIUIMCPBaseExecutor<YIUIAddComponentParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUIAddComponentParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.componentType)) return YIUIMCPResult.FailureLog("componentType不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var type = Type.GetType(data.componentType);
                if (type == null)
                {
                    type = Type.GetType($"UnityEngine.UI.{data.componentType}, UnityEngine.UI");
                }
                if (type == null)
                {
                    type = Type.GetType($"UnityEngine.{data.componentType}, UnityEngine");
                }
                if (type == null)
                {
                    throw new InvalidOperationException($"找不到组件类型: {data.componentType}");
                }

                var component = target.gameObject.GetComponent(type);
                if (component == null)
                {
                    component = target.gameObject.AddComponent(type);
                }

                if (data.properties != null)
                {
                    var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(
                        Newtonsoft.Json.JsonConvert.SerializeObject(data.properties));
                    foreach (var kvp in dict)
                    {
                        YIUIMCPYIUIHelper.SetPrivateOrSerializedField(component, kvp.Key, kvp.Value);
                    }
                }

                EditorUtility.SetDirty(component);
                return $"成功添加组件: {data.nodePath} -> {type.Name}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }

    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUIRemoveComponentParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath = ".";
        public string componentType;
    }

    [YIUIMCPTools("YIUIRemoveComponent", "从Prefab节点移除Unity组件")]
    public class YIUIMCPTools_YIUIRemoveComponent : YIUIMCPBaseExecutor<YIUIRemoveComponentParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUIRemoveComponentParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.componentType)) return YIUIMCPResult.FailureLog("componentType不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var type = Type.GetType(data.componentType);
                if (type == null)
                {
                    type = Type.GetType($"UnityEngine.UI.{data.componentType}, UnityEngine.UI");
                }
                if (type == null)
                {
                    type = Type.GetType($"UnityEngine.{data.componentType}, UnityEngine");
                }
                if (type == null)
                {
                    throw new InvalidOperationException($"找不到组件类型: {data.componentType}");
                }

                var component = target.gameObject.GetComponent(type);
                if (component != null)
                {
                    UnityEngine.Object.DestroyImmediate(component, true);
                    return $"成功移除组件: {data.nodePath} -> {type.Name}";
                }
                return $"组件不存在: {data.nodePath} -> {type.Name}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }
}

#endif
