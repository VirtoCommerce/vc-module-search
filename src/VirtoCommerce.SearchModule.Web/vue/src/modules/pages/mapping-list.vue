<template>
  <VcBlade
    :title="$t('SEARCH.PAGES.MAPPING.TITLE')"
    :subtitle="param"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      v-model:selection="selection"
      selection-mode="multiple"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      state-key="SEARCH_VALUE_MAPPING"
      :active-item-id="activeItemId"
      :empty-state="{ icon: 'lucide-wrench', title: $t('SEARCH.PAGES.MAPPING.EMPTY') }"
      @row-click="(e: { data: IndexFieldSetting }) => openField(e.data)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="fieldName"
        :title="$t('SEARCH.PAGES.MAPPING.COLUMNS.FIELD')"
        body-class="search-list__type"
        always-visible
      />
      <VcColumn
        id="values"
        :title="$t('SEARCH.PAGES.MAPPING.COLUMNS.VALUES')"
      >
        <template #body="{ data: setting }">{{ (setting.values ?? []).map((v: IndexFieldValueSetting) => v.value).join(", ") }}</template>
      </VcColumn>
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useApiClient, useAsync, useBlade, useDataTablePagination, useFunctions, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { IndexFieldSettingClient, type IndexFieldSetting, type IndexFieldValueSetting } from "../../api_client/virtocommerce.search";
import { errorText } from "../utils";

defineBlade({
  url: "/search-value-mapping",
  name: "SearchValueMappingList",
  permissions: ["search:index:manage"],
});

const { t } = useI18n({ useScope: "global" });
const { param, openBlade, closeChildren, exposeToChildren } = useBlade();
const { showConfirmation, showError } = usePopup();
const { debounce } = useFunctions();
const { getApiClient } = useApiClient(IndexFieldSettingClient);

const PAGE_SIZE = 20;
const items = ref<IndexFieldSetting[]>([]);
const totalCount = ref(0);
const selection = ref<IndexFieldSetting[]>([]);
const activeItemId = ref<string>();
const searchValue = ref<string>();
const pagination = useDataTablePagination({ pageSize: PAGE_SIZE, totalCount });

const { loading, action: load } = useAsync(async () => {
  const client = await getApiClient();
  const result = await client.search({
    documentType: param.value,
    keyword: searchValue.value || undefined,
    skip: pagination.skip,
    take: PAGE_SIZE,
  });
  items.value = result.results ?? [];
  totalCount.value = result.totalCount ?? 0;
});

function openField(setting?: IndexFieldSetting) {
  activeItemId.value = setting?.id;
  openBlade({
    name: "SearchValueMappingDetails",
    param: setting?.id,
    options: { documentType: param.value },
    onClose() {
      activeItemId.value = undefined;
    },
  });
}

watch(searchValue, () => pagination.setPage(1));
watch([searchValue, () => pagination.skip], debounce(load, 300));
load();

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("SEARCH.PAGES.MAPPING.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "add",
    title: t("SEARCH.PAGES.MAPPING.TOOLBAR.ADD"),
    icon: "lucide-plus",
    permissions: "search:index:manage",
    clickHandler: () => openField(),
  },
  {
    id: "delete",
    title: t("SEARCH.PAGES.MAPPING.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: "search:index:manage",
    disabled: computed(() => !selection.value.length),
    async clickHandler() {
      if (!(await showConfirmation(t("SEARCH.ALERTS.DELETE_FIELDS", { count: selection.value.length })))) return;
      await closeChildren();
      try {
        const client = await getApiClient();
        await client.delete(selection.value.map((x) => x.id!));
        selection.value = [];
      } catch (e) {
        showError(errorText(e));
      }
      await load();
    },
  },
]);

exposeToChildren({
  async reload(id?: string) {
    await load();
    if (id) activeItemId.value = id;
  },
});
</script>
