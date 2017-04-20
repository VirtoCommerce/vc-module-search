# VirtoCommerce.Search
VirtoCommerce.Search module provides indexed search functionality with Lucene and ElasticSearch engines.
Key features:
* Lucene search engine support
* ElasticSearch engine support
* Microsoft Azure support

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
* **scope** is a commont name (prefix) of all indexes. Each document type is stored in a seaprate index. Full index name is `scope-documenttype`. One search service can serve multiple indexes.

### Elasticsearch
```
provider=Elasticsearch;server=localhost:9200;scope=default
```
This provider stores documents on a standalone <a href="https://www.elastic.co/products/elasticsearch" target="_blank">Elasticsearch</a> server.
* **server** is a network address of the server.
* **scope** is a commont name (prefix) of all indexes. Each document type is stored in a seaprate index. Full index name is `scope-documenttype`. One server can serve multiple indexes.

### Lucene
```
provider=Lucene;server=~/App_Data/Lucene;scope=default
```

This provider stores documents in a local file system.
* **server** is a virtual or physical path to the root directory where indexed documents are stored.
* **scope** is a name of the index. In fact, this is the name of a subdirectory inside the root directory which can contain multiple indexes.

# Available resources
* [VirtoCommerce.SearchModule.Core](https://www.nuget.org/packages/VirtoCommerce.SearchModule.Core) - a NuGet package with module related abstractions.
* [VirtoCommerce.SearchModule.Data](https://www.nuget.org/packages/VirtoCommerce.SearchModule.Data) - a NuGet package with module related service implementations.

# License
Copyright (c) Virtosoftware Ltd.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
