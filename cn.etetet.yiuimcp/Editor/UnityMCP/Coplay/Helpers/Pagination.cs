// ----------------------------------------------------------------------------
// Ported from MCP for Unity — https://github.com/CoplayDev/unity-mcp
// Copyright (c) 2025 CoplayDev. Licensed under the MIT License.
// Full license text: see THIRD-PARTY-NOTICES.md at the repository root.
// Vendored into YIUI-UnityMCP (cn.etetet.yiuimcp) with minimal changes.
// ----------------------------------------------------------------------------
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Helpers
{
    /// <summary>
    /// Standard pagination request for all paginated tool operations.
    /// Provides consistent handling of page_size/pageSize and cursor/page_number parameters.
    /// </summary>
    public class PaginationRequest
    {
        /// <summary>
        /// Number of items per page. Default is 50.
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// 0-based cursor position for the current page.
        /// </summary>
        public int Cursor { get; set; } = 0;

        /// <summary>
        /// Creates a PaginationRequest from JObject parameters.
        /// Accepts both snake_case and camelCase parameter names for flexibility.
        /// Converts 1-based page_number to 0-based cursor if needed.
        /// </summary>
        public static PaginationRequest FromParams(JObject @params, int defaultPageSize = 50)
        {
            if (@params == null)
                return new PaginationRequest { PageSize = defaultPageSize };

            // Accept both page_size and pageSize
            int pageSize = ParamCoercion.CoerceInt(
                @params["page_size"] ?? @params["pageSize"],
                defaultPageSize
            );

            // Accept both cursor (0-based) and page_number (convert 1-based to 0-based)
            var cursorToken = @params["cursor"];
            var pageNumberToken = @params["page_number"] ?? @params["pageNumber"];

            int cursor;
            if (cursorToken != null)
            {
                cursor = ParamCoercion.CoerceInt(cursorToken, 0);
            }
            else if (pageNumberToken != null)
            {
                int pageNumber = ParamCoercion.CoerceInt(pageNumberToken, 1);
                cursor = (pageNumber - 1) * pageSize;
                if (cursor < 0) cursor = 0;
            }
            else
            {
                cursor = 0;
            }

            return new PaginationRequest
            {
                PageSize = pageSize > 0 ? pageSize : defaultPageSize,
                Cursor = cursor
            };
        }
    }

    /// <summary>
    /// Standard pagination response for all paginated tool operations.
    /// </summary>
    /// <typeparam name="T">The type of items in the paginated list</typeparam>
    public class PaginationResponse<T>
    {
        [JsonProperty("items")]
        public List<T> Items { get; set; } = new List<T>();

        [JsonProperty("cursor")]
        public int Cursor { get; set; }

        [JsonProperty("nextCursor")]
        public int? NextCursor { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("hasMore")]
        public bool HasMore => NextCursor.HasValue;

        /// <summary>
        /// Creates a PaginationResponse from a full list of items and pagination parameters.
        /// </summary>
        public static PaginationResponse<T> Create(IList<T> allItems, PaginationRequest request)
        {
            int totalCount = allItems.Count;
            int cursor = request.Cursor;
            int pageSize = request.PageSize;

            // Clamp cursor to valid range
            if (cursor < 0) cursor = 0;
            if (cursor > totalCount) cursor = totalCount;

            // Get the page of items
            var items = new List<T>();
            int endIndex = System.Math.Min(cursor + pageSize, totalCount);
            for (int i = cursor; i < endIndex; i++)
            {
                items.Add(allItems[i]);
            }

            int? nextCursor = endIndex < totalCount ? endIndex : (int?)null;

            return new PaginationResponse<T>
            {
                Items = items,
                Cursor = cursor,
                NextCursor = nextCursor,
                TotalCount = totalCount,
                PageSize = pageSize
            };
        }
    }
}
