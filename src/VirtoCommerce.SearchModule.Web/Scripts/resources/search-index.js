angular.module('virtoCommerce.searchModule')
.factory('virtoCommerce.searchModule.searchIndexation', ['$resource', function ($resource) {
    return $resource('api/search/indexes', {}, {
        get: { method: 'GET', isArray: true },
        getAll: { method: 'GET', url: 'api/search/indexes/all', isArray: true },
        getDocIndex: { method: 'GET', url: 'api/search/indexes/index/:documentType/:documentId', isArray: true },
        index: { method: 'POST', url: 'api/search/indexes/index' },       
        cancel: { method: 'GET', url: 'api/search/indexes/tasks/:taskId/cancel' },
        swapIndexSupported: { method: 'GET', url: 'api/search/indexes/swapIndexSupported' },
        swapIndex: { method: 'POST', url: 'api/search/indexes/swapIndex' }
    });
}]);
