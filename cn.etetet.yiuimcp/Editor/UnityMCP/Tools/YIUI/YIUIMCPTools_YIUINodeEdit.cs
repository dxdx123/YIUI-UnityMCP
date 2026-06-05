#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace YIUIFramework.Editor.MCP
{
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUICreateChildNodeParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string parentPath = ".";
        public string nodeName;
        public bool addRectTransform = true;
        public bool addImage = false;
        public bool overwriteIfExists = false;
    }

    [YIUIMCPTools("YIUICreateChildNode", "在Prefab中创建子节点")]
    public class YIUIMCPTools_YIUICreateChildNode : YIUIMCPBaseExecutor<YIUICreateChildNodeParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUICreateChildNodeParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.nodeName)) return YIUIMCPResult.FailureLog("nodeName不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var parent = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.parentPath);
                var existed = parent.Find(data.nodeName);
                if (existed != null)
                {
                    if (!data.overwriteIfExists) return $"节点已存在，跳过创建: {data.nodeName}";
                    UnityEngine.Object.DestroyImmediate(existed.gameObject);
                }

                var node = new GameObject(data.nodeName);
                node.transform.SetParent(parent, false);

                if (data.addRectTransform)
                {
                    node.GetOrAddComponent<RectTransform>();
                }

                if (data.addImage)
                {
                    var image = node.GetOrAddComponent<Image>();
                    image.color = new Color(1f, 1f, 1f, 1f);
                }

                YIUIMCPYIUIHelper.MarkDirty(node);
                return $"成功创建节点: {data.parentPath}/{data.nodeName}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }

    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUISetRectTransformParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath = ".";
        public Vector2 anchorMin = Vector2.zero;
        public Vector2 anchorMax = Vector2.one;
        public Vector2 offsetMin = Vector2.zero;
        public Vector2 offsetMax = Vector2.zero;
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        public Vector2 anchoredPosition = Vector2.zero;
        public Vector2 sizeDelta = new Vector2(300, 150);
        public Vector3 localScale = Vector3.one;
    }

    [YIUIMCPTools("YIUISetRectTransform", "设置Prefab节点的RectTransform")]
    public class YIUIMCPTools_YIUISetRectTransform : YIUIMCPBaseExecutor<YIUISetRectTransformParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUISetRectTransformParams data)
        {
            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var rect = target.GetOrAddComponent<RectTransform>();
                rect.anchorMin = data.anchorMin;
                rect.anchorMax = data.anchorMax;
                rect.offsetMin = data.offsetMin;
                rect.offsetMax = data.offsetMax;
                rect.pivot = data.pivot;
                rect.anchoredPosition = data.anchoredPosition;
                rect.sizeDelta = data.sizeDelta;
                rect.localScale = data.localScale;
                EditorUtility.SetDirty(rect);
                return $"成功设置RectTransform: {data.nodePath}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }

    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUISetGraphicParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath = ".";
        public string graphicType = "Image";
        public Color color = new Color(1f, 1f, 1f, 1f);
        public bool raycastTarget = true;
        public string spritePath;
        public bool setNativeSize = false;
        public string materialPath;
    }

    [YIUIMCPTools("YIUISetGraphic", "设置节点的图形组件")]
    public class YIUIMCPTools_YIUISetGraphic : YIUIMCPBaseExecutor<YIUISetGraphicParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUISetGraphicParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, _) =>
            {
                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var normalized = (data.graphicType ?? "Image").Trim().ToLowerInvariant();
                Graphic graphic;

                switch (normalized)
                {
                    case "image":
                        graphic = target.gameObject.GetOrAddComponent<Image>();
                        break;
                    case "uiblock":
                        graphic = target.gameObject.GetOrAddComponent<UIBlock>();
                        break;
                    case "rawimage":
                        graphic = target.gameObject.GetOrAddComponent<RawImage>();
                        break;
                    default:
                        throw new InvalidOperationException($"暂不支持的graphicType: {data.graphicType}");
                }

                graphic.color = data.color;
                graphic.raycastTarget = data.raycastTarget;

                if (!string.IsNullOrWhiteSpace(data.spritePath) && graphic is Image image)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(data.spritePath);
                    if (sprite == null) throw new InvalidOperationException($"找不到Sprite: {data.spritePath}");
                    image.sprite = sprite;
                    if (data.setNativeSize) image.SetNativeSize();
                }

                if (!string.IsNullOrWhiteSpace(data.materialPath))
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(data.materialPath);
                    if (material == null) throw new InvalidOperationException($"找不到Material: {data.materialPath}");
                    graphic.material = material;
                }

                EditorUtility.SetDirty(graphic);
                return $"成功设置Graphic: {data.nodePath} / {data.graphicType}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }

    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUIAddTextBindingToNodeParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath;
        public string dataName;
        public string textType = "Text";
        public bool createTextComponentIfMissing = true;
        public string format;
        public bool changeEnabled = true;
        public bool numberPrecision = false;
        public string numberPrecisionStr = "F1";
    }

    [YIUIMCPTools("YIUIAddTextBindingToNode", "在节点上添加文本绑定到YIUI Data")]
    public class YIUIMCPTools_YIUIAddTextBindingToNode : YIUIMCPBaseExecutor<YIUIAddTextBindingToNodeParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUIAddTextBindingToNodeParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.nodePath)) return YIUIMCPResult.FailureLog("nodePath不能为空");
            if (string.IsNullOrWhiteSpace(data.dataName)) return YIUIMCPResult.FailureLog("dataName不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, cdeTable) =>
            {
                if (cdeTable.DataTable == null) cdeTable.DataTable = cdeTable.gameObject.GetOrAddComponent<UIBindDataTable>();

                var dataDic = YIUIMCPYIUIHelper.GetPrivateDictionary<Dictionary<string, UIData>>(cdeTable.DataTable, "m_DataDic");
                if (!dataDic.TryGetValue(data.dataName, out var uiData))
                {
                    throw new InvalidOperationException($"找不到数据: {data.dataName}");
                }

                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var normalized = (data.textType ?? "Text").Trim().ToLowerInvariant();

                if (normalized == "text")
                {
                    var text = target.gameObject.GetOrAddComponent<Text>();
                    var bind = target.gameObject.GetOrAddComponent<UIDataBindText>();
                    YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_Format", data.format);
                    YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_ChangeEnabled", data.changeEnabled);
                    YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_NumberPrecision", data.numberPrecision);
                    YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_NumberPrecisionStr", data.numberPrecisionStr);
                    AddDataSelect(bind, data.dataName, uiData);
                    EditorUtility.SetDirty(text);
                    EditorUtility.SetDirty(bind);
                    return $"成功绑定Text到数据: {data.nodePath} -> {data.dataName}";
                }

                if (normalized == "tmp" || normalized == "textmeshpro" || normalized == "textmeshprougui")
                {
#if TextMeshPro
                    var text = target.gameObject.GetOrAddComponent<TextMeshProUGUI>();
                    var bind = target.gameObject.GetOrAddComponent<UIDataBindTextTMP>();
                    YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_Format", data.format);
                    YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_ChangeEnabled", data.changeEnabled);
                    YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_NumberPrecision", data.numberPrecision);
                    YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_NumberPrecisionStr", data.numberPrecisionStr);
                    AddDataSelect(bind, data.dataName, uiData);
                    EditorUtility.SetDirty(text);
                    EditorUtility.SetDirty(bind);
                    return $"成功绑定TMP到数据: {data.nodePath} -> {data.dataName}";
#else
                    throw new InvalidOperationException("当前项目未启用 TextMeshPro 宏(#if TextMeshPro)，无法绑定 TMP 文本，请改用 textType=\"text\" 或在 YIUI 宏设置中开启 TextMeshPro");
#endif
                }

                throw new InvalidOperationException($"暂不支持的textType: {data.textType}");
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }

        private static void AddDataSelect(Component bind, string dataName, UIData uiData)
        {
            var dict = YIUIMCPYIUIHelper.GetPrivateDictionary<Dictionary<string, UIDataSelect>>(bind, "m_DataSelectDic");
            dict[dataName] = new UIDataSelect(uiData);
            YIUIMCPYIUIHelper.MarkDirty(bind);
        }
    }
}

#endif
