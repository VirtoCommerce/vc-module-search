angular.module('virtoCommerce.searchModule')
.controller('virtoCommerce.searchModule.indexDetailController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'virtoCommerce.searchModule.search', function ($scope, bladeNavigationService, dialogService, searchAPI) {
    var blade = $scope.blade;

    blade.initialize = function (data) {
        blade.currentEntity = data;
        blade.isLoading = false;
    };

    blade.headIcon = 'fa-search';

    // blade.toolbarCommands = [];
    // Common index rebuild command added in module's run method:
    // toolbarService.register(rebuildIndexCommand, 'virtoCommerce.searchModule.indexDetailController');

    blade.initialize(blade.data);
}]);