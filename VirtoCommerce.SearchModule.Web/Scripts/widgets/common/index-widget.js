angular.module('virtoCommerce.searchModule')
.controller('virtoCommerce.searchModule.indexWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.searchModule.search', function ($scope, bladeNavigationService, searchAPI) {
    var blade = $scope.blade;
    $scope.loading = true;

    function refresh() {
        searchAPI.query({ documentType: $scope.widget.documentType, documentId: blade.currentEntityId }, function (data) {
            if (_.any(data)) {
                $scope.index = data[0];
                $scope.indexDate = moment.utc($scope.index.lastindexdate, 'YYYYMMDDHHmmss');
            }

            $scope.loading = false;
            updateStatus();
        });
    }

    function updateStatus() {
        if (!$scope.loading && blade.currentEntity) {
            $scope.widget.UIclass = !$scope.index || ($scope.indexDate < moment.utc(blade.currentEntity.modifiedDate)) ? 'error' : '';
        }
    }

    $scope.openBlade = function () {
        var newBlade = {
            id: 'detailChild',
            currentEntityId: blade.currentEntityId,
            data: $scope.index,
            indexDate: $scope.indexDate,
            documentType: $scope.widget.documentType,
            parentRefresh: refresh,
            title: blade.currentEntity.name,
            subtitle: 'search.blades.index-detail.subtitle',
            controller: 'virtoCommerce.searchModule.indexDetailController',
            template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/index-detail.tpl.html'
        };

        if (!$scope.index) {
            angular.extend(newBlade, {
                title: 'search.blades.index-detail.title-new',
                subtitle: undefined,
            });
        }

        bladeNavigationService.showBlade(newBlade, blade);
    };

    $scope.$watch('blade.currentEntity', updateStatus);

    refresh();
}]);
