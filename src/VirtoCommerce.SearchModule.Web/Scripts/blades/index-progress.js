angular.module('virtoCommerce.searchModule')
.controller('virtoCommerce.searchModule.indexProgressController', ['$scope', 'virtoCommerce.searchModule.searchIndexation', function ($scope, searchIndexationApi) {
    var blade = $scope.blade;

    $scope.$on("new-notification-event", function (event, notification) {
        if (blade.notification && notification.id == blade.notification.id) {
            blade.progressPercentage = notification.processedCount / notification.totalCount * 100;
            angular.copy(notification, blade.notification);
            if (notification.finished && blade.parentRefresh) {
                blade.parentRefresh();
            }
        }
    });

    blade.toolbarCommands = [{
        name: 'platform.commands.cancel',
        icon: 'fa fa-times',
        canExecuteMethod: function() {
            return blade.notification && !blade.notification.finished;
        },
        executeMethod: function() {
            searchIndexationApi.cancel({ taskId: blade.notification.id });
        }
    }];

    blade.title = blade.notification.title;
    blade.headIcon = 'fa fa-search';
    blade.isLoading = false;

    $scope.getElapsedTime = function () {
        if ($scope.elapsedTime) {
            return $scope.elapsedTime;
        }

        var start = new Date(blade.notification.created);
        var end = blade.notification.finished ? new Date(blade.notification.finished) : new Date();
        var result = calculateElapsedTime(start, end);

        if (blade.notification.finished) {
            $scope.elapsedTime = result;
        }

        return result;
    }

    function calculateElapsedTime(start, end) {
        const msPerSecond = 1000;
        const msPerMinute = 60 * msPerSecond;
        const msPerHour = 60 * msPerMinute;

        var elapsedMs = end - start;

        var hours = Math.floor(elapsedMs / msPerHour);
        elapsedMs -= hours * msPerHour;
        var minutes = Math.floor(elapsedMs / msPerMinute);
        elapsedMs -= minutes * msPerMinute;
        var seconds = Math.floor(elapsedMs / (1000));

        return hours + ':' + minutes + ':' + seconds;
    }
}]);
