namespace VirtoCommerce.SearchModule.Core.Model.Search
{
    public class Facet
    {
        /// <summary>
        /// Gets the group.
        /// </summary>
        /// <value>The group.</value>
        public FacetGroup Group { get; set; }

        /// <summary>
        /// Gets the facet labels.
        /// </summary>
        public FacetLabel[] Labels { get; }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public long? Count { get; }

        /// <summary>
        /// Gets or sets the Key.
        /// </summary>
        /// <value>The URL.</value>
        public string Key { get; }

        public Facet()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Facet"/> class.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="key">The key.</param>
        /// <param name="count">The count.</param>
        /// <param name="labels">The labels.</param>
        public Facet(FacetGroup group, string key, long? count, FacetLabel[] labels)
        {
            Group = group;
            Key = key;
            Labels = labels;
            Count = count;
        }
    }
}
