<template>
  <VcBlade
    :title="$t('SEARCH.PAGES.DOCUMENT.TITLE')"
    :subtitle="`${options?.documentType ?? ''} ${options?.documentId ?? ''}`"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcContainer>
      <VcForm class="search-form tw-space-y-4">
        <template v-if="doc">
          <VcField
            :label="$t('SEARCH.PAGES.DOCUMENT.BUILD_DATE')"
            :model-value="built?.toLocaleString()"
          />
          <VcCard :header="$t('SEARCH.PAGES.DOCUMENT.CONTENT')">
            <pre class="search-document">{{ json }}</pre>
          </VcCard>
        </template>
        <div
          v-else-if="!loading"
          class="search-values__empty"
        >
          {{ $t("SEARCH.PAGES.DOCUMENT.NO_INDEX") }}
        </div>
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { notification, useApiClient, useAsync, useBlade, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcCard, VcContainer, VcField, VcForm } from "@vc-shell/framework/ui";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { SearchIndexationModuleClient } from "../../api_client/virtocommerce.search";
import { errorText, indexDate } from "../utils";

defineBlade({
  url: "/search-index-document",
  name: "SearchIndexDocument",
  // the document comes from the widget (options): nothing to restore after a reload
  routable: false,
});

const { t } = useI18n({ useScope: "global" });
const { options, openBlade, exposeToChildren } = useBlade<{ documentType?: string; documentId?: string }>();
const documentType = options.value?.documentType ?? "";
const documentId = options.value?.documentId ?? "";
const { showError } = usePopup();
const { getApiClient } = useApiClient(SearchIndexationModuleClient);

const doc = ref<Record<string, unknown>>();
const built = computed(() => indexDate(doc.value));
const json = computed(() => JSON.stringify(doc.value, null, 2));

const { loading, action: load } = useAsync(async () => {
  const client = await getApiClient();
  const docs = (await client.getDocumentIndex(documentType, documentId)) as Record<string, unknown>[];
  doc.value = docs[0];
});

load();

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "rebuild",
    title: t("SEARCH.PAGES.DOCUMENT.TOOLBAR.REBUILD"),
    icon: "lucide-refresh-ccw",
    permissions: "search:index:rebuild",
    async clickHandler() {
      try {
        const client = await getApiClient();
        const started = await client.indexDocuments([{ documentType, documentIds: [documentId] }]);
        await openBlade({ name: "SearchIndexProgress", options: { notification: started } });
      } catch (e) {
        showError(errorText(e));
      }
    },
  },
  {
    id: "copy",
    title: t("SEARCH.PAGES.DOCUMENT.TOOLBAR.COPY"),
    icon: "lucide-clipboard-copy",
    disabled: computed(() => !doc.value),
    async clickHandler() {
      await navigator.clipboard.writeText(json.value);
      notification.success(t("SEARCH.ALERTS.COPIED"), { timeout: 3000 });
    },
  },
]);

// the progress blade calls this when the document is re-indexed
exposeToChildren({ reload: () => load() });
</script>

<style lang="scss">
.search-document {
  margin: 0;
  padding: 16px;
  max-height: 60vh;
  overflow: auto;
  font-family: ui-monospace, monospace;
  font-size: 12px;
  line-height: 1.5;
  color: var(--neutrals-800);
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
