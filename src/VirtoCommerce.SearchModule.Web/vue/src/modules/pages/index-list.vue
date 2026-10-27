<template>
  <VcBlade
    :title="$t('SEARCH.MENU.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      v-model:selection="selection"
      selection-mode="multiple"
      :items="items"
      :is-row-selectable="(x: Row) => !!x.isActive"
      state-key="SEARCH_INDEXES"
      :active-item-id="activeItemId"
      :show-all-columns="!activeItemId"
      :row-actions="rowActions"
      :empty-state="{ icon: 'lucide-search', title: $t('SEARCH.PAGES.LIST.EMPTY') }"
      @row-click="(e: { data: Row }) => openMapping(e.data)"
    >
      <VcColumn
        id="documentType"
        :title="$t('SEARCH.PAGES.LIST.COLUMNS.TYPE')"
        body-class="search-list__type"
        always-visible
      />
      <VcColumn
        v-if="showBackups"
        id="isActive"
        :title="$t('SEARCH.PAGES.LIST.COLUMNS.STATE')"
      >
        <template #body="{ data: row }">
          <div class="tw-inline-flex">
            <VcStatus :variant="row.isActive ? 'success' : 'info'">
              {{ row.isActive ? $t("SEARCH.PAGES.LIST.ACTIVE") : $t("SEARCH.PAGES.LIST.BACKUP") }}
            </VcStatus>
          </div>
        </template>
      </VcColumn>
      <VcColumn
        id="lastIndexationDate"
        :title="$t('SEARCH.PAGES.LIST.COLUMNS.LAST_BUILD')"
        always-visible
      >
        <!-- relative, like the shell's other "ago" dates (the AngularJS list's am-time-ago); N/A when never built -->
        <template #body="{ data: row }">{{ row.lastIndexationDate ? formatDateRelative(row.lastIndexationDate) : $t("SEARCH.PAGES.LIST.NA") }}</template>
      </VcColumn>
      <VcColumn
        id="indexedDocumentsCount"
        :title="$t('SEARCH.PAGES.LIST.COLUMNS.COUNT')"
        align="end"
      >
        <template #body="{ data: row }">{{ row.indexedDocumentsCount ?? $t("SEARCH.PAGES.LIST.NA") }}</template>
      </VcColumn>
      <VcColumn
        id="provider"
        :title="$t('SEARCH.PAGES.LIST.COLUMNS.PROVIDER')"
      />
      <VcColumn
        id="scope"
        :title="$t('SEARCH.PAGES.LIST.COLUMNS.SCOPE')"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  formatDateRelative,
  useApiClient,
  useAsync,
  useBlade,
  usePermissions,
  usePopup,
  type IBladeToolbar,
  type TableAction,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { SearchIndexationModuleClient, type IndexState } from "../../api_client/virtocommerce.search";
import { useChoice } from "../composables/useChoice";
import { errorText } from "../utils";

defineBlade({
  url: "/search-index",
  name: "SearchIndexList",
  isWorkspace: true,
  menuItem: {
    title: "SEARCH.MENU.TITLE",
    icon: "lucide-search",
    priority: 60,
  },
});

type Row = IndexState & { id: string; canSwap?: boolean };

const { t } = useI18n({ useScope: "global" });
const { openBlade, exposeToChildren } = useBlade();
const { showConfirmation, showError } = usePopup();
const { hasAccess } = usePermissions();
const { ask } = useChoice();
const { getApiClient } = useApiClient(SearchIndexationModuleClient);

const items = ref<Row[]>([]);
const selection = ref<Row[]>([]);
const activeItemId = ref<string>();
const showBackups = ref(false);
const swapSupported = ref(false);

const { loading, action: load } = useAsync(async () => {
  const client = await getApiClient();
  const states = showBackups.value ? await client.getAllIndices() : await client.getIndices();
  items.value = (states ?? []).map((x) => {
    // swap only when every index of the type has documents, or a backup has (recovery), as the AngularJS blade
    const same = states.filter((s) => s.documentType === x.documentType);
    const canSwap = same.every((s) => s.indexedDocumentsCount) || same.some((s) => !s.isActive && s.indexedDocumentsCount);
    return { ...x, id: `${x.documentType}|${x.isActive ? "active" : "backup"}`, canSwap };
  });
  selection.value = [];
});

