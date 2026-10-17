import * as blades from "./pages";
import * as locales from "./locales";
import { defineAppModule } from "@vc-shell/framework";
import type { App, Component } from "vue";
import IndexWidget from "./widgets/IndexWidget.vue";

/** The part of the Admin Shell's shared widgets this module uses (vc-module-admin-shell, docs/blade-widgets.md) */
interface SharedWidgets {
  provide(id: string, component: Component): void;
}

const appModule = defineAppModule({
  blades,
  locales,
});

export default {
  install(app: App) {
    appModule.install(app);
    // The "Index" widget (AngularJS indexWidgetController): the modules whose entities are indexed (Catalog, Customer,
    // Orders) depend on Search and place it on their own details blades with a documentType prop, as they did in
    // AngularJS. Search never registers widgets on their blades.
    (app.config.globalProperties.$sharedWidgets as SharedWidgets | undefined)?.provide("search.index", IndexWidget);
  },
};
