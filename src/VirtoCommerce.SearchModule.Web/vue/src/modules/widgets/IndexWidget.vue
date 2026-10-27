<template>
  <VcWidget
    v-if="canRead"
    :icon="icon"
    :title="label"
    :class="{ 'search-index-widget--warn': state !== 'ok' && state !== 'loading' }"
    :disabled="!documentId"
    @click="open"
  />
</template>

<script setup lang="ts">
// Index state of the entity on a details blade of another module (AngularJS indexWidgetController)
import { injectBladeContext, useApiClient, useBlade, usePermissions } from "@vc-shell/framework";
import { VcWidget } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { SearchIndexationModuleClient } from "../../api_client/virtocommerce.search";
import { indexDate } from "../utils";

const props = defineProps<{ documentType: string }>();

const { t } = useI18n({ useScope: "global" });
const { hasAccess } = usePermissions();
const { openBlade } = useBlade();
const { getApiClient } = useApiClient(SearchIndexationModuleClient);
const ctx = injectBladeContext();

const canRead = computed(() => hasAccess("search:index:read"));
const entity = computed(() => ctx.value.item as { id?: string; modifiedDate?: Date | string } | undefined);
const documentId = computed(() => entity.value?.id);
const doc = ref<Record<string, unknown>>();
const loading = ref(true);

async function refresh() {
  if (!documentId.value || !canRead.value) return;
  loading.value = true;
  try {
    const client = await getApiClient();
    doc.value = ((await client.getDocumentIndex(props.documentType, documentId.value)) as Record<string, unknown>[])[0];
  } catch {
    doc.value = undefined;
  } finally {
    loading.value = false;
  }
}

const state = computed(() => {
  if (loading.value) return "loading";
  if (!doc.value) return "none";
  const built = indexDate(doc.value);
  const modified = entity.value?.modifiedDate ? new Date(entity.value.modifiedDate) : undefined;
  return built && modified && built < modified ? "old" : "ok";
});
const label = computed(() => t({ loading: "SEARCH.WIDGET.LOADING", ok: "SEARCH.WIDGET.OK", old: "SEARCH.WIDGET.OLD", none: "SEARCH.WIDGET.NONE" }[state.value]));
const icon = computed(() => ({ loading: "lucide-file-search", ok: "lucide-file-check", old: "lucide-file-clock", none: "lucide-file-x" })[state.value]);

// sources compared one by one: the host replacing the item with the same id and date doesn't refetch
watch([documentId, () => String(entity.value?.modifiedDate ?? "")], refresh, { immediate: true });

function open() {
  openBlade({
    name: "SearchIndexDocument",
    options: { documentType: props.documentType, documentId: documentId.value },
    onClose: refresh,
  });
}
</script>

<style lang="scss">
.search-index-widget--warn .vc-widget__icon {
  color: var(--danger-500);
}
</style>
