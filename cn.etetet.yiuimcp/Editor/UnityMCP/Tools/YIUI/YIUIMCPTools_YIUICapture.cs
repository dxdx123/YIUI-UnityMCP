#if UNITY_EDITOR

using System;
using System.IO;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YIUIFramework.Editor.MCP
{
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUICapturePrefabParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public int width = 1080;
        public int height = 1920;
        public Color background = new Color(0.22f, 0.22f, 0.22f, 1f);
        public string outputPath; // 留空则写到 <项目>/Temp/YIUICapture_<名>.png
    }

    /// <summary>
    /// 把 UI Prefab 渲染成 PNG, 用于 AI 自检布局是否合理。
    /// 做法: 预览场景 + WorldSpace Canvas + 正交相机 -> RenderTexture -> PNG。
    /// 注意: 编辑态不跑运行时逻辑, 所以看到的是静态设计内容(数据绑定的动态文本不会填充)。
    /// </summary>
    [YIUIMCPTools("YIUICapturePrefab", "渲染UI Prefab为PNG(布局自检)")]
    public class YIUIMCPTools_YIUICapturePrefab : YIUIMCPBaseExecutor<YIUICapturePrefabParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUICapturePrefabParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(data.prefabPath);
            if (prefabAsset == null) return YIUIMCPResult.FailureLog($"找不到Prefab: {data.prefabPath}");

            var w = data.width > 0 ? data.width : 1080;
            var h = data.height > 0 ? data.height : 1920;

            var scene = EditorSceneManager.NewPreviewScene();
            GameObject camGo = null, canvasGo = null, instance = null;
            Camera cam = null;
            RenderTexture rt = null;
            Texture2D tex = null;
            var prevActive = RenderTexture.active;

            try
            {
                // 相机: 只渲染该预览场景
                camGo = new GameObject("__YIUICaptureCam");
                SceneManager.MoveGameObjectToScene(camGo, scene);
                camGo.transform.position = new Vector3(0f, 0f, -10f);
                camGo.transform.rotation = Quaternion.identity;
                cam = camGo.AddComponent<Camera>();
                cam.scene = scene;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = data.background;
                cam.orthographic = true;
                cam.orthographicSize = h / 2f; // 垂直可视 = h 个世界单位
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 100f;
                cam.cullingMask = ~0;

                // WorldSpace Canvas, 尺寸正好 = w x h, 居中于原点
                canvasGo = new GameObject("__YIUICaptureCanvas", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(canvasGo, scene);
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = cam;
                var canvasRect = canvasGo.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(w, h);
                canvasRect.position = Vector3.zero;
                canvasRect.localScale = Vector3.one;

                // 实例化 prefab 到 Canvas 下
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);
                if (instance == null) return YIUIMCPResult.FailureLog($"实例化失败: {data.prefabPath}");
                instance.transform.SetParent(canvasGo.transform, false);
                instance.SetActive(true);

                // 非全屏拉伸的根(View/Common/Item等)默认锚点可能在角落 -> 预览时居中显示;
                // 全屏拉伸的根(Panel: anchorMin=0,anchorMax=1)保持原样填满画布。
                var instRect = instance.GetComponent<RectTransform>();
                if (instRect != null)
                {
                    var stretched = instRect.anchorMin == Vector2.zero && instRect.anchorMax == Vector2.one;
                    if (!stretched)
                    {
                        instRect.anchorMin = instRect.anchorMax = new Vector2(0.5f, 0.5f);
                        instRect.pivot = new Vector2(0.5f, 0.5f);
                        instRect.anchoredPosition = Vector2.zero;
                    }
                }

                // 构建 UGUI 网格后再渲染
                Canvas.ForceUpdateCanvases();

                rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
                cam.targetTexture = rt;
                cam.aspect = (float)w / h;
                cam.Render();

                RenderTexture.active = rt;
                tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();

                var png = tex.EncodeToPNG();

                var outPath = string.IsNullOrWhiteSpace(data.outputPath)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "Temp", $"YIUICapture_{prefabAsset.name}.png")
                    : data.outputPath;
                var dir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(outPath, png);

                await Task.CompletedTask;
                return YIUIMCPResult.Success($"已渲染Prefab为PNG: {Path.GetFullPath(outPath)} ({w}x{h})");
            }
            finally
            {
                RenderTexture.active = prevActive;
                if (cam != null) cam.targetTexture = null;
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);
                if (camGo != null) UnityEngine.Object.DestroyImmediate(camGo);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}

#endif
