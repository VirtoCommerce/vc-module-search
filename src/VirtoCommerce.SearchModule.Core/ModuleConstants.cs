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

                public static string[] AllPermissions { get; } = {
                    IndexAccess,
                    IndexRebuild,
                };
            }
        }

        public static class Settings
        {
            public const int DefaultIndexPartitionSize = 50;

            public static class General
            {
                public static SettingDescriptor IndexPartitionSize { get; } = new()
                {
                    Name = "VirtoCommerce.Search.IndexPartitionSize",
                    GroupName = "Search|General",
                    ValueType = SettingValueType.PositiveInteger,
                    DefaultValue = DefaultIndexPartitionSize,
                };

                public static IEnumerable<SettingDescriptor> AllGeneralSettings
                {
                    get
                    {
                        yield return IndexPartitionSize;
                    }
                }
            }

            public static class IndexingJobs
            {
                public static SettingDescriptor Enable { get; } = new()
                {
                    Name = "VirtoCommerce.Search.IndexingJobs.Enable",
                    GroupName = "Search|Job",
                    ValueType = SettingValueType.Boolean,
                    DefaultValue = true,
                };

                public static SettingDescriptor CronExpression { get; } = new()
                {
                    Name = "VirtoCommerce.Search.IndexingJobs.CronExpression",
                    GroupName = "Search|Job",
                    ValueType = SettingValueType.ShortText,
                    DefaultValue = "0/5 * * * *",
                };

                public static IEnumerable<SettingDescriptor> AllIndexingJobsSettings
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
                    return General.AllGeneralSettings.Concat(IndexingJobs.AllIndexingJobsSettings);
                }
            }
        }
    }
}
