#if UNITY_EDITOR

using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace YIUIFramework.Editor.MCP
{
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUISetLayerRecursiveParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath = ".";
        public string layerName = "UI";
        public bool recursive = true;
    }

    [YIUIMCPTools("YIUISetLayerRecursive", "设置Prefab节点Layer，可递归设置子节点")]
    public class YIUIMCPTools_YIUISetLayerRecursive : YIUIMCPBaseExecutor<YIUISetLayerRecursiveParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUISetLayerRecursiveParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.layerName)) return YIUIMCPResult.FailureLog("layerName不能为空");

            var layer = LayerMask.NameToLayer(data.layerName);
            if (layer < 0) return YIUIMCPResult.FailureLog($"找不到Layer: {data.layerName}");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var count = 0;
                SetLayer(target, layer, data.recursive, ref count);
                return $"成功设置Layer: {data.nodePath} -> {data.layerName}, count={count}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }

        private static void SetLayer(Transform transform, int layer, bool recursive, ref int count)
        {
            transform.gameObject.layer = layer;
            EditorUtility.SetDirty(transform.gameObject);
            count++;

            if (!recursive) return;

            for (var i = 0; i < transform.childCount; i++)
            {
                SetLayer(transform.GetChild(i), layer, true, ref count);
            }
        }
    }
}

#endif
