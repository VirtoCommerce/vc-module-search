using System.Text;
using Lucene.Net.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.Lucene
{
    public class LuceneSearchQuery
    {
        public Query Query { get; set; }

        public Filter Filter { get; set; }

        #region Overrides of Object

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder();

            if (Query != null)
                builder.AppendFormat("query:{0}", Query);

            if (Filter != null)
                builder.AppendFormat("filter:{0}", Filter);

            return builder.ToString();
        }

        #endregion
    }
}
