using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

public interface IIndexSchemaBuilder
{
    Task BuildSchemaAsync(IndexDocument schema);
}
