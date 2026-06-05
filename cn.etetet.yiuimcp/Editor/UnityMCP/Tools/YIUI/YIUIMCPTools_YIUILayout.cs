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
    public class YIUISetLayoutElementParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath = ".";
        public float minWidth = -1;
        public float minHeight = -1;
        public float preferredWidth = -1;
        public float preferredHeight = -1;
        public float flexibleWidth = -1;
        public float flexibleHeight = -1;
        public int layoutPriority = 1;
        public bool ignoreLayout = false;
    }

    [YIUIMCPTools("YIUISetLayoutElement", "设置Prefab节点的LayoutElement属性")]
    public class YIUIMCPTools_YIUISetLayoutElement : YIUIMCPBaseExecutor<YIUISetLayoutElementParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUISetLayoutElementParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var layoutElement = target.gameObject.GetOrAddComponent<LayoutElement>();

                layoutElement.minWidth = data.minWidth;
                layoutElement.minHeight = data.minHeight;
                layoutElement.preferredWidth = data.preferredWidth;
                layoutElement.preferredHeight = data.preferredHeight;
                layoutElement.flexibleWidth = data.flexibleWidth;
                layoutElement.flexibleHeight = data.flexibleHeight;
                layoutElement.layoutPriority = data.layoutPriority;
                layoutElement.ignoreLayout = data.ignoreLayout;

                EditorUtility.SetDirty(layoutElement);
                return $"成功设置LayoutElement: {data.nodePath}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }
}

#endif
