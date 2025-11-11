using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model;

public interface IHasAggregations
{
    IList<Aggregation> Aggregations { get; set; }
}
