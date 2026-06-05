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
    public class YIUIAddDataBindActiveParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath;
        public string dataName;
        public bool reverse = false;
    }

    [YIUIMCPTools("YIUIAddDataBindActive", "添加UIDataBindActive组件控制节点显隐")]
    public class YIUIMCPTools_YIUIAddDataBindActive : YIUIMCPBaseExecutor<YIUIAddDataBindActiveParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUIAddDataBindActiveParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.nodePath)) return YIUIMCPResult.FailureLog("nodePath不能为空");
            if (string.IsNullOrWhiteSpace(data.dataName)) return YIUIMCPResult.FailureLog("dataName不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, cdeTable) =>
            {
                if (cdeTable.DataTable == null) cdeTable.DataTable = cdeTable.gameObject.GetOrAddComponent<UIBindDataTable>();

                var dataDic = YIUIMCPYIUIHelper.GetPrivateDictionary<System.Collections.Generic.Dictionary<string, UIData>>(cdeTable.DataTable, "m_DataDic");
                if (!dataDic.TryGetValue(data.dataName, out var uiData))
                {
                    throw new InvalidOperationException($"找不到数据: {data.dataName}");
                }

                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var bind = target.gameObject.GetOrAddComponent<UIDataBindActive>();
                
                YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_DataTable", cdeTable.DataTable);
                YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_Reverse", data.reverse);

                var dict = YIUIMCPYIUIHelper.GetPrivateDictionary<System.Collections.Generic.Dictionary<string, UIDataSelect>>(bind, "m_DataSelectDic");
                dict[data.dataName] = new UIDataSelect(uiData);
                YIUIMCPYIUIHelper.MarkDirty(bind);

                EditorUtility.SetDirty(bind);
                return $"成功添加Active绑定: {data.nodePath} -> {data.dataName}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }

    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUIAddDataBindImageParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath;
        public string dataName;
    }

    [YIUIMCPTools("YIUIAddDataBindImage", "添加UIDataBindImage组件控制Image sprite")]
    public class YIUIMCPTools_YIUIAddDataBindImage : YIUIMCPBaseExecutor<YIUIAddDataBindImageParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUIAddDataBindImageParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.nodePath)) return YIUIMCPResult.FailureLog("nodePath不能为空");
            if (string.IsNullOrWhiteSpace(data.dataName)) return YIUIMCPResult.FailureLog("dataName不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, cdeTable) =>
            {
                if (cdeTable.DataTable == null) cdeTable.DataTable = cdeTable.gameObject.GetOrAddComponent<UIBindDataTable>();

                var dataDic = YIUIMCPYIUIHelper.GetPrivateDictionary<System.Collections.Generic.Dictionary<string, UIData>>(cdeTable.DataTable, "m_DataDic");
                if (!dataDic.TryGetValue(data.dataName, out var uiData))
                {
                    throw new InvalidOperationException($"找不到数据: {data.dataName}");
                }

                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var image = target.gameObject.GetOrAddComponent<Image>();
                var bind = target.gameObject.GetOrAddComponent<UIDataBindImage>();
                
                YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_DataTable", cdeTable.DataTable);
                YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_Image", image);

                var dict = YIUIMCPYIUIHelper.GetPrivateDictionary<System.Collections.Generic.Dictionary<string, UIDataSelect>>(bind, "m_DataSelectDic");
                dict[data.dataName] = new UIDataSelect(uiData);
                YIUIMCPYIUIHelper.MarkDirty(bind);

                EditorUtility.SetDirty(image);
                EditorUtility.SetDirty(bind);
                return $"成功添加Image绑定: {data.nodePath} -> {data.dataName}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }

    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUIAddDataBindColorParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath;
        public string dataName;
    }

    [YIUIMCPTools("YIUIAddDataBindColor", "添加UIDataBindColor组件控制Graphic颜色")]
    public class YIUIMCPTools_YIUIAddDataBindColor : YIUIMCPBaseExecutor<YIUIAddDataBindColorParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUIAddDataBindColorParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.nodePath)) return YIUIMCPResult.FailureLog("nodePath不能为空");
            if (string.IsNullOrWhiteSpace(data.dataName)) return YIUIMCPResult.FailureLog("dataName不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, cdeTable) =>
            {
                if (cdeTable.DataTable == null) cdeTable.DataTable = cdeTable.gameObject.GetOrAddComponent<UIBindDataTable>();

                var dataDic = YIUIMCPYIUIHelper.GetPrivateDictionary<System.Collections.Generic.Dictionary<string, UIData>>(cdeTable.DataTable, "m_DataDic");
                if (!dataDic.TryGetValue(data.dataName, out var uiData))
                {
                    throw new InvalidOperationException($"找不到数据: {data.dataName}");
                }

                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var graphic = target.gameObject.GetComponent<Graphic>();
                if (graphic == null)
                {
                    throw new InvalidOperationException($"节点上没有Graphic组件: {data.nodePath}");
                }

                var bind = target.gameObject.GetOrAddComponent<UIDataBindColor>();
                
                YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_DataTable", cdeTable.DataTable);
                YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_Graphic", graphic);

                var dict = YIUIMCPYIUIHelper.GetPrivateDictionary<System.Collections.Generic.Dictionary<string, UIDataSelect>>(bind, "m_DataSelectDic");
                dict[data.dataName] = new UIDataSelect(uiData);
                YIUIMCPYIUIHelper.MarkDirty(bind);

                EditorUtility.SetDirty(bind);
                return $"成功添加Color绑定: {data.nodePath} -> {data.dataName}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }
}

#endif
