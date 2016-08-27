using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.SearchModule.Data.Model.Filters;

namespace VirtoCommerce.SearchModule.Web.Services
{
    public class FilterService : IBrowseFilterService
    {
        private readonly IStoreService _storeService;
        private ISearchFilter[] _filters;

        public FilterService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public ISearchFilter[] GetFilters(IDictionary<string, object> context)
        {
            if (_filters != null)
            {
                return _filters;
            }

            var filters = new List<ISearchFilter>();

            var storeId = GetStringValue(context, "StoreId");
            if (!string.IsNullOrEmpty(storeId)) // include store filters
            {
                var store = _storeService.GetById(storeId);
                if (store != null)
                {
                    var browsing = GetFilteredBrowsing(store);
                    if (browsing != null)
                    {
                        if (browsing.Attributes != null)
                        {
                            filters.AddRange(browsing.Attributes);
                        }

                        if (browsing.AttributeRanges != null)
                        {
                            filters.AddRange(browsing.AttributeRanges);
                        }

                        if (browsing.Prices != null)
                        {
                            filters.AddRange(browsing.Prices);
                        }
                    }
                }
            }

            _filters = filters.ToArray();
            return _filters;
        }


        private static string GetStringValue(IDictionary<string, object> context, string key)
        {
            string result = null;

            if (context.ContainsKey(key))
            {
                var value = context[key];

                if (value != null)
                {
                    result = value.ToString();
                }
            }

            return result;
        }

        private static FilteredBrowsing GetFilteredBrowsing(IHasDynamicProperties store)
        {
            FilteredBrowsing result = null;

            var filterSettingValue = store.GetDynamicPropertyValue("FilteredBrowsing", string.Empty);

            if (!string.IsNullOrEmpty(filterSettingValue))
            {
                var reader = new StringReader(filterSettingValue);
                var serializer = new XmlSerializer(typeof(FilteredBrowsing));
                result = serializer.Deserialize(reader) as FilteredBrowsing;
            }

            return result;
        }
    }
}
