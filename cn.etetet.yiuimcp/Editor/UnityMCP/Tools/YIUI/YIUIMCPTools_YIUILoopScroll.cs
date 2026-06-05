#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using YIUIFramework;
using Object = UnityEngine.Object;

namespace YIUIFramework.Editor.MCP
{
    /// <summary>
    /// 创建 LoopScroll 滚动列表参数
    /// </summary>
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUICreateLoopScrollParams : YIUIMCPBaseParams
    {
        /// <summary>目标 Prefab 路径</summary>
        public string prefabPath;

        /// <summary>父节点路径（相对 Prefab 根），默认根</summary>
        public string parentPath = ".";

        /// <summary>创建后的节点名（为空则用模板名）</summary>
        public string nodeName;

        /// <summary>
        /// 滚动类型：Vertical / Horizontal / VerticalGroup / HorizontalGroup / VerticalReverse / HorizontalReverse
        /// </summary>
        public string scrollType = "Vertical";
    }

    /// <summary>
    /// 在 Prefab 节点下创建 YIUI LoopScroll 滚动列表。
    /// 等价于编辑器右键 GameObject/YIUI/LoopScroll/* —— 即克隆官方模板预制
    /// (Packages/cn.etetet.yiuiloopscrollrectasync/Editor/TemplatePrefabs/LoopScroll{Type}.prefab)
    /// 到目标节点下（与 UIMenuItemHelper.CloneGameObjectByPath 同行为）。
    /// 通过 PrefabUtility.LoadPrefabContents 无头编辑，全程 MCP 可驱动。
    /// </summary>
    [YIUIMCPTools("YIUICreateLoopScroll", "在Prefab节点下创建YIUI LoopScroll滚动列表(克隆官方模板, 支持水平/垂直/分组等)")]
    public class YIUIMCPTools_YIUICreateLoopScroll : YIUIMCPBaseExecutor<YIUICreateLoopScrollParams>
    {
        private const string TemplateDir = "Packages/cn.etetet.yiuiloopscrollrectasync/Editor/TemplatePrefabs";

        private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Vertical", "Horizontal", "VerticalGroup", "HorizontalGroup", "VerticalReverse", "HorizontalReverse"
        };

        protected override async Task<YIUIMCPResult> Run(YIUICreateLoopScrollParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");

            var scrollType = string.IsNullOrWhiteSpace(data.scrollType) ? "Vertical" : data.scrollType.Trim();
            if (!ValidTypes.Contains(scrollType))
            {
                return YIUIMCPResult.FailureLog(
                    $"未知 scrollType: {scrollType}（可选: Vertical/Horizontal/VerticalGroup/HorizontalGroup/VerticalReverse/HorizontalReverse）");
            }

            // 规范化首字母大写，匹配模板命名
            scrollType = NameUtility.ToFirstUpper(scrollType);
            var templatePath = $"{TemplateDir}/LoopScroll{scrollType}.prefab";
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
            if (template == null) return YIUIMCPResult.FailureLog($"找不到 LoopScroll 模板: {templatePath}");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, cdeTable) =>
            {
                var parent = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.parentPath);

                // 与 UIMenuItemHelper.CloneGameObjectByPath 一致：实例化模板到父节点下
                var go = Object.Instantiate(template, parent, false);
                if (go.name.EndsWith("(Clone)")) go.name = go.name.Replace("(Clone)", string.Empty);
                if (!string.IsNullOrWhiteSpace(data.nodeName)) go.name = data.nodeName.Trim();

                YIUIMCPYIUIHelper.MarkDirty(parent.gameObject);
                YIUIMCPYIUIHelper.MarkDirty(cdeTable);
                return $"已创建 LoopScroll({scrollType}) 节点: {data.parentPath}/{go.name}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }
}
#endif
