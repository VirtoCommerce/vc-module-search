using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model;

public class Aggregation
{
    /// <summary>
    /// Gets or sets the value of the aggregation type
    /// </summary>
    /// <value>
    /// "Attribute", "PriceRange", "Range" or "Category"
    /// </value>
    public string AggregationType { get; set; }

    /// <summary>
    /// Gets or sets the value of the aggregation field
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// Gets or sets the collection of the aggregation labels
    /// </summary>
    public IList<AggregationLabel> Labels { get; set; }

    /// <summary>
    /// Gets or sets the collection of the aggregation items
    /// </summary>
    public IList<AggregationItem> Items { get; set; }

    /// <summary>
    /// Statistics for range aggregations, such as "PriceRange" or "Range".
    /// </summary>
    public AggregationStatistics Statistics { get; set; }
}
