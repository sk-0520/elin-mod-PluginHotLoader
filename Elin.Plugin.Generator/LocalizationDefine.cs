using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Elin.Plugin.Generator
{
    internal record class LocalizationType
    {
        #region property

        public string Type { get; set; } = string.Empty;
        public string? Format { get; set; }

        #endregion
    }
    internal record class LocalizationItem
    {
        #region property

        public string JP { get; set; } = string.Empty;
        public string EN { get; set; } = string.Empty;
        public string? CN { get; set; }
        public string? ZHTW { get; set; }
        public string? KR { get; set; }

        public Dictionary<string, LocalizationType>? Parameters { get; set; }

        public IEnumerable<KeyValuePair<string, string>> RequiredLanguages => field ??=
        [
            new ("JP", JP),
            new ("EN", EN)
        ];

        public IEnumerable<KeyValuePair<string, string?>> OptionalLanguages => field ??=
        [
            new ("CN", CN),
            new ("ZHTW", ZHTW),
            new ("KR", KR)
        ];

        public IEnumerable<KeyValuePair<string, string?>> Languages => field ??= RequiredLanguages
            .Select(a => new KeyValuePair<string, string?>(a.Key, a.Value))
            .Concat(OptionalLanguages)
            .ToArray()
        ;

        #endregion

        #region index

        public string? this[string key] => key switch
        {
            "JP" => JP,
            "EN" => EN,
            "CN" => CN,
            "ZHTW" => ZHTW,
            "KR" => KR,
            _ => throw new KeyNotFoundException($"Invalid key: {key}")
        };

        #endregion
    }

    internal record class LocalizationDefine
    {
        #region property

        public string BaseLang { get; set; } = string.Empty;

        [JsonPropertyName("general")]
        public Dictionary<string, LocalizationItem> General { get; set; } = new Dictionary<string, LocalizationItem>();
        [JsonPropertyName("format")]
        public Dictionary<string, LocalizationItem> Format { get; set; } = new Dictionary<string, LocalizationItem>();

        #endregion
    }
}
