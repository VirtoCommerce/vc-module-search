# Virto Commerce Search Module

[![CI status](https://github.com/VirtoCommerce/vc-module-search/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-search/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search)

VirtoCommerce.Search module defines common abstractions for indexed search functionality for the following Search Engines:
* [Lucene](https://github.com/VirtoCommerce/vc-module-lucene-search)
* [Elasticsearch](https://github.com/VirtoCommerce/vc-module-elastic-search)
* [Azure Cognitive Search](https://github.com/VirtoCommerce/vc-module-azure-search)
* [Elastic App Search](https://github.com/VirtoCommerce/vc-module-elastic-app-search)
* [Algolia](https://github.com/VirtoCommerce/vc-module-algolia-search)
and any other custom engine.

## Features
* Full-Text Search Capability
* Extensibile Document Model
* Multi Document Support. Ex: Product, Categories, Members, Orders, etc.
* Blue Green Indexation
* Indexation Logs
* Admin Back Office


## Permissions

* `search:index:access`: Allows access to the Search Index menu.
* `search:index:rebuild`: Allows access to indexation functions (delete and build).

## Documentation

* [Virto Commerce Documentation](https://docs.virtocommerce.org)


## References

* Deploy: https://virtocommerce.com/docs/latest/developer-guide/deploy-module-from-source-code/
* Installation: https://www.virtocommerce.com/docs/latest/user-guide/modules/
* Home: https://virtocommerce.com
* Community: https://www.virtocommerce.org
* [Download Latest Release](https://github.com/VirtoCommerce/vc-module-catalog/releases/latest)

## License

Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
