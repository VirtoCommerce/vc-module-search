angular.module('virtoCommerce.searchModule')
.controller('virtoCommerce.searchModule.indexWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.searchModule.search', function ($scope, bladeNavigationService, searchAPI) {
    var blade = $scope.blade;
    $scope.loading = true;

    searchAPI.query({ documentType: $scope.widget.documentType, documentId: blade.currentEntityId }, function (data) {
        if (_.any(data)) {
            $scope.index = data[0];
        }

        $scope.loading = false;
        updateStatus();
    });

    $scope.openBlade = function () {
        var newBlade = {
            id: 'detailChild',
            currentEntityId: blade.currentEntityId,
            data: $scope.index,
            documentType: $scope.widget.documentType,
            parentRefresh: blade.parentRefresh,
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

    function updateStatus() {
        if (!$scope.loading && blade.currentEntity) {
            if (!$scope.index) {
                $scope.widget.UIclass = 'error';
            } else if ($scope.index.buildDate < blade.currentEntity.modifiedDate)
                $scope.widget.UIclass = 'error';
        }
    }

    $scope.$watch('blade.currentEntity', updateStatus);
}]);
