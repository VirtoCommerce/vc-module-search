angular.module('virtoCommerce.searchModule')
    .controller('virtoCommerce.searchModule.valueMappingValueDetailsController', [
        '$scope', 'platformWebApp.bladeNavigationService',
        function ($scope, bladeNavigationService) {
            var blade = $scope.blade;
            blade.headIcon = 'fas fa-wrench';
            blade.title = blade.data.value ? blade.data.value : 'search.blades.value-mapping.new-value';
            blade.updatePermission = 'search:index:update';

            function initializeBlade() {
                blade.originalEntity = {};
                blade.originalEntity.id = blade.data.id;
                blade.originalEntity.value = blade.data.value;
                blade.originalEntity.values = blade.data.synonyms.map(x => { return { value: x } });

                blade.currentEntity = angular.copy(blade.originalEntity);
                blade.isLoading = false;
            }

            blade.refresh = function () {
                blade.isLoading = false;
            };

            blade.onClose = function (closeCallback) {
                bladeNavigationService.showConfirmationIfNeeded(isDirty(), true, blade, $scope.saveChanges, closeCallback,
                    "search.dialogs.value-mapping-save-values.title", "search.dialogs.value-mapping-save-values.message");
            };

            $scope.toggleAll = function () {
                blade.currentEntity.values.forEach(x => x.$selected = blade.selectedAll);
            };

            $scope.saveChanges = function () {
                angular.copy(blade.currentEntity, blade.originalEntity);

                const data = {};
                data.id = blade.currentEntity.id;
                data.value = blade.currentEntity.value;
                data.synonyms = blade.currentEntity.values.map(x => x.value).filter(x => !!x);

                blade.parentRefresh(data);
                $scope.bladeClose();
            };

            $scope.cancelChanges = function () {
                $scope.bladeClose();
            };

            function addValue() {
                blade.currentEntity.values.splice(0, 0, { value: '' });
            }

            function canAdd() {
                // Can add new value only if there is no empty value
                return blade.currentEntity.values.every(x => x.value);
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
                    name: 'platform.commands.reset',
                    icon: 'fa fa-undo',
                    executeMethod: function () {
                        angular.copy(blade.originalEntity, blade.currentEntity);
                    },
                    canExecuteMethod: isDirty,
                    permission: blade.updatePermission
                },
                {
                    name: 'platform.commands.add',
                    icon: 'fas fa-plus',
                    executeMethod: addValue,
                    canExecuteMethod: canAdd,
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

            initializeBlade();
        }
    ]);
