angular.module('virtoCommerce.searchModule')
.controller('virtoCommerce.searchModule.indexDetailController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'virtoCommerce.searchModule.search', function ($scope, bladeNavigationService, dialogService, searchAPI) {
    var blade = $scope.blade;
    blade.updatePermission = 'VirtoCommerce.Search:Index:Rebuild';
    blade.isNew = !blade.data;

    blade.initialize = function (data) {
        blade.currentEntity = data;
        blade.isLoading = false;
    };

    function openProgressBlade(data) {
        // show indexing progress
        var newBlade = {
            id: 'indexProgress',
            notification: data,
            parentRefresh: blade.parentRefresh,
            controller: 'virtoCommerce.searchModule.indexProgressController',
            template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/index-progress.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, blade.parentBlade);
    }

    blade.headIcon = 'fa-search';

    blade.toolbarCommands = [
        {
            name: "search.commands.index", icon: 'fa fa-recycle',
            executeMethod: function () {
                searchAPI.index({ documentType: blade.documentType }, [{ id: blade.currentEntityId }], openProgressBlade);
            },
            canExecuteMethod: function () { return true; },
            permission: 'VirtoCommerce.Search:Index:Rebuild'
        },
        {
            name: "search.commands.index-all", icon: 'fa fa-recycle',
            executeMethod: function () {
                dialogService.showConfirmationDialog({
                    id: "confirm",
                    title: "search.dialogs.rebuild-index.title",
                    message: "search.dialogs.rebuild-index.message",
                    callback: function (confirmed) {
                        if (confirmed) {
                            searchAPI.reindex({ documentType: blade.documentType }, [{ id : blade.currentEntityId }], openProgressBlade);
                        }
                    }
                });
            },
            canExecuteMethod: function () { return true; },
            permission: 'VirtoCommerce.Search:Index:Rebuild'
        }
    ];

    blade.initialize(blade.data);
}]);