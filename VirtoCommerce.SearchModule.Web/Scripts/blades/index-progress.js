angular.module('virtoCommerce.searchModule')
.controller('virtoCommerce.searchModule.indexProgressController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.modules', function ($scope, bladeNavigationService, modules) {
    var blade = $scope.blade;

    $scope.$on("new-notification-event", function (event, notification) {
        if (blade.notification && notification.id == blade.notification.id) {
            angular.copy(notification, blade.notification);
            if (notification.finished && blade.parentRefresh) {
                blade.parentRefresh();
            }
        }
    });

    blade.title = blade.notification.title;
    blade.headIcon = 'fa-search';
    blade.isLoading = false;
}]);