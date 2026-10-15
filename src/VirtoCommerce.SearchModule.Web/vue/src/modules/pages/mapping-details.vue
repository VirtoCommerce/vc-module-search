<template>
  <VcBlade
    :title="title"
    :subtitle="item.documentType"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcContainer>
      <VcForm class="search-form tw-space-y-4">
        <VcInput
          v-model="item.fieldName"
          :label="$t('SEARCH.PAGES.FIELD.FIELD_NAME')"
          :hint="$t('SEARCH.PAGES.FIELD.FIELD_NAME_HINT')"
          required
        />
        <VcCard :header="$t('SEARCH.PAGES.FIELD.VALUES')">
          <div class="search-values">
            <div
              v-if="!item.values?.length"
              class="search-values__empty"
            >
              {{ $t("SEARCH.PAGES.FIELD.NO_VALUES") }}
            </div>
            <div
              v-for="(value, i) in item.values"
              :key="i"
              class="search-values__row"
            >
              <VcInput
                v-model="value.value"
                class="search-values__value"
                :label="$t('SEARCH.PAGES.FIELD.VALUE')"
                required
              />
              <VcMultivalue
                class="search-values__synonyms"
                :model-value="(value.synonyms ?? []).map((s) => ({ id: s, value: s }))"
                :label="$t('SEARCH.PAGES.FIELD.SYNONYMS')"
                :placeholder="$t('SEARCH.PAGES.FIELD.SYNONYMS_HINT')"
                option-label="value"
                option-value="value"
                @update:model-value="(chips) => (value.synonyms = chips.map((c) => String((c as { value?: string }).value ?? c.id)))"
              />
              <VcButton
                class="search-values__remove"
                icon="lucide-trash-2"
                text
                :aria-label="$t('SEARCH.PAGES.FIELD.REMOVE_VALUE')"
                @click="item.values!.splice(i, 1)"
              />
            </div>
          </div>
        </VcCard>
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { useApiClient, useAsync, useBlade, useBladeForm, useLoading, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcButton, VcCard, VcContainer, VcForm, VcInput, VcMultivalue } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { IndexFieldSettingClient, type IndexFieldSetting, type IndexFieldValueSetting } from "../../api_client/virtocommerce.search";
import { errorText } from "../utils";

defineBlade({
  url: "/search-value-mapping-field",
  name: "SearchValueMappingDetails",
  permissions: ["search:index:manage"],
});

const { t } = useI18n({ useScope: "global" });
const { param, options, callParent, closeSelf } = useBlade<{ documentType: string }>();
const { showConfirmation, showError } = usePopup();
const { getApiClient } = useApiClient(IndexFieldSettingClient);

const item = ref<IndexFieldSetting>({ documentType: options.value?.documentType, fieldName: "", values: [] });
const isNew = computed(() => !item.value.id);
const title = computed(() => (isNew.value ? t("SEARCH.PAGES.FIELD.NEW") : (item.value.fieldName ?? "")));

const valid = computed(() => !!item.value.fieldName?.trim() && (item.value.values ?? []).every((v) => !!v.value?.trim()));
const form = useBladeForm({
  data: item,
  canSaveOverride: valid,
  closeConfirmMessage: computed(() => t("SEARCH.ALERTS.CLOSE_CONFIRMATION")),
});

const lower = (s?: string) => (s ?? "").trim().toLocaleLowerCase();

// The AngularJS value blade's rules, applied on save: one entry per value (case-insensitive), synonyms unique
// and sorted, and a synonym belongs to one value only.
function normalize(values: IndexFieldValueSetting[]) {
  const byValue = new Map<string, IndexFieldValueSetting>();
  const taken = new Set<string>();
  for (const v of values) {
    const key = lower(v.value);
    if (!key) continue;
    const target = byValue.get(key) ?? byValue.set(key, { id: v.id, value: v.value!.trim(), synonyms: [] }).get(key)!;
    for (const s of v.synonyms ?? []) {
      if (!lower(s) || taken.has(lower(s))) continue;
      taken.add(lower(s));
      target.synonyms!.push(s.trim());
    }
  }
  const compare = (a?: string, b?: string) => (a ?? "").localeCompare(b ?? "", undefined, { sensitivity: "base" });
  return [...byValue.values()].map((v) => ({ ...v, synonyms: v.synonyms!.sort(compare) })).sort((a, b) => compare(a.value, b.value));
}

const { loading: itemLoading, action: loadItem } = useAsync<string>(async (id) => {
  const client = await getApiClient();
  item.value = await client.get(id!);
});

// the Save button shows its own error popup
const { loading: saveLoading, action: save } = useAsync(
  async () => {
    const client = await getApiClient();
    item.value = await client.save({ ...item.value, fieldName: item.value.fieldName?.trim(), values: normalize(item.value.values ?? []) });
  },
  { notify: false },
);

const loading = useLoading(itemLoading, saveLoading);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: t("SEARCH.PAGES.FIELD.TOOLBAR.SAVE"),
    icon: "lucide-save",
    permissions: "search:index:manage",
    disabled: computed(() => !form.canSave.value),
    async clickHandler() {
      try {
        await save();
        form.setBaseline();
        await callParent("reload", item.value.id);
      } catch (e) {
        showError(errorText(e));
      }
    },
  },
  {
    id: "reset",
    title: t("SEARCH.PAGES.FIELD.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => form.revert(),
  },
  {
    id: "addValue",
    title: t("SEARCH.PAGES.FIELD.TOOLBAR.ADD_VALUE"),
    icon: "lucide-plus",
    permissions: "search:index:manage",
    disabled: computed(() => (item.value.values ?? []).some((v) => !v.value?.trim())),
    clickHandler: () => (item.value.values = [{ value: "", synonyms: [] }, ...(item.value.values ?? [])]),
  },
  {
    id: "delete",
    title: t("SEARCH.PAGES.FIELD.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: "search:index:manage",
    disabled: isNew,
    async clickHandler() {
      if (!(await showConfirmation(t("SEARCH.ALERTS.DELETE_FIELDS", { count: 1 })))) return;
      try {
        const client = await getApiClient();
        await client.delete([item.value.id!]);
        form.setBaseline();
        await callParent("reload");
        await closeSelf();
      } catch (e) {
        showError(errorText(e));
      }
    },
  },
]);

onMounted(async () => {
  if (param.value) await loadItem(param.value);
  form.setBaseline();
});
</script>

<style lang="scss">
.search-form {
  padding: 20px 22px 36px;
}
.search-values {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 16px;
  &__empty {
    font-size: 14px;
    color: var(--neutrals-500);
  }
  &__row {
    display: grid;
    grid-template-columns: minmax(140px, 1fr) 2fr auto;
    gap: 12px;
    align-items: end;
    padding-bottom: 12px;
    border-bottom: 1px solid var(--neutrals-200);
    &:last-child {
      border-bottom: 0;
      padding-bottom: 0;
    }
  }
  &__remove {
    margin-bottom: 4px;
  }
}
</style>
