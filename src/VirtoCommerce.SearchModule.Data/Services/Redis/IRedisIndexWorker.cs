using System.Threading.Tasks;

namespace VirtoCommerce.SearchModule.Data.Services.Redis;

public interface IRedisIndexWorker
{
    Task Start();
}
