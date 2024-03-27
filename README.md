# Virto Commerce Search Module

[![CI status](https://github.com/VirtoCommerce/vc-module-search/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-search/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search)

The VirtoCommerce.Search module provides a comprehensive solution for indexed search functionality, offering full-text search capability, extensible document models, and multi-document support. It enables efficient indexing, querying, and management of search data for various e-commerce entities, empowering administrators to optimize search experiences for end-users.

## Providers
The VirtoCommerce.Search module defines common abstractions for indexed search functionality across various search engines, providing flexibility and scalability for e-commerce search solutions. Choose from a range of well-know search engines:

* [Elastic App Search](https://github.com/VirtoCommerce/vc-module-elastic-app-search) - Preferred search provider with rich no-code search customization and analytics tools.
* [Elasticsearch 8](https://github.com/VirtoCommerce/vc-module-elastic-search-8) -  Version compatible with Elasticsearch 8.x. For driving innovation like semantic and hybrid search.
* [Lucene](https://github.com/VirtoCommerce/vc-module-lucene-search) - Recommended for local development щтдн.
* [Elasticsearch](https://github.com/VirtoCommerce/vc-module-elastic-search) - Version compatible with Elasticsearch 7.x.
* [Azure Cognitive Search](https://github.com/VirtoCommerce/vc-module-azure-search) 
* [Algolia](https://github.com/VirtoCommerce/vc-module-algolia-search)
  
or create a custom search provider to integrate with another search engine.

## Features
* Full-Text Search Capability
* Extensible Document Model
* Multi-Document Support (e.g., Product, Categories, Members, Orders, etc.)
* Blue Green Indexation
* Indexation Logs
* Native Integration with Admin Back Office

## Architecture
Explore the [Indexed Search Overview](https://docs.virtocommerce.org/platform/developer-guide/Fundamentals/Indexed-Search/overview/) for detailed insights into the architecture and functionality of the VirtoCommerce.Search module.

## Configuration
Configure the search provider modules and activate them in the Search.Provider section, providing connection parameters as specified in the module documentation:

```json
"Search": {
		"Provider": "ElasticAppSearch",
		"Scope": "default",
		"ElasticAppSearch": {
      "Endpoint": "https://localhost:3002",
			"PrivateApiKey": "private-key",
		  "KibanaBaseUrl": "https://localhost:5601"
		}
	}
```

## Select Search Provider per Document Type
Tailor the search provider per document type to optimize search performance and functionality. Configure the provider for each document type as needed:

```
{
  "Search": {
    "Provider": "ElasticAppSearch",
    "DocumentScopes": [
      {
        "DocumentType": "Category",
        "Provider": "ElasticSearch8"
      }
    ]
  }
}
```

## References
* Home: https://virtocommerce.com
* Documantation: https://docs.virtocommerce.org
* Community: https://www.virtocommerce.org
* [Download Latest Release](https://github.com/VirtoCommerce/vc-module-search/releases/latest)

## License
Copyright (c) Virto Solutions LTD.  All rights reserved.

This software is licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense.

Unless required by the applicable law or agreed to in written form, the software
distributed under the License is provided on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
