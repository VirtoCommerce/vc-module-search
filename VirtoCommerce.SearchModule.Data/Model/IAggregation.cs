using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtoCommerce.SearchModule.Data.Model
{
    public interface IAggregation
    {
        IDictionary<string, object> Meta { get; set; }
    }
}
