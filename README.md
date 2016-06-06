# VirtoCommerce.Search
VirtoCommerce.Search module provides indexed search functionality with Lucene and ElasticSearch engines.
Key features:
* Lucene search engine support
* ElasticSearch engine support
* Microsoft Azure support

![image](https://cloud.githubusercontent.com/assets/5801549/15715109/f338fc1a-2825-11e6-84a6-3c437274a51c.png)

# Documentation
User guide: trigger manual search index rebuild form Catalogs list in Manager.

Developer guide:
* <a href="http://docs.virtocommerce.com/display/vc2devguide/Platform+settings#Platformsettings-SearchConnectionString" target="_blank">Configuring SearchConnectionString setting</a>
* <a href="http://docs.virtocommerce.com/x/FADl" target="_blank">Deploy Elasticsearch to dedicated server</a>

# Installation
Installing the module:
* Automatically: in VC Manager go to Configuration -> Modules -> Search module -> Install
* Manually: download module zip package from https://github.com/VirtoCommerce/vc-module-search/releases. In VC Manager go to Configuration -> Modules -> Advanced -> upload module package -> Install.

# Available resources
* Module related service implementations as a <a href="https://www.nuget.org/packages/VirtoCommerce.SearchModule.Data" target="_blank">NuGet package</a>
* API client as a <a href="https://www.nuget.org/packages/VirtoCommerce.SearchModule.Client" target="_blank">NuGet package</a>
* API client documentation http://demo.virtocommerce.com/admin/docs/ui/index#!/Search_module

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
