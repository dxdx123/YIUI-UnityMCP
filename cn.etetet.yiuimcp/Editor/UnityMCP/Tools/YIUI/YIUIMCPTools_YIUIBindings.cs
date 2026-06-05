#if UNITY_EDITOR



using System.Collections.Generic;

using System.Threading.Tasks;

using Sirenix.OdinInspector;

using UnityEditor;

using UnityEngine;

using YIUIFramework;



namespace YIUIFramework.Editor.MCP

{

    [HideLabel]

    [HideReferenceObjectPicker]

    public class YIUIAddDataBindingParams : YIUIMCPBaseParams

    {

        public string prefabPath;

        public string dataName;

        public string dataValueType = "String";

        public string defaultValue = "";

    }



    [YIUIMCPTools("YIUIAddDataBinding", "通过YIUI API给Prefab添加Data变量")]

    public class YIUIMCPTools_YIUIAddDataBinding : YIUIMCPBaseExecutor<YIUIAddDataBindingParams>

    {

        protected override async Task<YIUIMCPResult> Run(YIUIAddDataBindingParams data)

        {

            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");

            if (string.IsNullOrWhiteSpace(data.dataName)) return YIUIMCPResult.FailureLog("dataName不能为空");



            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (_, cdeTable) =>

            {

                if (cdeTable.DataTable == null) cdeTable.DataTable = cdeTable.gameObject.GetOrAddComponent<UIBindDataTable>();



                var dataDic = YIUIMCPYIUIHelper.GetPrivateDictionary<Dictionary<string, UIData>>(cdeTable.DataTable, "m_DataDic");

                if (dataDic.ContainsKey(data.dataName)) throw new System.InvalidOperationException($"Data已存在: {data.dataName}");



                dataDic[data.dataName] = new UIData(data.dataName, YIUIMCPYIUIHelper.CreateDataValue(data.dataValueType, data.defaultValue));

                YIUIMCPYIUIHelper.MarkDirty(cdeTable.DataTable);

                YIUIMCPYIUIHelper.MarkDirty(cdeTable);

                return $"添加Data成功: {data.dataName} -> {data.prefabPath}";

            });



            await Task.CompletedTask;

            return YIUIMCPResult.Success(message);

        }

    }



    [HideLabel]

    [HideReferenceObjectPicker]

    public class YIUIAddEventBindingParams : YIUIMCPBaseParams

    {

        public string prefabPath;

        public string eventName;

        public string eventType = "P0";

        public bool isTaskEvent = false;

        public bool addClickBinder = true;

        public string clickNodeName = "OnClick";

    }



    [YIUIMCPTools("YIUIAddEventBinding", "通过YIUI API给Prefab添加Event事件")]

    public class YIUIMCPTools_YIUIAddEventBinding : YIUIMCPBaseExecutor<YIUIAddEventBindingParams>

    {

        protected override async Task<YIUIMCPResult> Run(YIUIAddEventBindingParams data)

        {

            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");

            if (string.IsNullOrWhiteSpace(data.eventName)) return YIUIMCPResult.FailureLog("eventName不能为空");



            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (_, cdeTable) =>

            {

                if (cdeTable.EventTable == null) cdeTable.EventTable = cdeTable.gameObject.GetOrAddComponent<UIBindEventTable>();



                var eventDic = YIUIMCPYIUIHelper.GetPrivateDictionary<Dictionary<string, UIEventBase>>(cdeTable.EventTable, "m_EventDic");

                if (eventDic.ContainsKey(data.eventName)) throw new System.InvalidOperationException($"Event已存在: {data.eventName}");



                var uiEvent = YIUIMCPYIUIHelper.CreateEvent(data.eventType, data.isTaskEvent, data.eventName);

                eventDic[data.eventName] = uiEvent;



                if (data.addClickBinder) AddClickBinder(cdeTable, data, uiEvent);



                YIUIMCPYIUIHelper.MarkDirty(cdeTable.EventTable);

                YIUIMCPYIUIHelper.MarkDirty(cdeTable);

                return $"添加Event成功: {data.eventName} -> {data.prefabPath}";

            });



            await Task.CompletedTask;

            return YIUIMCPResult.Success(message);

        }



        private static void AddClickBinder(UIBindCDETable cdeTable, YIUIAddEventBindingParams data, UIEventBase uiEvent)

        {

            var root = cdeTable.gameObject;

            var clickTransform = root.transform.Find(data.clickNodeName);

            if (clickTransform == null)

            {

                var clickObject = new GameObject(data.clickNodeName, typeof(RectTransform));

                clickTransform = clickObject.transform;

                clickTransform.SetParent(root.transform, false);

                var rect = clickObject.GetComponent<RectTransform>();

                rect.anchorMin = Vector2.zero;

                rect.anchorMax = Vector2.one;

                rect.sizeDelta = Vector2.zero;

                var image = clickObject.AddComponent<UnityEngine.UI.Image>();

                image.color = new Color(1f, 1f, 1f, 0f);

            }



            var bind = clickTransform.gameObject.GetOrAddComponent<UIEventBindClick>();

            YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_EventTable", cdeTable.EventTable);

            YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_EventName", data.eventName);

            YIUIMCPYIUIHelper.SetPrivateOrSerializedField(bind, "m_UIEvent", uiEvent);

            EditorUtility.SetDirty(bind);

        }

    }

}



#endif

