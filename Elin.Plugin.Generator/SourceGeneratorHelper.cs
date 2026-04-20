using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Text.Json;

namespace Elin.Plugin.Generator
{
    internal static class SourceGeneratorHelper
    {
        #region variable

        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        #endregion

        #region function

        private static bool TryParseDefine<T>(string rawJson, out T result)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<T>(rawJson, JsonSerializerOptions);
                if (obj is not null)
                {
                    result = obj;
                    return true;
                }
            }
            catch
            {
                // EPG012 を出力したい、わからん！
            }
            result = default!;
            return false;
        }

        private static T? SafeParseDefine<T>(string rawJson)
            where T : class
        {
            if (TryParseDefine<T>(rawJson, out var result))
            {
                return result;
            }

            return null;
        }

        public static IncrementalValueProvider<T?> CollectJsonClass<T>(IncrementalValuesProvider<AdditionalText> additionalTextsProvider, Func<AdditionalText, bool> predicate)
            where T : class
        {
            return additionalTextsProvider
                .Where(predicate)
                .Select((file, _) => (file: file, json: file.GetText()?.ToString()))
                .Where(a => a.json is not null)
                .Select((a, _) => SafeParseDefine<T>(a.json!))
                .Where(a => a is not null)
                .Collect()
                .Select((arr, _) => arr.FirstOrDefault())
            ;
        }

        #endregion
    }
}
