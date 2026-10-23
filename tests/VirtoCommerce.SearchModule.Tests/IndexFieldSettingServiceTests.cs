using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Data.Services;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

[Trait("Category", "Unit")]
public class IndexFieldSettingServiceTests
{
    private const string DocumentType = "Product";

    [Fact]
    public async Task SearchAsync_NoClone_StoredValueUnchanged_ReturnsSameInstances()
    {
        var (service, _) = CreateService(CreateSetting("Manufacturer", "Acme", "ACME Inc"));

        var first = await service.SearchAsync(CreateCriteria(), clone: false);
        var second = await service.SearchAsync(CreateCriteria(), clone: false);

        Assert.Same(first.Results.Single(), second.Results.Single());
    }

    [Fact]
    public async Task SearchAsync_Clone_MutatingResult_DoesNotChangeLaterResults()
    {
        var (service, _) = CreateService(CreateSetting("Manufacturer", "Acme", "ACME Inc"));

        var cloned = (await service.SearchAsync(CreateCriteria(), clone: true)).Results.Single();
        cloned.FieldName = "Changed";
        cloned.Values.Single().Synonyms.Add("Changed");

        var current = (await service.SearchAsync(CreateCriteria(), clone: false)).Results.Single();
        Assert.Equal("Manufacturer", current.FieldName);
        Assert.Equal(["ACME Inc"], current.Values.Single().Synonyms);
    }

    [Fact]
    public async Task SearchAsync_StoredValueChanged_ReturnsNewSettings()
    {
        var (service, settingEntry) = CreateService(CreateSetting("Manufacturer", "Acme", "ACME Inc"));
        await service.SearchAsync(CreateCriteria(), clone: false);

        settingEntry.Value = Serialize(CreateSetting("Brand", "Globex", "Globex Corp"));

        var result = await service.SearchAsync(CreateCriteria(), clone: false);
        Assert.Equal("Brand", result.Results.Single().FieldName);
    }

    [Fact]
    public async Task SaveChangesAsync_NewSetting_IsReturnedBySearchAndLeavesEarlierResultsIntact()
    {
        var (service, _) = CreateService(CreateSetting("Manufacturer", "Acme", "ACME Inc"));
        var before = await service.SearchAsync(CreateCriteria(), clone: false);

        await service.SaveChangesAsync([CreateSetting("Brand", "Globex", "Globex Corp")]);

        var after = await service.SearchAsync(CreateCriteria(), clone: false);
        Assert.Equal(["Brand", "Manufacturer"], after.Results.Select(x => x.FieldName).Order());
        Assert.Equal(["Manufacturer"], before.Results.Select(x => x.FieldName));
    }

    private static (IndexFieldSettingService Service, ObjectSettingEntry SettingEntry) CreateService(params IndexFieldSetting[] settings)
    {
        var settingEntry = new ObjectSettingEntry
        {
            Name = ModuleConstants.Settings.General.IndexSettings.Name,
            Value = Serialize(settings),
        };

        var settingsManager = new Mock<ISettingsManager>();
        settingsManager
            .Setup(x => x.GetObjectSettingAsync(settingEntry.Name, null, null))
            .ReturnsAsync(settingEntry);
        settingsManager
            .Setup(x => x.SaveObjectSettingsAsync(It.IsAny<IEnumerable<ObjectSettingEntry>>()))
            .Returns(Task.CompletedTask);

        return (new IndexFieldSettingService(settingsManager.Object), settingEntry);
    }

    private static IndexFieldSetting CreateSetting(string fieldName, string value, string synonym)
    {
        return new IndexFieldSetting
        {
            DocumentType = DocumentType,
            FieldName = fieldName,
            Values = [new IndexFieldValueSetting { Value = value, Synonyms = [synonym] }],
        };
    }

    private static IndexFieldSettingSearchCriteria CreateCriteria()
    {
        return new IndexFieldSettingSearchCriteria { DocumentType = DocumentType };
    }

    private static string Serialize(params IndexFieldSetting[] settings)
    {
        return JsonConvert.SerializeObject(settings);
    }
}
