// ----------------------------------------------------------------------------
// Ported from MCP for Unity — https://github.com/CoplayDev/unity-mcp
// Copyright (c) 2025 CoplayDev. Licensed under the MIT License.
// Full license text: see THIRD-PARTY-NOTICES.md at the repository root.
// Vendored into YIUI-UnityMCP (cn.etetet.yiuimcp) with minimal changes.
// ----------------------------------------------------------------------------
using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Helpers
{
    /// <summary>
    /// Utility class for coercing JSON parameter values to strongly-typed values.
    /// Handles various input formats (strings, numbers, booleans) gracefully.
    /// </summary>
    public static class ParamCoercion
    {
        /// <summary>
        /// Coerces a JToken to an integer value, handling strings and floats.
        /// </summary>
        public static int CoerceInt(JToken token, int defaultValue)
        {
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            try
            {
                if (token.Type == JTokenType.Integer)
                    return token.Value<int>();

                var s = token.ToString().Trim();
                if (s.Length == 0)
                    return defaultValue;

                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    return i;

                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    return (int)d;
            }
            catch
            {
                // Swallow and return default
            }

            return defaultValue;
        }

        /// <summary>
        /// Coerces a JToken to a long value, handling strings and floats.
        /// </summary>
        public static long CoerceLong(JToken token, long defaultValue)
        {
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            try
            {
                if (token.Type == JTokenType.Integer)
                    return token.Value<long>();

                var s = token.ToString().Trim();
                if (s.Length == 0)
                    return defaultValue;

                if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                    return l;

                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    return (long)d;
            }
            catch
            {
                // Swallow and return default
            }

            return defaultValue;
        }

        /// <summary>
        /// Coerces a JToken to a nullable integer value.
        /// Returns null if token is null, empty, or cannot be parsed.
        /// </summary>
        public static int? CoerceIntNullable(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            try
            {
                if (token.Type == JTokenType.Integer)
                    return token.Value<int>();

                var s = token.ToString().Trim();
                if (s.Length == 0)
                    return null;

                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    return i;

                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    return (int)d;
            }
            catch
            {
                // Swallow and return null
            }

            return null;
        }

        /// <summary>
        /// Coerces a JToken to a boolean value, handling strings like "true", "1", etc.
        /// </summary>
        public static bool CoerceBool(JToken token, bool defaultValue)
        {
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            try
            {
                if (token.Type == JTokenType.Boolean)
                    return token.Value<bool>();

                var s = token.ToString().Trim().ToLowerInvariant();
                if (s.Length == 0)
                    return defaultValue;

                if (bool.TryParse(s, out var b))
                    return b;

                if (s == "1" || s == "yes" || s == "on")
                    return true;

                if (s == "0" || s == "no" || s == "off")
                    return false;
            }
            catch
            {
                // Swallow and return default
            }

            return defaultValue;
        }

        /// <summary>
        /// Coerces a JToken to a nullable boolean value.
        /// Returns null if token is null, empty, or cannot be parsed.
        /// </summary>
        public static bool? CoerceBoolNullable(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            try
            {
                if (token.Type == JTokenType.Boolean)
                    return token.Value<bool>();

                var s = token.ToString().Trim().ToLowerInvariant();
                if (s.Length == 0)
                    return null;

                if (bool.TryParse(s, out var b))
                    return b;

                if (s == "1" || s == "yes" || s == "on")
                    return true;

                if (s == "0" || s == "no" || s == "off")
                    return false;
            }
            catch
            {
                // Swallow and return null
            }

            return null;
        }

        /// <summary>
        /// Coerces a JToken to a float value, handling strings and integers.
        /// </summary>
        public static float CoerceFloat(JToken token, float defaultValue)
        {
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            try
            {
                if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
                    return token.Value<float>();

                var s = token.ToString().Trim();
                if (s.Length == 0)
                    return defaultValue;

                if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    return f;
            }
            catch
            {
                // Swallow and return default
            }

            return defaultValue;
        }

        /// <summary>
        /// Coerces a JToken to a nullable float value.
        /// Returns null if token is null, empty, or cannot be parsed.
        /// </summary>
        public static float? CoerceFloatNullable(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            try
            {
                if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
                    return token.Value<float>();

                var s = token.ToString().Trim();
                if (s.Length == 0)
                    return null;

                if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    return f;
            }
            catch
            {
                // Swallow and return null
            }

            return null;
        }

        /// <summary>
        /// Coerces a JToken to a string value, with null handling.
        /// </summary>
        public static string CoerceString(JToken token, string defaultValue = null)
        {
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            var s = token.ToString();
            return string.IsNullOrEmpty(s) ? defaultValue : s;
        }

        /// <summary>
        /// Coerces a JToken to an enum value, handling strings.
        /// </summary>
        public static T CoerceEnum<T>(JToken token, T defaultValue) where T : struct, Enum
        {
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            try
            {
                var s = token.ToString().Trim();
                if (s.Length == 0)
                    return defaultValue;

                if (Enum.TryParse<T>(s, ignoreCase: true, out var result))
                    return result;
            }
            catch
            {
                // Swallow and return default
            }

            return defaultValue;
        }

        /// <summary>
        /// Checks if a JToken represents a numeric value (integer or float).
        /// </summary>
        public static bool IsNumericToken(JToken token)
        {
            return token != null && (token.Type == JTokenType.Integer || token.Type == JTokenType.Float);
        }

        /// <summary>
        /// Validates that an optional field in a JObject is numeric if present.
        /// </summary>
        public static bool ValidateNumericField(JObject obj, string fieldName, out string error)
        {
            error = null;
            var token = obj[fieldName];
            if (token == null || token.Type == JTokenType.Null)
            {
                return true; // Field not present, valid (will use default)
            }
            if (!IsNumericToken(token))
            {
                error = $"must be a number, got {token.Type}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that an optional field in a JObject is an integer if present.
        /// </summary>
        public static bool ValidateIntegerField(JObject obj, string fieldName, out string error)
        {
            error = null;
            var token = obj[fieldName];
            if (token == null || token.Type == JTokenType.Null)
            {
                return true; // Field not present, valid
            }
            if (token.Type != JTokenType.Integer)
            {
                error = $"must be an integer, got {token.Type}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Normalizes a property name by removing separators and converting to camelCase.
        /// Examples: "Use Gravity" → "useGravity", "is_kinematic" → "isKinematic".
        /// </summary>
        public static string NormalizePropertyName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Split on common separators: space, underscore, dash
            var parts = input.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return input;

            // First word is lowercase, subsequent words are Title case (camelCase)
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (i == 0)
                {
                    sb.Append(part.ToLowerInvariant());
                }
                else
                {
                    sb.Append(char.ToUpperInvariant(part[0]));
                    if (part.Length > 1)
                        sb.Append(part.Substring(1).ToLowerInvariant());
                }
            }
            return sb.ToString();
        }
    }
}
