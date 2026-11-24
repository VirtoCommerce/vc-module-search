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
      :items="visibleItems"
      :is-row-selectable="(x: Row) => !!x.isActive"
      search-bar
      :searchable="false"
      state-key="SEARCH_INDEXES"
      :global-filters="globalFilters"
      :unfiltered-count="items.length"
      :active-item-id="activeItemId"
      :show-all-columns="!activeItemId"
      :row-actions="rowActions"
      :empty-state="{ icon: 'lucide-search', title: $t('SEARCH.PAGES.LIST.EMPTY') }"
      @row-click="(e: { data: Row }) => openMapping(e.data)"
      @filter="onFilter"
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
  type GlobalFilterConfig,
  type IBladeToolbar,
  type TableAction,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
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
const swapSupported = ref(false);

// The API lists every document type's index state with no filters and no keyword search, so the search bar
// narrows the rows it returned: what a platform admin looks for is a type never built or not built for a while.
type Filters = { neverBuilt?: boolean; builtFrom?: string; builtTo?: string; backups?: boolean };
const filters = ref<Filters>({});
// backup indices exist only for providers that swap (not Lucene); a backups value restored from the URL waits for that
const showBackups = computed(() => swapSupported.value && !!filters.value.backups);

// vc-shell 2.13 types global-filters as GlobalFilterConfig[], which lacks the search bar's toggle field
const globalFilters = computed(
  () =>
    [
      { id: "neverBuilt", label: t("SEARCH.PAGES.LIST.FILTERS.NEVER_BUILT"), filter: { toggle: true } },
      { id: "lastBuild", label: t("SEARCH.PAGES.LIST.COLUMNS.LAST_BUILD"), filter: { range: ["builtFrom", "builtTo"] } },
      swapSupported.value && { id: "backups", label: t("SEARCH.PAGES.LIST.FILTERS.BACKUPS"), filter: { toggle: true } },
    ].filter(Boolean) as unknown as GlobalFilterConfig[],
);

// the bar sends days (YYYY-MM-DD, both ends included), compared with the local day of the last build
const localYmd = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

// both set read "built in the range, or never": a range ending a month ago plus Never built lists the stale types
function matches(row: Row) {
  const { neverBuilt, builtFrom, builtTo } = filters.value;
  const ranged = !!(builtFrom || builtTo);
  if (!neverBuilt && !ranged) return true;
  if (!row.lastIndexationDate) return !!neverBuilt;
  if (!ranged) return false;
  const day = localYmd(new Date(row.lastIndexationDate));
  return (!builtFrom || day >= builtFrom) && (!builtTo || day <= builtTo);
}

const visibleItems = computed(() => items.value.filter(matches));

function onFilter(event: { filters: Record<string, unknown> }) {
  const text = (v: unknown) => (typeof v === "string" && v ? v : undefined);
  filters.value = {
    neverBuilt: event.filters.neverBuilt === true,
    builtFrom: text(event.filters.builtFrom),
    builtTo: text(event.filters.builtTo),
    backups: event.filters.backups === true,
  };
  // the toolbar builds the selection: keep only the rows still shown
  selection.value = selection.value.filter(matches);
}

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
// backup indices come from another API call
watch(showBackups, () => load());

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
]);

exposeToChildren({ reload: () => load() });
</script>

<style lang="scss">
.search-list__type {
  color: var(--neutrals-900);
}
</style>
