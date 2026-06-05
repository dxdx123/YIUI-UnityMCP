#if UNITY_EDITOR

using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace YIUIFramework.Editor.MCP
{
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUISetContentSizeFitterParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath = ".";
        public string horizontalFit = "Unconstrained";
        public string verticalFit = "Unconstrained";
    }

    [YIUIMCPTools("YIUISetContentSizeFitter", "设置Prefab节点的ContentSizeFitter")]
    public class YIUIMCPTools_YIUISetContentSizeFitter : YIUIMCPBaseExecutor<YIUISetContentSizeFitterParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUISetContentSizeFitterParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var fitter = target.gameObject.GetOrAddComponent<ContentSizeFitter>();

                if (Enum.TryParse<ContentSizeFitter.FitMode>(data.horizontalFit, true, out var hFit))
                {
                    fitter.horizontalFit = hFit;
                }

                if (Enum.TryParse<ContentSizeFitter.FitMode>(data.verticalFit, true, out var vFit))
                {
                    fitter.verticalFit = vFit;
                }

                EditorUtility.SetDirty(fitter);
                return $"成功设置ContentSizeFitter: {data.nodePath}, H={fitter.horizontalFit}, V={fitter.verticalFit}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }

    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUISetLayoutGroupParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath = ".";
        public string groupType = "Vertical";
        public float spacing = 0;
        public bool childForceExpandWidth = true;
        public bool childForceExpandHeight = true;
        public bool childControlWidth = true;
        public bool childControlHeight = true;
        public int paddingLeft = 0;
        public int paddingRight = 0;
        public int paddingTop = 0;
        public int paddingBottom = 0;
        public string childAlignment = "UpperLeft";
    }

    [YIUIMCPTools("YIUISetLayoutGroup", "设置Prefab节点的LayoutGroup")]
    public class YIUIMCPTools_YIUISetLayoutGroup : YIUIMCPBaseExecutor<YIUISetLayoutGroupParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUISetLayoutGroupParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                HorizontalOrVerticalLayoutGroup group = null;

                var normalized = data.groupType.ToLowerInvariant();
                if (normalized == "horizontal")
                {
                    group = target.gameObject.GetOrAddComponent<HorizontalLayoutGroup>();
                }
                else if (normalized == "vertical")
                {
                    group = target.gameObject.GetOrAddComponent<VerticalLayoutGroup>();
                }
                else if (normalized == "grid")
                {
                    var gridGroup = target.gameObject.GetOrAddComponent<GridLayoutGroup>();
                    gridGroup.padding = new RectOffset(data.paddingLeft, data.paddingRight, data.paddingTop, data.paddingBottom);
                    if (Enum.TryParse<TextAnchor>(data.childAlignment, true, out var alignment))
                    {
                        gridGroup.childAlignment = alignment;
                    }
                    EditorUtility.SetDirty(gridGroup);
                    return $"成功设置GridLayoutGroup: {data.nodePath}";
                }
                else
                {
                    throw new InvalidOperationException($"不支持的LayoutGroup类型: {data.groupType}");
                }

                if (group != null)
                {
                    group.spacing = data.spacing;
                    group.childForceExpandWidth = data.childForceExpandWidth;
                    group.childForceExpandHeight = data.childForceExpandHeight;
                    group.childControlWidth = data.childControlWidth;
                    group.childControlHeight = data.childControlHeight;
                    group.padding = new RectOffset(data.paddingLeft, data.paddingRight, data.paddingTop, data.paddingBottom);
                    
                    if (Enum.TryParse<TextAnchor>(data.childAlignment, true, out var alignment))
                    {
                        group.childAlignment = alignment;
                    }

                    EditorUtility.SetDirty(group);
                    return $"成功设置{data.groupType}LayoutGroup: {data.nodePath}";
                }

                return $"设置LayoutGroup失败: {data.nodePath}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }
}

#endif
