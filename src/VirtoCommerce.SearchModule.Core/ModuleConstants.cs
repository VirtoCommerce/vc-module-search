using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.SearchModule.Core
{
    public static class ModuleConstants
    {
        public static class Security
        {
            public static class Permissions
            {
                public const string IndexRebuild = "search:index:rebuild";

                public static string[] AllPermissions = new[] { IndexRebuild };
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
                    ValueType = SettingValueType.Integer,
                    DefaultValue = "50",
                };
                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return IndexPartitionSize;
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


#pragma warning disable S125 // Sections of code should not be commented out
                /* // VP-6502: Temporary disabled indexing setting "Enable scale-out" until further investigation
                public static SettingDescriptor ScaleOut = new SettingDescriptor
                {
                    Name = "VirtoCommerce.Search.IndexingJobs.ScaleOut",
                    GroupName = "Search|Job",
                    ValueType = SettingValueType.Boolean,
                    DefaultValue = false
                };
                */
#pragma warning restore S125 // Sections of code should not be commented out

                public static SettingDescriptor MaxQueueSize = new SettingDescriptor
                {
                    Name = "VirtoCommerce.Search.IndexingJobs.MaxQueueSize",
                    GroupName = "Search|Job",
                    ValueType = SettingValueType.Integer,
                    DefaultValue = 25
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return Enable;
                        yield return CronExpression;
                        // yield return ScaleOut; // VP-6502: Temporary disabled indexing setting "Enable scale-out" until further investigation
                        yield return MaxQueueSize;
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

