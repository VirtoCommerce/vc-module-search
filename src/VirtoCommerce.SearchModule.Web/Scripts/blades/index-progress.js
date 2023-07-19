angular.module('virtoCommerce.searchModule')
    .controller('virtoCommerce.searchModule.indexProgressController', [
        '$scope', '$interval', 'virtoCommerce.searchModule.searchIndexation',
        function ($scope, $interval, searchIndexationApi) {
            var blade = $scope.blade;
            blade.title = blade.notification.title;
            blade.headIcon = 'fa fa-search';
            blade.isLoading = false;

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
                canExecuteMethod: function () {
                    return blade.notification && !blade.notification.finished;
                },
                executeMethod: function () {
                    searchIndexationApi.cancel({ taskId: blade.notification.id });
                }
            }];

            var elapsedTimeIntervalPromise = startAnimatingElapsedTime();

            function startAnimatingElapsedTime() {
                stopAnimatingElapsedTime();
                updateElapsedTime();

                return $interval(updateElapsedTime, 1000);
            }

            function stopAnimatingElapsedTime() {
                if (angular.isDefined(elapsedTimeIntervalPromise)) {
                    $interval.cancel(elapsedTimeIntervalPromise);
                }
            }

            function updateElapsedTime() {
                $scope.elapsedTime = getElapsedTime();
            }

            function getElapsedTime() {
                if ($scope.finalElapsedTime) {
                    return $scope.finalElapsedTime;
                }

                var start = new Date(blade.notification.created);
                var end = blade.notification.finished ? new Date(blade.notification.finished) : new Date();
                var result = calculateElapsedTime(start, end);

                if (blade.notification.finished) {
                    $scope.finalElapsedTime = result;
                    stopAnimatingElapsedTime();
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

                return `${hours}:${minutes}:${seconds}`;
            }
        }]);
