angular.module('virtoCommerce.searchModule')
    .factory('virtoCommerce.searchModule.indexFieldSettingsApi', ['$resource', function ($resource) {
        return $resource('api/search/index-field-settings', {}, {
            search: { method: 'POST', url: 'api/search/index-field-settings/search' },
            save: { method: 'POST' },
            delete: { method: 'DELETE' },
            getFieldValues: { method: 'GET', url: 'api/search/field-values', isArray: true },
        });
    }]);
