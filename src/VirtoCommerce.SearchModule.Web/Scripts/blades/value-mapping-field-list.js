angular.module('virtoCommerce.searchModule')
    .controller('virtoCommerce.searchModule.valueMappingFieldListController', [
        '$scope', 'uiGridConstants', 'platformWebApp.uiGridHelper', 'platformWebApp.bladeUtils', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'virtoCommerce.searchModule.indexFieldSettingsApi',
        function ($scope, uiGridConstants, uiGridHelper, bladeUtils, bladeNavigationService, dialogService, indexFieldSettingsApi) {
            var blade = $scope.blade;
            blade.headIcon = 'fas fa-wrench';
            blade.title = 'search.blades.value-mapping.title';
            blade.updatePermission = 'search:index:manage';

            blade.refresh = function (setting) {
                blade.isLoading = true;

                indexFieldSettingsApi.search({
                    documentType: blade.currentEntity.documentType,
                }, function (data) {
                    blade.isLoading = false;

                    blade.currentEntities = data.results;
                    $scope.pageSettings.totalItems = data.totalCount;
                });

                if (setting) {
                    $scope.selectedNodeId = setting.id;
                }
            };

            blade.selectNode = function (node) {
                if (!node) {
                    node = {
                        documentType: blade.currentEntity.documentType,
                        fieldName: '',
                        values: [],
                    };
                }

                $scope.selectedNodeId = node.id;

                bladeNavigationService.closeChildrenBlades(blade, function () {
                    var newBlade = {
                        id: 'valueMappingFieldDetails',
                        controller: 'virtoCommerce.searchModule.valueMappingFieldDetailsController',
                        template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/value-mapping-field-details.html',
                        data: node,
                        parentRefresh: blade.refresh,
                    };
                    bladeNavigationService.showBlade(newBlade, blade);
                });
            };

            $scope.deleteList = function (selection) {
                var dialog = {
                    id: 'confirmDeleteItem',
                    title: 'search.dialogs.value-mapping-delete-fields.title',
                    message: 'search.dialogs.value-mapping-delete-fields.message',
                    messageValues: { quantity: selection.length },
                    callback: function (remove) {
                        if (remove) {
                            bladeNavigationService.closeChildrenBlades(blade, function () {
                                var ids = _.pluck(selection, 'id');
                                indexFieldSettingsApi.delete({ ids: ids }, blade.refresh);
                            });
                        }
                    },
                };
                dialogService.showConfirmationDialog(dialog);
            };

            blade.toolbarCommands = [
                {
                    name: 'platform.commands.refresh',
                    icon: 'fa fa-refresh',
                    executeMethod: blade.refresh,
                    canExecuteMethod: function () {
                        return true;
                    },
                },
                {
                    name: 'platform.commands.add',
                    icon: 'fas fa-plus',
                    executeMethod: function () {
                        bladeNavigationService.closeChildrenBlades(blade, blade.selectNode);
                    },
                    canExecuteMethod: function () {
                        return true;
                    },
                    permission: blade.updatePermission,
                },
                {
                    name: 'platform.commands.delete',
                    icon: 'fas fa-trash-alt',
                    executeMethod: function () { $scope.deleteList($scope.gridApi.selection.getSelectedRows()); },
                    canExecuteMethod: function () {
                        return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
                    },
                    permission: blade.updatePermission,
                },
            ];

            const filter = $scope.filter = blade.filter || {};

            filter.criteriaChanged = function () {
                if ($scope.pageSettings.currentPage > 1) {
                    $scope.pageSettings.currentPage = 1;
                }
                else {
                    blade.refresh();
                }
            };

            $scope.gridOptions = {};
            $scope.uiGridConstants = uiGridConstants;

            $scope.setGridOptions = function (gridOptions) {
                uiGridHelper.initialize($scope, gridOptions, function (gridApi) {
                    uiGridHelper.bindRefreshOnSortChanged($scope);
                });
                bladeUtils.initializePagination($scope);
            };
        }
    ]);
