#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using YIUIFramework;
using YIUIFramework.Editor;

namespace YIUIFramework.Editor.MCP
{
    /// <summary>
    /// 在 Panel 源数据的 AllViewParent / AllPopupViewParent 下创建 View 参数
    /// </summary>
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUICreateViewParams : YIUIMCPBaseParams
    {
        /// <summary>Panel 源数据 prefab 路径(Source 下的 *PanelSource.prefab)</summary>
        public string panelSourcePrefabPath;

        /// <summary>View 名(建议以 View 结尾, 如 TestListView)。ViewParent 取名 {viewName}Parent</summary>
        public string viewName;

        /// <summary>true=放到 AllPopupViewParent(弹窗), false=AllViewParent(普通)</summary>
        public bool popup = false;
    }

    /// <summary>
    /// 在 Panel 源数据下创建一个 View (含 ViewParent 占位, 注册到 PanelSplitData.AllCreateView/AllPopupView)。
    /// 无头复刻 MenuItemYIUIView.CreateYIUIViewByGameObject 的逻辑:
    ///   AllViewParent → {viewName}Parent(ViewParent) → {viewName}(View, codeType=View)
    /// 源数据拆分时会把该 View 拆成独立 prefab 并建立 ViewParent 关联(运行时 OpenViewAsync 克隆进来)。
    /// PanelSplitData 为 internal, 用反射获取; 其成员 AllViewParent/AllCreateView 为 public。
    /// </summary>
    [YIUIMCPTools("YIUICreateView", "在Panel源数据的AllViewParent下创建View(含ViewParent, 注册AllCreateView)")]
    public class YIUIMCPTools_YIUICreateView : YIUIMCPBaseExecutor<YIUICreateViewParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUICreateViewParams data)
        {
            if (string.IsNullOrWhiteSpace(data.panelSourcePrefabPath)) return YIUIMCPResult.FailureLog("panelSourcePrefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.viewName)) return YIUIMCPResult.FailureLog("viewName不能为空");

            var viewName = data.viewName.Trim();
            var viewParentName = $"{viewName}{YIUIConstHelper.Const.UIParentName}"; // TestListView + Parent

            var message = YIUIMCPYIUIHelper.EditPrefab(data.panelSourcePrefabPath, (root, cdeTable) =>
            {
                if (cdeTable.UICodeType != EUICodeType.Panel)
                    throw new System.InvalidOperationException("目标必须是 Panel 源数据");

                // 反射取 internal 的 PanelSplitData (类型 public)
                var psdField = typeof(UIBindCDETable).GetField("PanelSplitData",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var psd = psdField?.GetValue(cdeTable) as UIPanelSplitData;
                if (psd == null) throw new System.InvalidOperationException("无法获取 PanelSplitData");

                // 找父级(AllViewParent / AllPopupViewParent)
                var parentRect = data.popup ? psd.AllPopupViewParent : psd.AllViewParent;
                if (parentRect == null)
                {
                    var parentName = data.popup ? YIUIConstHelper.Const.UIAllPopupViewParentName : YIUIConstHelper.Const.UIAllViewParentName;
                    var found = root.transform.Find(parentName);
                    if (found == null) throw new System.InvalidOperationException($"源面板缺少 {parentName} 节点");
                    parentRect = (RectTransform)found;
                }

                // ViewParent
                var viewParentGo = new GameObject(viewParentName, typeof(RectTransform));
                var viewParentRect = viewParentGo.GetComponent<RectTransform>();
                viewParentRect.SetParent(parentRect, false);
                SetFullScreen(viewParentRect);

                // View (MenuItemYIUIView.CreateYIUIView 为 public, 创建带 UIBindCDETable+View类型 的 View)
                var viewGo = MenuItemYIUIView.CreateYIUIView(viewParentGo);
                viewGo.name = viewName;
                var viewCde = viewGo.GetComponent<UIBindCDETable>();
                viewCde.ViewWindowType = data.popup ? EViewWindowType.Popup : EViewWindowType.View;

                // 注册到 PanelSplitData (public 成员)
                if (data.popup) psd.AllPopupView.Add(viewParentRect);
                else psd.AllCreateView.Add(viewParentRect);

                YIUIMCPYIUIHelper.MarkDirty(cdeTable);
                return $"已在 {(data.popup ? "AllPopupViewParent" : "AllViewParent")} 下创建 View: {viewParentName}/{viewName}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }

        private static void SetFullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localPosition = Vector3.zero;
        }
    }
}
#endif
