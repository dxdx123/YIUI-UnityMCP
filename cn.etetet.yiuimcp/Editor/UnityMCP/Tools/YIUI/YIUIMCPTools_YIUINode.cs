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
    public class YIUISetNodeActiveParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath;
        public bool active = true;
    }

    [YIUIMCPTools("YIUISetNodeActive", "设置Prefab节点的激活状态")]
    public class YIUIMCPTools_YIUISetNodeActive : YIUIMCPBaseExecutor<YIUISetNodeActiveParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUISetNodeActiveParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.nodePath)) return YIUIMCPResult.FailureLog("nodePath不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                target.gameObject.SetActive(data.active);
                EditorUtility.SetDirty(target.gameObject);
                return $"成功设置节点激活状态: {data.nodePath} -> {data.active}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }

    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUIDeleteChildNodeParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath;
    }

    [YIUIMCPTools("YIUIDeleteChildNode", "删除Prefab子节点")]
    public class YIUIMCPTools_YIUIDeleteChildNode : YIUIMCPBaseExecutor<YIUIDeleteChildNodeParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUIDeleteChildNodeParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.nodePath)) return YIUIMCPResult.FailureLog("nodePath不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var parent = target.parent;
                UnityEngine.Object.DestroyImmediate(target.gameObject, true);
                if (parent != null)
                {
                    EditorUtility.SetDirty(parent.gameObject);
                }
                return $"成功删除节点: {data.nodePath}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }
}

#endif
