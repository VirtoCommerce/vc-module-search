angular.module('virtoCommerce.searchModule')
    .controller('virtoCommerce.searchModule.indexesListController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'virtoCommerce.searchModule.searchIndexation', 'platformWebApp.ui-grid.extension',
        function ($scope, bladeNavigationService, dialogService, searchIndexationApi, gridOptionExtension) {
            var blade = $scope.blade;
            blade.isLoading = false;

            blade.manageIndexMode = false;

            blade.showBackupIndicesCommand = {
                name: 'Show backup indices',
                icon: 'fas fa-eye',
                canExecuteMethod: function () {
                    return true;
                },
                executeMethod: function () {
                    blade.manageIndexMode = !blade.manageIndexMode;
                    blade.refresh();

                    if (blade.manageIndexMode) {
                        blade.showBackupIndicesCommand.name = 'search.commands.hide-backup-indices';
                        blade.showBackupIndicesCommand.icon = 'fas fa-eye-slash';
                    }
                    else {
                        blade.showBackupIndicesCommand.name = 'search.commands.show-backup-indices';
                        blade.showBackupIndicesCommand.icon = 'fas fa-eye';
                    }
                }
            };

            searchIndexationApi.swapIndexSupported({}, function (response) {
                blade.toolbarCommands = [{
                    name: 'platform.commands.refresh',
                    icon: 'fa fa-refresh',
                    canExecuteMethod: function () {
                        return true;
                    },
                    executeMethod: function () {
                        blade.refresh();
                    }
                }, {
                    name: 'search.commands.rebuild-index',
                    icon: 'fa fa-recycle',
                    canExecuteMethod: function () {
                        return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
                    },
                    executeMethod: function () {
                        $scope.rebuildIndex($scope.gridApi.selection.getSelectedRows());
                    },
                    permission: 'search:index:rebuild'
                }];

                if (response.result) {
                    blade.toolbarCommands.push(blade.showBackupIndicesCommand);
                }
            });

            blade.refresh = function () {
                blade.isLoading = true;

                var getIndicesTask;
                if (blade.manageIndexMode) {
                    getIndicesTask = searchIndexationApi.getAll;
                }
                else {
                    getIndicesTask = searchIndexationApi.get;
                }

                getIndicesTask({}, function (response) {
                    blade.currentEntities = response;

                    if (blade.manageIndexMode) {
                        var documentTypesGroup = _.groupBy(blade.currentEntities, function (x) { return x.documentType; });
                        _.each(documentTypesGroup, function (typedDocs) {
                            _.each(typedDocs, function (doc) {
                                doc.canSwap = typedDocs.every(x => x.indexedDocumentsCount);
                            });
                        });
                    }
                    blade.isLoading = false;
                });
            }

            $scope.rebuildIndex = function (documentTypes) {
                // index only one documentType in collection
                var documentTypesGroup = _.groupBy(documentTypes, function (x) { return x.documentType; });

                var dialog = {
                    id: 'confirmRebuildIndex',
                    callback: function (doReindex) {
                        var options = _.map(documentTypesGroup, function (x) {
                            var documentType = _.first(x);
                            return {
                                documentType: documentType.documentType,
                                deleteExistingIndex: doReindex
                            };
                        });
                        searchIndexationApi.index(options, function openProgressBlade(data) {
                            // show indexing progress
                            var newBlade = {
                                id: 'indexProgress',
                                notification: data,
                                parentRefresh: blade.parentRefresh,
                                controller: 'virtoCommerce.searchModule.indexProgressController',
                                template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/index-progress.tpl.html'
                            };
                            bladeNavigationService.showBlade(newBlade, blade.parentBlade || blade);
                        });
                    }
                }
                dialogService.showDialog(dialog, 'Modules/$(VirtoCommerce.Search)/Scripts/dialogs/reindex-dialog.tpl.html', 'platformWebApp.confirmDialogController');
            }

            $scope.swapIndex = function (documentType) {
                $scope.loading = true;

                searchIndexationApi.swapIndex({ documentType: documentType }, function (response) {
                    blade.refresh();
                });
            }

            $scope.openValueMapping = function (entity) {
                $scope.selectedNodeId = entity.documentType;
                var newBlade = {
                    id: 'valueMappingFieldList',
                    controller: 'virtoCommerce.searchModule.valueMappingFieldListController',
                    template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/value-mapping-field-list.html',
                    currentEntity: entity,
                };
                bladeNavigationService.showBlade(newBlade, blade);
            };

            // ui-grid
            $scope.setGridOptions = function (gridId, gridOptions) {
                gridOptions.isRowSelectable = (row) => row.entity.isActive;
                $scope.gridOptions = gridOptions;
                gridOptionExtension.tryExtendGridOptions(gridId, gridOptions);
                return gridOptions;
            };
            blade.refresh();
        }
    ]);
