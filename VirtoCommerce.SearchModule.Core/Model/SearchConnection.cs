using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace VirtoCommerce.SearchModule.Core.Model
{
    /// <summary>
    /// Contains connection parameters to connecting to the search service.
    /// </summary>
    public class SearchConnection : ISearchConnection
    {
        private readonly Dictionary<string, string> _parameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchConnection"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string: "provider=MyProvider;parameter1=value1;...;parameterN=valueN"</param>
        public SearchConnection(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString");

            _parameters = ParseString(connectionString);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchConnection"/> class.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        /// <param name="scope">The scope.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="accessKey"></param>
        public SearchConnection(string dataSource, string scope, string provider = "default", string accessKey = "")
        {
            _parameters = new Dictionary<string, string>();
            DataSource = dataSource;
            Scope = scope;
            Provider = provider;
            AccessKey = accessKey;
        }

        private Dictionary<string, string> ParseString(string s)
        {
            var nvc = new Dictionary<string, string>();

            // remove anything other than query string from url
            if (s.Contains("?"))
            {
                s = s.Substring(s.IndexOf('?') + 1);
            }

            foreach (var vp in Regex.Split(s, ";"))
            {
                var singlePair = Regex.Split(vp, "=");
                nvc.Add(singlePair[0], singlePair.Length == 2 ? singlePair[1] : string.Empty);
            }

            return nvc;
        }

        public string DataSource
        {
            get
            {
                if (_parameters != null)
                    return _parameters["server"];

                throw new InvalidOperationException("DataSource must be specified using server parameter for the search connection string");
            }
            private set
            {
                _parameters.Add("server", value);
            }
        }

        public string AccessKey
        {
            get
            {
                if (_parameters != null)
                    return _parameters["key"];

                throw new InvalidOperationException("Key must be specified using server parameter for the search connection string");
            }
            private set
            {
                _parameters.Add("key", value);
            }
        }

        public string Scope
        {
            get
            {
                if (_parameters != null)
                    return _parameters["scope"];
                return "default";
            }
            private set
            {
                _parameters.Add("scope", value);
            }
        }

        public string Provider
        {
            get
            {
                if (_parameters != null && _parameters.ContainsKey("provider"))
                    return _parameters["provider"];
                return "default";
            }
            private set
            {
                _parameters.Add("provider", value);
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var parameter in _parameters.Keys)
            {
                builder.AppendFormat("{2}{0}={1}", parameter, _parameters[parameter], builder.Length > 0 ? ";" : string.Empty);
            }

            return builder.ToString();
        }
    }
}
