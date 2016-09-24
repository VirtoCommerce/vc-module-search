using System;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Data.Providers.Azure
{

    public class AzureSearchException : SearchException
    {
        public AzureSearchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AzureSearchException(string message)
            : base(message)
        {
        }
    }
}
