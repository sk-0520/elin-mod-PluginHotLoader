using Elin.Plugin.Generator.Properties;
using Microsoft.CodeAnalysis;

namespace Elin.Plugin.Generator
{
    internal static class DiagnosticDescriptors
    {
        #region define

        public static readonly DiagnosticDescriptor EPG001 = new DiagnosticDescriptor(
            id: "EPG001",
            title: new LocalizableResourceString(nameof(Resources.EPG001_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG001_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EPG002 = new DiagnosticDescriptor(
            id: "EPG002",
            title: new LocalizableResourceString(nameof(Resources.EPG002_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG002_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EPG003 = new DiagnosticDescriptor(
            id: "EPG003",
            title: new LocalizableResourceString(nameof(Resources.EPG003_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG003_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EPG004 = new DiagnosticDescriptor(
            id: "EPG004",
            title: new LocalizableResourceString(nameof(Resources.EPG004_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG004_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EPG005 = new DiagnosticDescriptor(
            id: "EPG005",
            title: new LocalizableResourceString(nameof(Resources.EPG005_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG005_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EPG006 = new DiagnosticDescriptor(
            id: "EPG006",
            title: new LocalizableResourceString(nameof(Resources.EPG006_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG006_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EPG007 = new DiagnosticDescriptor(
            id: "EPG007",
            title: new LocalizableResourceString(nameof(Resources.EPG007_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG007_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EPG008 = new DiagnosticDescriptor(
            id: "EPG008",
            title: new LocalizableResourceString(nameof(Resources.EPG008_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG008_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EPG009 = new DiagnosticDescriptor(
            id: "EPG009",
            title: new LocalizableResourceString(nameof(Resources.EPG009_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG009_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EPG010 = new DiagnosticDescriptor(
            id: "EPG010",
            title: new LocalizableResourceString(nameof(Resources.EPG010_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG010_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EPG011 = new DiagnosticDescriptor(
            id: "EPG011",
            title: new LocalizableResourceString(nameof(Resources.EPG011_A_Title), Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(nameof(Resources.EPG011_B_Message), Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        #endregion
    }
}
