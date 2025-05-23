using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

public interface IIndexFieldSettingSearchService : ISearchService<IndexFieldSettingSearchCriteria, IndexFieldSettingSearchResult, IndexFieldSetting>
{
}
