using Newtonsoft.Json;
using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Data.Model
{
    public class DocumentDictionary : Dictionary<string, object>
    {
        public object Id
        {
            get
            {
                if (this.ContainsKey("__key"))
                    return this["__key"];

                return null;
            }
            set
            {
                if (this.ContainsKey("__key"))
                    this["__key"] = value;
                else
                    this.Add("__key", value);
            }
        }
    }
}