function openMapping(row: Row) {
  if (!hasAccess("search:index:manage")) return; // the mapping blades need it (row click reaches here too)
  activeItemId.value = row.id;
  openBlade({
    name: "SearchValueMappingList",
    param: row.documentType,
    onClose() {
      activeItemId.value = undefined;
    },
  });
}

async function build(rows: Row[]) {
  const types = [...new Set(rows.map((x) => x.documentType!))];
  const choice = await ask({
    title: t("SEARCH.BUILD.TITLE"),
    message: t("SEARCH.BUILD.MESSAGE", { types: types.join(", ") }),
    notes: [t("SEARCH.BUILD.NOTE_INDEX"), t("SEARCH.BUILD.NOTE_REBUILD")],
    choices: [
      { value: "index", label: t("SEARCH.BUILD.INDEX") },
      { value: "rebuild", label: t("SEARCH.BUILD.REBUILD") },
    ],
    cancelLabel: t("SEARCH.BUILD.CANCEL"),
  });
  if (!choice) return;
  try {
    const client = await getApiClient();
    const notification = await client.indexDocuments(types.map((documentType) => ({ documentType, deleteExistingIndex: choice === "rebuild" })));
    await openBlade({ name: "SearchIndexProgress", options: { notification } });
  } catch (e) {
    showError(errorText(e));
  }
}

async function swap(row: Row) {
  if (!(await showConfirmation(t("SEARCH.ALERTS.SWAP", { type: row.documentType })))) return;
  try {
    const client = await getApiClient();
    await client.swapIndex({ documentType: row.documentType });
  } catch (e) {
    showError(errorText(e));
  }
  await load();
}

const rowActions = (row: Row): TableAction[] => [
  {
    id: "build",
    icon: "lucide-refresh-ccw",
    title: t("SEARCH.PAGES.LIST.ACTIONS.BUILD"),
    type: "info",
    hidden: !row.isActive || !hasAccess("search:index:rebuild"),
    clickHandler: () => build([row]),
  },
  {
    id: "mapping",
    icon: "lucide-wrench",
    title: t("SEARCH.PAGES.LIST.ACTIONS.MAPPING"),
    type: "info",
    hidden: !hasAccess("search:index:manage"),
    clickHandler: () => openMapping(row),
  },
  {
    id: "swap",
    icon: "lucide-arrow-left-right",
    title: t("SEARCH.PAGES.LIST.ACTIONS.SWAP"),
    type: "info",
    hidden: !showBackups.value || !row.canSwap || !hasAccess("search:index:rebuild"),
    clickHandler: () => swap(row),
  },
];

// swap/backup indices exist only for providers that support them (not Lucene)
fetch("/api/search/indexes/swapIndexSupported")
  .then((r) => r.json())
  .then((r: { result?: boolean }) => (swapSupported.value = !!r.result))
  .catch(() => undefined);
load();

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("SEARCH.PAGES.LIST.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "build",
    title: t("SEARCH.PAGES.LIST.TOOLBAR.BUILD"),
    icon: "lucide-refresh-ccw",
    permissions: "search:index:rebuild",
    disabled: computed(() => !selection.value.length),
    clickHandler: () => build(selection.value),
  },
  {
    id: "backups",
    title: computed(() => t(showBackups.value ? "SEARCH.PAGES.LIST.TOOLBAR.HIDE_BACKUP" : "SEARCH.PAGES.LIST.TOOLBAR.SHOW_BACKUP")),
    icon: () => (showBackups.value ? "lucide-eye-off" : "lucide-eye"),
    isVisible: swapSupported,
    clickHandler: () => {
      showBackups.value = !showBackups.value;
      load();
    },
  },
]);

exposeToChildren({ reload: () => load() });
</script>

<style lang="scss">
.search-list__type {
  color: var(--neutrals-900);
}
</style>
