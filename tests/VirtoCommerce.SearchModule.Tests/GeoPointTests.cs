using VirtoCommerce.SearchModule.Core.Model;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests
{
    public class GeoPointTests
    {

        [Theory]
        [InlineData(-90, 90)]
        [InlineData(90, -90)]
        [InlineData(0.123, 0.123)]
        [InlineData(54.0, 20.0)]
        [InlineData(54.1234, 20.1234)]
        [InlineData(54.12345678, 20.12345678)]
        [InlineData(54.1234567891, 20.1234567891)]
        [InlineData(-54.0, -20.0)]
        [InlineData(-54.1234, -20.1234)]
        [InlineData(-54.12345678, -20.12345678)]
        [InlineData(-54.1234567891, -20.1234567891)]
        public void GeoPoint_ToString_Parse(double latitude, double longitude)
        {
            var geoPoint = new GeoPoint(latitude, longitude);
            // Arrange
            var strGeoPoint = geoPoint.ToString();

            // Act
            var result = GeoPoint.Parse(strGeoPoint);

            // Assert
            Assert.Equal(geoPoint.ToString(), result.ToString());
        }
    }
}
