using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class GeoDistanceFilter : ValueObject, IFilter, INamedFilter
    {
        public string FieldName { get; set; }

        /// <summary>
        /// The point from which the distance is measured
        /// </summary>
        public GeoPoint Location { get; set; }

        /// <summary>
        ///  Gets or sets distance for search by location (radius) in any measure units (meters, kilometers etc)
        /// </summary>
        public double Distance { get; set; }

        public override object Clone()
        {
            var result = base.Clone() as GeoDistanceFilter;
            result.Location = Location?.Clone() as GeoPoint;

            return result;
        }

    }
}
