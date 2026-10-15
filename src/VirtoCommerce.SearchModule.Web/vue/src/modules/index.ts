import * as blades from "./pages";
import * as locales from "./locales";
import { defineAppModule, registerExternalWidget } from "@vc-shell/framework";
import { defineComponent, h, markRaw } from "vue";
import IndexWidget from "./widgets/IndexWidget.vue";

// The "Index" widget the Catalog, Customer and Orders modules put on their details blades (AngularJS:
// virtoCommerce.searchModule.indexWidgetController with a documentType), registered here by blade name.
const targets: [string, string][] = [
  ["CatalogProductDetails", "Product"],
  ["CatalogCategoryDetails", "Category"],
  ["CustomerContactDetails", "Member"],
  ["CustomerOrganizationDetails", "Member"],
  ["OrdersDetails", "CustomerOrder"],
];
for (const [blade, documentType] of targets) {
  registerExternalWidget({
    id: `SearchIndex${blade}`,
    title: "Index",
    component: markRaw(defineComponent({ name: `SearchIndexWidget${documentType}`, setup: () => () => h(IndexWidget, { documentType }) })),
    targetBlades: [blade],
    // saved entities only (a new one has no index document; AngularJS showed none there either)
    isVisible: (b) => !!b?.param,
  });
}

export default defineAppModule({
  blades,
  locales,
});
