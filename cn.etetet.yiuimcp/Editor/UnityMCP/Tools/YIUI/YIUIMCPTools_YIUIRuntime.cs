#if UNITY_EDITOR

using System.IO;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace YIUIFramework.Editor.MCP
{
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUICaptureGameViewParams : YIUIMCPBaseParams
    {
        public string outputPath; // 留空则写到 <项目>/Temp/YIUICaptureGame.png
    }

    /// <summary>
    /// 截取运行时 Game 视图为 PNG(需 Play 模式)。用于查看真实运行界面(含运行时生成的列表等)。
    /// </summary>
    [YIUIMCPTools("YIUICaptureGameView", "截取运行时Game视图为PNG(需Play)")]
    public class YIUIMCPTools_YIUICaptureGameView : YIUIMCPBaseExecutor<YIUICaptureGameViewParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUICaptureGameViewParams data)
        {
            if (!Application.isPlaying) return YIUIMCPResult.FailureLog("YIUICaptureGameView 需要在 Play 模式下使用");

            var outPath = string.IsNullOrWhiteSpace(data.outputPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "Temp", "YIUICaptureGame.png")
                : data.outputPath;
            var dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // 文件版 ScreenCapture 会挂到渲染管线, 在下一帧渲染后写入 PNG, 自动处理时机;
            // CaptureScreenshotAsTexture 从编辑器 update 调用会因不在渲染帧而返回 null。
            // 文件是下一帧才落盘 -> 调用方需稍等(~1s)再读取。
            if (File.Exists(outPath)) File.Delete(outPath);
            ScreenCapture.CaptureScreenshot(outPath);

            await Task.CompletedTask;
            return YIUIMCPResult.Success($"已请求截取Game视图(下一帧落盘): {Path.GetFullPath(outPath)} —— 请稍等~1秒再读取");
        }
    }

    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUISimulateClickParams : YIUIMCPBaseParams
    {
        public string name; // 运行时场景中目标节点名(如 u_OpenTestBtn)
    }

    /// <summary>
    /// 在 Play 模式下模拟点击一个 UI 节点(按名字找到活动的 GameObject, 触发 IPointerClickHandler)。
    /// YIUI 的 UIEventBindClick 实现了 IPointerClickHandler, 所以这会走真实点击事件流。
    /// </summary>
    [YIUIMCPTools("YIUISimulateClick", "Play模式下按名字模拟点击UI节点")]
    public class YIUIMCPTools_YIUISimulateClick : YIUIMCPBaseExecutor<YIUISimulateClickParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUISimulateClickParams data)
        {
            if (!Application.isPlaying) return YIUIMCPResult.FailureLog("YIUISimulateClick 需要在 Play 模式下使用");
            if (string.IsNullOrWhiteSpace(data.name)) return YIUIMCPResult.FailureLog("name不能为空");

            var go = FindActiveByName(data.name);
            if (go == null) return YIUIMCPResult.FailureLog($"未找到活动的 UI 节点: {data.name}");

            var es = EventSystem.current;
            var rect = go.GetComponent<RectTransform>();
            var screenPos = rect != null ? (Vector2)rect.position : (Vector2)go.transform.position;

            var ped = new PointerEventData(es)
            {
                button = PointerEventData.InputButton.Left,
                position = screenPos,
            };

            var handled = ExecuteEvents.Execute(go, ped, ExecuteEvents.pointerClickHandler);

            await Task.CompletedTask;
            return handled
                ? YIUIMCPResult.Success($"已点击: {data.name}")
                : YIUIMCPResult.FailureLog($"节点 {data.name} 上没有可响应点击的组件(IPointerClickHandler)");
        }

        private static GameObject FindActiveByName(string name)
        {
            // 优先用 GameObject.Find(活动对象)
            var found = GameObject.Find(name);
            if (found != null) return found;

            // 兜底: 遍历所有已加载对象(含层级中按名匹配), 只取在已加载场景中且激活的
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name != name) continue;
                var g = t.gameObject;
                if (!g.scene.IsValid() || !g.scene.isLoaded) continue; // 跳过 prefab 资源
                if (!g.activeInHierarchy) continue;
                return g;
            }
            return null;
        }
    }
}

#endif
