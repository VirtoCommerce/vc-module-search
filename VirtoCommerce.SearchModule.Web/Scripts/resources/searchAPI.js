angular.module('virtoCommerce.searchModule')
.factory('virtoCommerce.searchModule.search', ['$resource', function ($resource) {
    return $resource('api/search/index/:documentType/:documentId', {}, {
        index: { method: 'POST' },
        // index: { method: 'POST', url: 'api/search/index/:documentType' },
        reindex: { method: 'POST', url: 'api/search/reindex/:documentType' },
        rebuild: { url: 'api/search/catalogitem/rebuild' },
    });
}]);
