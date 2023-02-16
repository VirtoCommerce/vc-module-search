namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface IIndexQueueServiceFactory
    {
        IIndexQueueService Create();
    }
}
