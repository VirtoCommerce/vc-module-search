using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

public interface ISupportIndexCreate
{
    Task CreateIndexAsync(string documentType, IndexDocument schema);
}
