<template>
  <VcBlade
    :title="job?.title ?? $t('SEARCH.PAGES.PROGRESS.TITLE')"
    :toolbar-items="bladeToolbar"
    width="50%"
  >
    <VcContainer>
      <JobProgress
        :job="job"
        :finished="finished"
        :percent="percent"
        prefix="SEARCH"
      />
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { useApiClient, useBlade, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcContainer } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { SearchIndexationModuleClient, type IndexProgressPushNotification } from "../../api_client/virtocommerce.search";
import JobProgress from "../components/JobProgress.vue";
import { useJob, type JobNotification } from "../composables/useJob";
import { errorText } from "../utils";

defineBlade({
  url: "/search-index-progress",
  name: "SearchIndexProgress",
});

// IndexProgressPushNotification is one of the types the shell's notification store skips
// (EXCLUDED_NOTIFICATION_TYPES), so this job is followed through useJob's notification API poll.
const { t } = useI18n({ useScope: "global" });
const { options, callParent } = useBlade<{ notification: IndexProgressPushNotification }>();
const { showError } = usePopup();
const { getApiClient } = useApiClient(SearchIndexationModuleClient);
const { job, finished, percent, follow } = useJob();

if (options.value?.notification) follow(options.value.notification as JobNotification);

const cancelling = ref(false);

// the list's counts and build dates change when the job ends
watch(finished, (done) => done && callParent("reload"));

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "cancel",
    title: t("SEARCH.PAGES.PROGRESS.TOOLBAR.CANCEL"),
    icon: "lucide-circle-x",
    permissions: "search:index:rebuild",
    disabled: computed(() => finished.value || cancelling.value || !job.value?.id),
    async clickHandler() {
      cancelling.value = true;
      try {
        const client = await getApiClient();
        await client.cancelIndexationProcess(job.value!.id!);
      } catch (e) {
        cancelling.value = false;
        showError(errorText(e));
      }
    },
  },
]);
</script>
