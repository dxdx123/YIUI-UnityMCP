#if UNITY_EDITOR

using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using YIUIFramework;

namespace YIUIFramework.Editor.MCP
{
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUIBindClickEventToNodeParams : YIUIMCPBaseParams
    {
        public string prefabPath;
        public string nodePath;
        public string eventName;
        public bool createEventIfNotExists = true;
        public string eventType = "P0";
        public bool isTaskEvent = false;
        public bool addUIBlock = false;
        public bool addClickEffect = false;
        public float clickEffectScaleValue = 0.9f;
        public float clickEffectScaleTime = 0f;
        public float clickEffectPopTime = 0f;
    }

    [YIUIMCPTools("YIUIBindClickEventToNode", "在指定节点上添加UIEventBindClick并绑定事件")]
    public class YIUIMCPTools_YIUIBindClickEventToNode : YIUIMCPBaseExecutor<YIUIBindClickEventToNodeParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUIBindClickEventToNodeParams data)
        {
            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");
            if (string.IsNullOrWhiteSpace(data.nodePath)) return YIUIMCPResult.FailureLog("nodePath不能为空");
            if (string.IsNullOrWhiteSpace(data.eventName)) return YIUIMCPResult.FailureLog("eventName不能为空");

            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, cdeTable) =>
            {
                if (cdeTable.EventTable == null) cdeTable.EventTable = cdeTable.gameObject.GetOrAddComponent<UIBindEventTable>();

                var eventDic = YIUIMCPYIUIHelper.GetPrivateDictionary<Dictionary<string, UIEventBase>>(cdeTable.EventTable, "m_EventDic");
                if (!eventDic.TryGetValue(data.eventName, out var uiEvent))
                {
                    if (!data.createEventIfNotExists)
                    {
                        throw new System.InvalidOperationException($"Event不存在且未开启自动创建: {data.eventName}");
                    }

                    uiEvent = YIUIMCPYIUIHelper.CreateEvent(data.eventType, data.isTaskEvent, data.eventName);
                    eventDic[data.eventName] = uiEvent;
                    YIUIMCPYIUIHelper.MarkDirty(cdeTable.EventTable);
                }

                var targetNode = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.nodePath);
                var bind = targetNode.gameObject.GetOrAddComponent<UIEventBindClick>();
                YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_EventTable", cdeTable.EventTable);
                YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_EventName", data.eventName);
                YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_UIEvent", uiEvent);
                EditorUtility.SetDirty(bind);

                if (data.addUIBlock)
                {
                    var block = targetNode.gameObject.GetOrAddComponent<UIBlock>();
                    EditorUtility.SetDirty(block);
                }

            if (string.IsNullOrWhiteSpace(data.nodePath)) data.nodePath = ".";
                {
                    var effect = targetNode.gameObject.GetOrAddComponent<YIUIClickEffect>();
                    effect.scaleValue = data.clickEffectScaleValue;
                    effect.scaleTime = data.clickEffectScaleTime;
                    effect.popTime = data.clickEffectPopTime;
                    EditorUtility.SetDirty(effect);
                }

                YIUIMCPYIUIHelper.MarkDirty(cdeTable);
                return $"成功在节点 {data.nodePath} 上绑定点击事件 {data.eventName}";
            });

            await Task.CompletedTask;
            return YIUIMCPResult.Success(message);
        }
    }
}

#endif
