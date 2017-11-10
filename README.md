# VirtoCommerce.Search
VirtoCommerce.Search module defines common abstractions for indexed search functionality and contains implementations for the following search engines:
* Lucene
* Elasticsearch
* Azure Search

![image](https://cloud.githubusercontent.com/assets/5801549/15715109/f338fc1a-2825-11e6-84a6-3c437274a51c.png)

# Documentation
User guide: trigger manual search index rebuild form Catalogs list in Manager.

Developer guide: <a href="https://virtocommerce.com/docs/vc2devguide/deployment/deploy-elasticsearch-to-dedicated-server" target="_blank">Deploy Elasticsearch to dedicated server</a>

# Installation
Installing the module:
* Automatically: in VC Manager go to Configuration -> Modules -> Search module -> Install
* Manually: download module zip package from https://github.com/VirtoCommerce/vc-module-search/releases. In VC Manager go to Configuration -> Modules -> Advanced -> upload module package -> Install.

# Settings
## VirtoCommerce.Search.SearchConnectionString
Search configuration string. The string consists of two parts. First part is provider=XXXX, which specifies which provider to use for search. The remainder of the string is passed to provider's constructor. Currently 3 search providers are supported: Azure Search, Elasticsearch and Lucene.

### Azure Search
```
provider=AzureSearch;server=servicename;key=accesskey;scope=default
```
This provider stores documents in the cloud search service <a href="https://azure.microsoft.com/en-us/services/search/" target="_blank">Azure Search</a>.
* **server** is the name of the search service instance in your Azure account (https://SERVICENAME.search.windows.net).
* **key** is the primary or secondary admin key for this search service.
* **scope** is a common name (prefix) of all indexes. Each document type is stored in a separate index. Full index name is `scope-documenttype`. One search service can serve multiple indexes.

### Elasticsearch
```
provider=Elasticsearch;server=localhost:9200;scope=default
```
This provider stores documents on a standalone <a href="https://www.elastic.co/products/elasticsearch" target="_blank">Elasticsearch</a> server.
* **server** is a network address of the server.
* **scope** is a common name (prefix) of all indexes. Each document type is stored in a separate index. Full index name is `scope-documenttype`. One server can serve multiple indexes.

### Lucene
```
provider=Lucene;server=~/App_Data/Lucene;scope=default
```

This provider stores documents in a local file system.
* **server** is a virtual or physical path to the root directory where indexed documents are stored.
* **scope** is a name of the index. In fact, this is the name of a subdirectory inside the root directory which can contain multiple indexes.

# Search criteria preprocessors
Before building a search query each search provider passes search criteria to several search criteria preprocessors, which can modify the original search criteria. This module registers the preprocessor which parses the `ISearchCriteria.SearchPhrase` string and converts any found filters to real filters which then are added to the `ISearchCriteria.CurrentFilters` collection. Every found filter is removed from the search phrase.

Preprocessors should be registered in the `IModule.Initialize()` method:
```
_container.RegisterType<ISearchCriteriaPreprocessor, PhraseSearchCriteriaPreprocessor>(nameof(PhraseSearchCriteriaPreprocessor));
```

## Search phrase syntax
The search phrase can contain keywords, attribute filters, range filters and price range filters.

### Keyword
Keyword is not converted to a filter and can be a simple string or a quoted string.
* Simple string: any characters, except the following: `space`, `tab`, `"`, `:`, `,`, `[`, `]`, `(`, `)`
* Quoted string: any characters enclosed in double quotes. The double quote character inside the string can be escaped as `\"`. Other supported escape sequences are `\\`, `\r`, `\n`, '\t`.

Examples:
```
simple_string
"quoted \" \r \n \t \\ string"
```

### Attribute filter
Attribute filter is defined as a field name followed by a colon and one or more field values separated with comma:

`field` `:` `value1` `,` `value2` ... `,` `valueN`

For field name and values the same rules apply as for keywords.

Examples:
```
color:red
color:green,"light blue"
"screen size":5.5,6
date:"2017-01-01T00:00:00.000Z"
```

### Range filter
Range filter is defined as a field name followed by a colon and one or more ranges separated with comma.
Range is defined as two range bounds separated by ` TO ` and enclosed in square brackets or parenthesis.
Square bracket includes the range bound value and a parenthesis excludes it. You can mix square brackets and parenthesis in the same range. One of the bounds can be omitted.

`field` `:` `[lower1 TO upper1]` `,` `(lower2 TO upper2)` ... `,` `rangeN`

For field name and range bounds the same rules apply as for keywords.

Examples:
```
size:[5 TO 10]
size:[TO 10),(20 TO]
"screen size":(5.5 TO 6]
date:["2017-01-01T00:00:00.000Z" TO "2017-12-31T23:59:59.999Z"]
```

### Price range filter
Price range filter is a range filter which has a field name `price` and numeric range bounds. Also, the field name can contain currency name separated from `price` with underscore.

Examples:
```
price:[TO 100)
price:[TO 100),[500 TO]
price_usd:[100 TO 500)
```

# Available resources
* [VirtoCommerce.SearchModule.Core](https://www.nuget.org/packages/VirtoCommerce.SearchModule.Core) - a NuGet package with module related abstractions.
* [VirtoCommerce.SearchModule.Data](https://www.nuget.org/packages/VirtoCommerce.SearchModule.Data) - a NuGet package with module related service implementations.

# License
Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
