<template>
	<page-container>
		<!-- Download Toolbar -->
		<download-bar
			:has-selected="hasSelected"
			@pause="pauseDownloadTasks(getSelected)"
			@stop="stopDownloadTasks(getSelected)"
			@restart="restartDownloadTasks(getSelected)"
			@start="startDownloadTasks(getSelected)"
			@clear="clearDownloadTasks(getSelected)"
			@delete="deleteDownloadTasks(getSelected)"
		/>
		<!--	The Download Table	-->
		<vue-scroll class="download-page-tables">
			<v-row v-if="downloads.length > 0">
				<v-col>
					<v-expansion-panels v-model="openExpansions" multiple>
						<v-expansion-panel v-for="plexServer in getServersWithDownloads" :key="plexServer.id">
							<v-expansion-panel-header>
								<h2>{{ plexServer.name }}</h2>
							</v-expansion-panel-header>
							<v-expansion-panel-content>
								<downloads-table
									v-model="selected"
									:server-id="plexServer.id"
									@action="commandSwitch"
									@selected="updateSelected(plexServer.id, $event)"
								/>
							</v-expansion-panel-content>
						</v-expansion-panel>
					</v-expansion-panels>
				</v-col>
			</v-row>
			<v-row v-else justify="center">
				<v-col cols="auto">
					<h2>{{ $t('pages.downloads.no-downloads') }}</h2>
				</v-col>
			</v-row>
		</vue-scroll>
		<download-details-dialog :download-task="downloadTaskDetail" :dialog="dialog" @close="closeDetailsDialog" />
	</page-container>
</template>

<script lang="ts">
import Log from 'consola';
import { Component, Vue } from 'vue-property-decorator';
import { DownloadService, ServerService } from '@service';
import { DownloadTaskDTO, PlexServerDTO } from '@dto/mainApi';

declare interface ISelection {
	plexServerId: number;
	downloadTaskIds: number[];
}

@Component
export default class Downloads extends Vue {
	plexServers: PlexServerDTO[] = [];
	downloads: DownloadTaskDTO[] = [];
	openExpansions: number[] = [];
	downloadTaskDetail: DownloadTaskDTO | null = null;
	selected: ISelection[] = [];

	private dialog: boolean = false;

	get getSelected(): number[] {
		return this.selected.map((x) => x.downloadTaskIds).flat(1);
	}

	get getServersWithDownloads(): PlexServerDTO[] {
		return this.plexServers.filter((x) => this.downloads.some((y) => y.plexServerId === x.id));
	}

	get hasSelected(): boolean {
		return this.getSelected.length > 0;
	}

	// region single commands

	commandSwitch({ action, item }: { action: string; item: DownloadTaskDTO }) {
		const ids = [item.id];
		switch (action) {
			case 'pause':
				this.pauseDownloadTasks(ids);
				break;
			case 'clear':
				this.clearDownloadTasks(ids);
				break;
			case 'delete':
				this.deleteDownloadTasks(ids);
				break;
			case 'stop':
				this.stopDownloadTasks(ids);
				break;
			case 'restart':
				this.restartDownloadTasks(ids);
				break;
			case 'start':
				this.startDownloadTasks(ids);
				break;
			case 'details':
				this.detailsDownloadTask(item);
				break;
			default:
				Log.error(`Action: ${action} does not have a assigned command with payload: ${item}`, { action, item });
		}
	}

	detailsDownloadTask(downloadTask: DownloadTaskDTO): void {
		this.downloadTaskDetail = downloadTask;
		this.dialog = true;
	}

	updateSelected(plexServerId: number, downloadTaskIds: number[]) {
		const index = this.selected.findIndex((x) => x.plexServerId === plexServerId);
		if (index === -1) {
			this.selected.push({ plexServerId, downloadTaskIds });
		} else {
			this.selected.splice(index, 1, { plexServerId, downloadTaskIds });
		}
	}

	// endregion

	// region batch commands
	clearDownloadTasks(downloadTaskIds: number[]): void {
		if (downloadTaskIds && downloadTaskIds.length > 0) {
			DownloadService.clearDownloadTasks(downloadTaskIds);
			return;
		}

		if (this.hasSelected) {
			DownloadService.clearDownloadTasks(this.getSelected);
			this.selected = [];
		} else {
			DownloadService.clearDownloadTasks();
		}
	}

	startDownloadTasks(downloadTaskIds: number[]): void {
		DownloadService.startDownloadTasks(downloadTaskIds);
	}

	pauseDownloadTasks(downloadTaskIds: number[]): void {
		DownloadService.pauseDownloadTasks(downloadTaskIds);
	}

	stopDownloadTasks(downloadTaskIds: number[]): void {
		DownloadService.stopDownloadTasks(downloadTaskIds);
	}

	restartDownloadTasks(downloadTaskIds: number[]): void {
		DownloadService.restartDownloadTasks(downloadTaskIds);
	}

	deleteDownloadTasks(downloadTaskIds: number[]): void {
		DownloadService.deleteDownloadTasks(downloadTaskIds);
	}

	// endregion

	closeDetailsDialog(): void {
		this.downloadTaskDetail = null;
		this.dialog = false;
	}

	mounted(): void {
		this.$subscribeTo(ServerService.getServers(), (servers) => {
			this.plexServers = servers;
			this.openExpansions = [...Array(servers?.length).keys()] ?? [];
		});

		this.$subscribeTo(DownloadService.getDownloadList(), (downloads) => {
			this.downloads = downloads;
		});
	}
}
</script>
