# Virto Commerce Search Module

[![CI status](https://github.com/VirtoCommerce/vc-module-search/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-search/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-search&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-search)

## Overview

The Virto Commerce Search module provides the **core abstractions and orchestration** for indexed search in Virto Commerce. It standardizes how documents are described, indexed, queried, and managed, while delegating the actual search engine implementation to pluggable provider modules.

The module is designed to support multiple document types (e.g., Products, Categories, Members, Orders) and to let you route different document types to different providers when needed.

**Providers**

This module defines common contracts for indexed search across search engines. Use one of the provider modules below or implement your own provider:

* [Elasticsearch 9](https://github.com/VirtoCommerce/vc-module-elastic-search-9): Version compatible with Elasticsearch 8 and 10. For driving innovation like semantic and hybrid search.
* [Elasticsearch 8](https://github.com/VirtoCommerce/vc-module-elastic-search-8): Version compatible with Elasticsearch 8 and 9. For driving innovation like semantic and hybrid search.
* [Algolia](https://github.com/VirtoCommerce/vc-module-algolia-search): A cloud-based search platform that provides developers with a set of APIs to easily implement fast and relevant search experiences in their applications.
* [Azure Cognitive Search](https://github.com/VirtoCommerce/vc-module-azure-search): A fully managed cloud search service offered by Microsoft Azure that enables developers to build powerful search capabilities into applications without the need for managing infrastructure.
* [Lucene](https://github.com/VirtoCommerce/vc-module-lucene-search): Recommended for local development.
* [Elastic App Search](https://github.com/VirtoCommerce/vc-module-elastic-app-search): Preferred search provider with rich no-code search customization and analytics tools.
* [Elasticsearch](https://github.com/VirtoCommerce/vc-module-elastic-search): Version compatible with Elasticsearch 7.x.
* And you can easily create a module and integrate with your favorite search engine.

> If no provider is installed/registered, the module falls back to a dummy provider and will throw a clear runtime error instructing you to install a provider module.

## Key Features

* **Pluggable provider model**: swap search engines without changing the rest of the platform.
* **Multi-document support**: index and search multiple document types in a unified way.
* **Per-document provider & scope routing**: select provider/scope per document type via configuration.
* **Indexing orchestration**: supports full rebuild and incremental “changes” indexation.
* **Blue-green indexation support**: supports backup indices and index swap for providers that implement it.
* **Scheduled background jobs**: recurring indexation via Hangfire, controllable by platform settings.
* **Search phrase parsing**: query/phrase parsing infrastructure (ANTLR-based) for consistent request building.
* **Admin integration**: API endpoints, permissions, and back office UI assets/localization for managing indexing.

## Configuration

### Provider selection (required)

The module binds `SearchOptions` from the `Search` configuration section. At minimum, you must specify:

- `Search:Provider` (default provider name)
- `Search:Scope` (default scope)

Provider modules usually require additional provider-specific configuration under `Search:<ProviderName>`.

```json
{
  "Search": {
    "Provider": "ElasticAppSearch",
    "Scope": "default",
    "ElasticAppSearch": {
      "Endpoint": "https://localhost:3002",
      "PrivateApiKey": "private-key",
      "KibanaBaseUrl": "https://localhost:5601"
    }
  }
}
```

### Select provider (and/or scope) per document type

Use `Search:DocumentScopes` to override the provider and/or scope for a specific document type:

```json
{
  "Search": {
    "Provider": "ElasticAppSearch",
    "Scope": "default",
    "DocumentScopes": [
      {
        "DocumentType": "Category",
        "Provider": "ElasticSearch8",
        "Scope": "catalog"
      }
    ]
  }
}
```

### Platform settings

The module also exposes operational settings via the Virto Commerce settings system:

- **VirtoCommerce.Search.IndexPartitionSize**: indexation batch size; default `50`
- **VirtoCommerce.Search.PartialDocumentUpdate.Enable**: enable partial updates (provider must support it); default `false`
- **VirtoCommerce.Search.IndexingJobs.Enable**: enable recurring indexation job; default `true`
- **VirtoCommerce.Search.IndexingJobs.CronExpression**: Hangfire cron for recurring indexation; default `"0/5 * * * *"`

## Architecture

At runtime, the module acts as an orchestration layer between the Virto Commerce platform and a concrete search provider:

- **Provider routing via gateway**
  - Providers register themselves into `SearchGateway` using `appBuilder.UseSearchProvider<T>("ProviderName")`.
  - `SearchGateway` implements `ISearchProvider` and routes calls to the configured provider based on `SearchOptions` (`Search:Provider` and `Search:DocumentScopes`).
  - A fallback provider (registered with `name: null`) is always present to ensure misconfiguration fails fast with a clear message.

- **Indexing orchestration**
  - `IIndexingManager` is responsible for index state, full rebuild, incremental changes indexation, and immediate batch operations.
  - Indexation can be triggered manually (via API/back office) or automatically using Hangfire recurring jobs (`IndexingJobs`).

- **Operational API**
  - The module exposes REST endpoints under `api/search/indexes` for index status, rebuild, cancellation, and (when supported by provider) index swap.
  - Access is controlled by module permissions (e.g., `search:index:read`, `search:index:rebuild`, `search:index:manage`).

### Search phrase parsing

The module includes a built-in **search phrase parsing** pipeline (ANTLR-based) used to interpret user-entered search phrases consistently across providers.

- **Implementation location**: `src/VirtoCommerce.SearchModule.Data/SearchPhraseParsing`
- **Entry point**: `ISearchPhraseParser` / `SearchPhraseParser`
- **Purpose**: tokenize and parse phrases into a structured form that can be used by request builders/providers (e.g., to support consistent behavior for quoting, operators, and validation).

#### Examples


##### 1) Keyword-only search

Input:
```txt
  one two three
```
Result:
```txt
  Keyword: "one two three"
  Filters: []
```

##### 2) Term filter + quoted keyword (keeps punctuation/spaces)

Input:
```txt
  color:red,blue "B2B, test"
```
Result:
```txt
  Keyword: "B2B, test"
  Filters:
    - TermFilter(field="color", values=["red","blue"])
```

##### 3) Negation

Input:
```txt
  !size:medium
```
Result:
```txt
  Keyword: ""
  Filters:
    - NotFilter(TermFilter(field="size", values=["medium"]))
``` 

##### 4) Range filter (inclusive/exclusive bounds + optional sides)

Input:
```txt
  createddate:["2023-12-01T01:12:00Z" TO "2023-12-31T01:12:00Z"]
```
Result:
```txt
  Keyword: ""
  Filters:
    - RangeFilter(field="createddate", values=[{ lower="2023-12-01T01:12:00Z", upper="2023-12-31T01:12:00Z", includeLower=true, includeUpper=true }])
```

##### 5) Boolean composition with parentheses

Input:
```txt
  (!size:medium AND (color:red OR color:blue)) "running shoes"
```
Result:
```txt
  Keyword: "running shoes"
  Filters:
    - AndFilter(
        NotFilter(TermFilter(field="size", values=["medium"])),
        OrFilter(
          TermFilter(field="color", values=["red"]),
          TermFilter(field="color", values=["blue"])
        )
      )
```

##### 6) Escaping inside quoted strings

Input:
```txt
  brand:"ACME \"Pro\""
```
Result:
```txt
  Keyword: ""
  Filters:
    - TermFilter(field="brand", values=["ACME \"Pro\""])
```

## Components

### Project Structure

```text
vc-module-search/
├── src/
│   ├── VirtoCommerce.SearchModule.Core/         # Domain models, abstractions, settings, extensions
│   ├── VirtoCommerce.SearchModule.Data/         # Provider gateway, indexing services, background jobs, phrase parsing
│   └── VirtoCommerce.SearchModule.Web/          # Module bootstrap, Web API, back office assets & localization
└── tests/
    └── VirtoCommerce.SearchModule.Tests/        # Unit and integration tests
```

### Key Components

- **SearchOptions**: binds `Search:Provider`, `Search:Scope`, and `Search:DocumentScopes` for routing by document type.
- **SearchGateway**: `ISearchProvider` multiplexer that routes search/index calls to the configured provider (plus fallback provider).
- **ISearchProvider**: provider contract implemented by provider modules (Lucene / Elasticsearch / Azure Search / Algolia, etc.).
- **IIndexingManager**: central orchestration for index state, full rebuild, incremental changes indexation, and batch operations.
- **IndexingJobs**: Hangfire jobs for recurring changes indexation and manual full indexation, controlled by platform settings.
- **SearchIndexationModuleController**: operational API under `api/search/indexes` for status, rebuild, cancel, and index swap (when supported).
- **DummySearchProvider**: fallback provider that throws a clear error when no real provider is registered.

## Documentation

* [Search module user documentation](https://docs.virtocommerce.org/platform/user-guide/search/overview/)
* [Search module developer documentation](https://docs.virtocommerce.org/platform/developer-guide/Fundamentals/Indexed-Search/overview/)
* [REST API](https://virtostart-demo-admin.govirto.com/docs/index.html?urls.primaryName=VirtoCommerce.Search)
* [Search providers configuration](https://docs.virtocommerce.org/platform/developer-guide/Configuration-Reference/appsettingsjson/#search)
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-search)


## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-search/releases/latest)

## License
Copyright (c) Virto Solutions LTD.  All rights reserved.

This software is licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense.

Unless required by the applicable law or agreed to in written form, the software
distributed under the License is provided on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
