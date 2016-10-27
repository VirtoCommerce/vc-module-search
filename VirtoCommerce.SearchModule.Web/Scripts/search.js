var moduleName = "virtoCommerce.searchModule";

if (AppDependencies != undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [
    'ngSanitize'
])
.run(
  ['platformWebApp.toolbarService', 'platformWebApp.pushNotificationTemplateResolver', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.widgetService', function (toolbarService, pushNotificationTemplateResolver, bladeNavigationService, dialogService, widgetService) {
      // register WIDGETS
      var indexWidget  = {
          controller: 'virtoCommerce.searchModule.indexWidgetController',
          // size: [3, 1],
          template: 'Modules/$(VirtoCommerce.Search)/Scripts/widgets/common/index-widget.tpl.html'
      };
      // integration: index in product details
      var widgetToRegister = angular.extend({}, indexWidget, { documentType: 'catalogitem' })
      widgetService.registerWidget(widgetToRegister, 'itemDetail');
      // integration: index in CATEGORY details
      widgetToRegister = angular.extend({}, indexWidget, { documentType: 'catalogcategory' })
      widgetService.registerWidget(widgetToRegister, 'categoryDetail');
      // integration: index in catalog details
      widgetToRegister = angular.extend({}, indexWidget, { documentType: 'catalog' })
      widgetService.registerWidget(widgetToRegister, 'catalogDetail');

      // register notification template
      pushNotificationTemplateResolver.register({
          priority: 900,
          satisfy: function (notify, place) { return place == 'history' && notify.notifyType == 'IndexProgressPushNotification'; },
          template: '$(Platform)/Scripts/app/pushNotifications/blade/historyDefault.tpl.html',
          action: function (notify) {
              var blade = {
                  id: 'indexProgress',
                  currentEntity: notify,
                  controller: 'virtoCommerce.searchModule.indexProgressController',
                  template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/index-progress.tpl.html'
              };
              bladeNavigationService.showBlade(blade);
          }
      });

      // toolbar button in catalogs list
      var rebuildIndexCommand = {
          name: "search.commands.rebuild-index",
          icon: 'fa fa-recycle',
          index: 4,
          executeMethod: function (blade) {
              var dialog = {
                  id: "confirmRebuildIndex",
                  title: "search.dialogs.rebuild-index.title",
                  message: "search.dialogs.rebuild-index.message",
                  callback: function (confirm) {
                      if (confirm) {
                          var newBlade = {
                              id: 'rebuildIndex',
                              controller: 'virtoCommerce.searchModule.rebuildIndexController',
                              template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/rebuildIndex.tpl.html'
                          };
                          bladeNavigationService.showBlade(newBlade, blade);
                      }
                  }
              }
              dialogService.showConfirmationDialog(dialog);
          },
          canExecuteMethod: function () { return true; },
          permission: 'VirtoCommerce.Search:Index:Rebuild'
      };
      toolbarService.register(rebuildIndexCommand, 'virtoCommerce.catalogModule.catalogsListController');
  }]
);
