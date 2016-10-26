angular.module('virtoCommerce.searchModule')
.controller('virtoCommerce.searchModule.indexProgressController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.modules', function ($scope, bladeNavigationService, modules) {
    var blade = $scope.blade;

    $scope.$on("new-notification-event", function (event, notification) {
        if (blade.currentEntity && notification.id == blade.currentEntity.id) {
            angular.copy(notification, blade.currentEntity);
            if (notification.finished && _.any(notification.progressLog) && _.last(notification.progressLog).level !== 'Error' && blade.parentRefresh) {
                blade.parentRefresh();
            }
        }
    });

    blade.title = blade.currentEntity.documentType;
    blade.subtitle = 'search.blades.index-progress.subtitle';
    blade.headIcon = 'fa-search';
    blade.isLoading = false;
}]);