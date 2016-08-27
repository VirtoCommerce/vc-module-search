using System;
using VirtoCommerce.SearchModule.Data.Model;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    /// <summary>
    /// General Elastic Search Exception
    /// </summary>
    public class ElasticSearchException : SearchException
    {

        public ElasticSearchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ElasticSearchException(string message)
            : base(message)
        {
        }

    }
}
