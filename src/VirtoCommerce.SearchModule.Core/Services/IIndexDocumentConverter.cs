using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

public interface IIndexDocumentConverter
{
    Task ConvertAsync(string documentType, IList<IndexDocument> documents);
}
