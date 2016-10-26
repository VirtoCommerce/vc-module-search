﻿using System;

namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    public interface ISearchIndexController
    {
        void RemoveIndex(string scope, string documentType, string[] documentsIds = null);      
        void BuildIndex(string scope, string documentType, Action<IndexProgressInfo> progressCallback, string[] documentsIds = null);
    }
}
