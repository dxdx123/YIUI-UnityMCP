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
    public class YIUIMoveNodeParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath;
        public string targetParentPath;
        public int siblingIndex = -1;
    }

    [YIUIMCPTools("YIUIMoveNode", "移动Prefab节点到新父节点或调整同级顺序")]
    public class YIUIMCPTools_YIUIMoveNode : YIUIMCPBaseExecutor<YIUIMoveNodeParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUIMoveNodeParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.nodePath)) return YIUIMCPResult.FailureLog("nodePath不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var node = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                
                if (!string.IsNullOrWhiteSpace(data.targetParentPath))
                {
                    var targetParent = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.targetParentPath);
                    node.SetParent(targetParent, false);
                    EditorUtility.SetDirty(targetParent.gameObject);
                }

                if (data.siblingIndex >= 0)
                {
                    node.SetSiblingIndex(data.siblingIndex);
                }

                EditorUtility.SetDirty(node.gameObject);
                return $"成功移动节点: {data.nodePath} -> {data.targetParentPath ?? "same parent"}, index={data.siblingIndex}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }

    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUIDuplicateNodeParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath;
        public string newName;
    }

    [YIUIMCPTools("YIUIDuplicateNode", "复制Prefab节点")]
    public class YIUIMCPTools_YIUIDuplicateNode : YIUIMCPBaseExecutor<YIUIDuplicateNodeParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUIDuplicateNodeParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.nodePath)) return YIUIMCPResult.FailureLog("nodePath不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var node = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var duplicate = UnityEngine.Object.Instantiate(node.gameObject, node.parent);
                
                if (!string.IsNullOrWhiteSpace(data.newName))
                {
                    duplicate.name = data.newName;
                }
                else
                {
                    duplicate.name = node.name + "_Copy";
                }

                EditorUtility.SetDirty(duplicate);
                if (node.parent != null)
                {
                    EditorUtility.SetDirty(node.parent.gameObject);
                }
                return $"成功复制节点: {data.nodePath} -> {duplicate.name}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }
}

#endif
