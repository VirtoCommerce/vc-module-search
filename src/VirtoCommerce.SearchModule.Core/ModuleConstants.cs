using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.SearchModule.Core
{
    [ExcludeFromCodeCoverage]
    public static class ModuleConstants
    {
        public static class Security
        {
            public static class Permissions
            {
                public const string IndexAccess = "search:index:access";
                public const string IndexRebuild = "search:index:rebuild";

                public static string[] AllPermissions { get; } = new[]
                {
                    IndexAccess,
                    IndexRebuild
                };
            }
        }

        public static class Settings
        {
            public static class General
            {
                public static SettingDescriptor IndexPartitionSize = new SettingDescriptor
                {
                    Name = "VirtoCommerce.Search.IndexPartitionSize",
                    GroupName = "Search|General",
                    ValueType = SettingValueType.PositiveInteger,
                    DefaultValue = "50",
                };
                public static SettingDescriptor EnablePartialDocumentUpdate { get; } = new()
                {
                    Name = "VirtoCommerce.Search.PartialDocumentUpdate.Enable",
                    GroupName = "Search|General",
                    ValueType = SettingValueType.Boolean,
                    DefaultValue = false,
                };
                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return IndexPartitionSize;
                        yield return EnablePartialDocumentUpdate;
                    }
                }
            }

            public static class IndexingJobs
            {
                public static SettingDescriptor Enable = new SettingDescriptor
                {
                    Name = "VirtoCommerce.Search.IndexingJobs.Enable",
                    GroupName = "Search|Job",
                    ValueType = SettingValueType.Boolean,
                    DefaultValue = true
                };
                public static SettingDescriptor CronExpression = new SettingDescriptor
                {
                    Name = "VirtoCommerce.Search.IndexingJobs.CronExpression",
                    GroupName = "Search|Job",
                    ValueType = SettingValueType.ShortText,
                    DefaultValue = "0/5 * * * *"
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return Enable;
                        yield return CronExpression;
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> AllSettings
            {
                get
                {
                    return General.AllSettings.Concat(IndexingJobs.AllSettings);
                }
            }

        }
    }
}

