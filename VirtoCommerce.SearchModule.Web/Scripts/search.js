var moduleName = "virtoCommerce.searchModule";

if (AppDependencies != undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [
    'ngSanitize'
])
.run(
  ['platformWebApp.toolbarService', 'platformWebApp.pushNotificationTemplateResolver', 'platformWebApp.bladeNavigationService',  'platformWebApp.widgetService', 'virtoCommerce.searchModule.search', function (toolbarService, pushNotificationTemplateResolver, bladeNavigationService, widgetService, searchAPI) {
     
      // register notification template
      pushNotificationTemplateResolver.register({
          priority: 900,
          satisfy: function (notify, place) { return place == 'history' && notify.notifyType == 'IndexProgressPushNotification'; },
          template: '$(Platform)/Scripts/app/pushNotifications/blade/historyDefault.tpl.html',
          action: function (notify) {
              var blade = {
                  id: 'indexProgress',
                  notification: notify,
                  controller: 'virtoCommerce.searchModule.indexProgressController',
                  template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/index-progress.tpl.html'
              };
              bladeNavigationService.showBlade(blade);
          }
      });

      var indexCommand = {
          name: "search.commands.rebuild-index",
          icon: 'fa fa-recycle',
          index: 2,
          executeMethod: function (blade) {

              var apiToCall = searchAPI.index;
              var documentsIds = blade.currentEntityId ? [{ id: blade.currentEntityId }] : undefined;
              apiToCall({ documentType: blade.documentType }, documentsIds,
                      function openProgressBlade(data) {
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
          },
          canExecuteMethod: function () { return true; },
          permission: 'VirtoCommerce.Search:Index:Rebuild'
      };
      
      // register in index details
      toolbarService.register(indexCommand, 'virtoCommerce.searchModule.indexDetailController');
    
  }]
);
