angular.module('virtoCommerce.searchModule')
    .directive('uiSelectEdit', [function () {
        return {
            restrict: 'E',
            templateUrl: 'Modules/$(VirtoCommerce.Search)/Scripts/directives/ui-select-edit.html',
            require: 'ngModel',
            replace: true,
            scope: {
                data: '=',
                disabled: '=?',
                required: '=?',
                placeholder: '=?',
            },
            link: function ($scope, element, attrs, controller) {
                $scope.context = {
                    modelValue: null,
                    required: getBooleanAttributeValue('required'),
                    disabled: getBooleanAttributeValue('disabled'),
                };

                function getBooleanAttributeValue(name) {
                    const value = attrs[name];
                    return angular.isDefined(value) && (value === '' || value === name || value.toLowerCase() === 'true');
                }

                $scope.$watch('context.modelValue', function (newValue, oldValue) {
                    if (newValue !== oldValue) {
                        controller.$setViewValue($scope.context.modelValue);
                    }
                });

                controller.$render = function () {
                    $scope.context.modelValue = controller.$modelValue;
                };
            }
        }
    }]);
