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
                    required: angular.isDefined(attrs.required) && (attrs.required === '' || attrs.required.toLowerCase() === 'true'),
                    disabled: angular.isDefined(attrs.disabled) && (attrs.disabled === '' || attrs.disabled.toLowerCase() === 'true'),
                };

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
