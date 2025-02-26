angular.module('virtoCommerce.searchModule')
    .controller('virtoCommerce.searchModule.valueMappingFieldDetailsController', [
        '$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.searchModule.indexFieldSettingsApi',
        function ($scope, bladeNavigationService, indexFieldSettingsApi) {
            var blade = $scope.blade;
            blade.headIcon = 'fas fa-wrench';
            blade.updatePermission = 'search:index:manage';
            initializeBlade(blade.data);

            const field = {
                name: undefined,
                values: undefined,
            };

            blade.refresh = function (newSetting) {
                $scope.selectedNodeId = undefined;

                if (!newSetting) {
                    loadSettings(blade.currentEntity);
                }
                else {
                    updateSettings(newSetting);
                }
            };

            function loadSettings(setting) {
                if (!setting.fieldName) {
                    blade.isLoading = false;
                }
                else {
                    indexFieldSettingsApi.search({
                        documentType: setting.documentType,
                        fieldName: setting.fieldName,
                    }, function (response) {
                        blade.isLoading = false;

                        if (response.results.length > 0) {
                            initializeBlade(response.results[0]);
                        }
                    });
                }
            }

            function initializeBlade(setting) {
                blade.title = setting.fieldName ? setting.fieldName : 'search.blades.value-mapping.new-field';
                blade.originalEntity = angular.copy(setting);
                blade.currentEntity = angular.copy(blade.originalEntity);
            }

            function updateSettings(newSetting) {
                newSetting.synonyms.sort(compareIgnoreCase);

                // Remove duplicates ignoring case
                newSetting.synonyms = _.uniq(newSetting.synonyms, true, function (x) {
                    return x.toLowerCase();
                });

                removeDuplicateSynonymsFromExistingValues(newSetting);

                if (!updateSettingsById(newSetting)) {
                    updateSettingsByValue(newSetting);
                }
            }

            function removeDuplicateSynonymsFromExistingValues(newSetting) {
                if (!newSetting.synonyms.length) {
                    return;
                }

                blade.currentEntity.values.forEach(function (existingSetting) {
                    existingSetting.synonyms = existingSetting.synonyms.filter(function (existingSynonym) {
                        return !newSetting.synonyms.some(function (newSynonym) {
                            return equalsIgnoreCase(newSynonym, existingSynonym);
                        });
                    });
                });
            }

            function updateSettingsById(newSetting) {
                if (!newSetting.id) {
                    return false;
                }

                const existingSetting = blade.currentEntity.values.find(x => equalsIgnoreCase(x.id, newSetting.id));

                if (!existingSetting) {
                    return false;
                }

                if (!equalsIgnoreCase(existingSetting.value, newSetting.value)) {
                    // If value has changed, remove existing setting and update by value
                    blade.currentEntity.values = blade.currentEntity.values.filter(x => !equalsIgnoreCase(x.id, newSetting.id));
                    return false;
                }

                existingSetting.value = newSetting.value;
                existingSetting.synonyms = newSetting.synonyms;

                return true;
            }

            function updateSettingsByValue(newSetting) {
                const existingSetting = blade.currentEntity.values.find(x => equalsIgnoreCase(x.value, newSetting.value));

                if (existingSetting) {
                    // Remove existing setting and add new one
                    blade.currentEntity.values = blade.currentEntity.values.filter(x => !equalsIgnoreCase(x.value, newSetting.value));
                }

                blade.currentEntity.values.push(newSetting);
                blade.currentEntity.values.sort((a, b) => compareIgnoreCase(a.value, b.value));
            }

            blade.onClose = function (closeCallback) {
                bladeNavigationService.showConfirmationIfNeeded(isDirty(), true, blade, $scope.saveChanges, closeCallback,
                    "search.dialogs.value-mapping-save-values.title", "search.dialogs.value-mapping-save-values.message");
            };

            blade.selectNode = function (node) {
                if (!node) {
                    node = {
                        id: '',
                        value: '',
                        synonyms: [],
                    };
                }

                $scope.selectedNodeId = node.id;

                // Load field values if not loaded yet
                if (field.name !== blade.currentEntity.fieldName || !field.values) {
                    indexFieldSettingsApi.getFieldValues({
                        documentType: blade.currentEntity.documentType,
                        fieldName: blade.currentEntity.fieldName
                    }, function (response) {
                        field.name = blade.currentEntity.fieldName;
                        field.values = response;
                    });
                }

                bladeNavigationService.closeChildrenBlades(blade, function () {
                    var newBlade = {
                        id: 'valueMappingValueDetails',
                        controller: 'virtoCommerce.searchModule.valueMappingValueDetailsController',
                        template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/value-mapping-value-details.html',
                        data: node,
                        field: field,
                        parentRefresh: blade.refresh,
                    };
                    bladeNavigationService.showBlade(newBlade, blade);
                });
            };

            $scope.saveChanges = function () {
                blade.isLoading = true;

                indexFieldSettingsApi.save({}, blade.currentEntity, function (response) {
                    blade.isLoading = false;

                    blade.refresh();

                    if (blade.parentRefresh) {
                        blade.parentRefresh(response);
                    }
                });
            };

            $scope.toggleAll = function () {
                blade.currentEntity.values.forEach(x => x.$selected = blade.selectedAll);
            };

            function equalsIgnoreCase(a, b) {
                return compareIgnoreCase(a, b) === 0;
            }

            function compareIgnoreCase(a, b) {
                return a.localeCompare(b, undefined, { sensitivity: 'base' });
            }

            function deleteSelected() {
                blade.currentEntity.values = blade.currentEntity.values.filter(x => !x.$selected);
                blade.selectedAll = false;
            }

            function canDelete() {
                return blade.currentEntity.values.some(x => x.$selected);
            }

            function isDirty() {
                return !angular.equals(blade.currentEntity, blade.originalEntity) && blade.hasUpdatePermission();
            }

            blade.toolbarCommands = [
                {
                    name: "platform.commands.save",
                    icon: 'fas fa-save',
                    executeMethod: $scope.saveChanges,
                    canExecuteMethod: isDirty,
                    permission: blade.updatePermission,
                },
                {
                    name: "platform.commands.reset",
                    icon: 'fa fa-undo',
                    executeMethod: function () {
                        angular.copy(blade.originalEntity, blade.currentEntity);
                    },
                    canExecuteMethod: isDirty,
                    permission: blade.updatePermission,
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
                    executeMethod: deleteSelected,
                    canExecuteMethod: canDelete,
                    permission: blade.updatePermission,
                },
            ];

            blade.refresh();
        }
    ]);
