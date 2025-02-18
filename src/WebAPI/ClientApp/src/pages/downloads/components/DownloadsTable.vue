<template>
	<div>
		<v-tree-view-table
			:items="downloadRows"
			:headers="getHeaders"
			height-auto
			media-icons
			load-children
			@action="tableAction"
			@selected="selectedAction"
		/>
	</div>
</template>

<script lang="ts">
import Log from 'consola';
import { Component, Prop, Vue } from 'vue-property-decorator';
import { DownloadStatus, DownloadTaskDTO, FileMergeProgress } from '@dto/mainApi';
import ITreeViewTableHeader from '@vTreeViewTable/ITreeViewTableHeader';
import TreeViewTableHeaderEnum from '@enums/treeViewTableHeaderEnum';
import ButtonType from '@enums/buttonType';
import { DownloadService } from '@service';

@Component
export default class DownloadsTable extends Vue {
	@Prop({ type: Boolean })
	readonly loading: Boolean = false;

	@Prop({ required: true, type: Array as () => DownloadTaskDTO[] })
	readonly value!: DownloadTaskDTO[];

	@Prop({ required: true, type: Number })
	readonly serverId!: number;

	fileMergeProgressList: FileMergeProgress[] = [];

	downloadRows: DownloadTaskDTO[] = [];

	get getHeaders(): ITreeViewTableHeader[] {
		return [
			{
				text: 'Title',
				value: 'title',
				maxWidth: 250,
			},
			{
				text: 'Status',
				value: 'status',
				width: 120,
			},
			{
				text: 'Received',
				value: 'dataReceived',
				type: TreeViewTableHeaderEnum.FileSize,
				width: 120,
			},
			{
				text: 'Size',
				value: 'dataTotal',
				type: TreeViewTableHeaderEnum.FileSize,
				width: 120,
			},
			{
				text: 'Speed',
				value: 'downloadSpeed',
				type: TreeViewTableHeaderEnum.FileSpeed,
				width: 120,
			},
			{
				text: 'ETA',
				value: 'timeRemaining',
				type: TreeViewTableHeaderEnum.Duration,
				width: 120,
			},
			{
				text: 'Percentage',
				value: 'percentage',
				type: TreeViewTableHeaderEnum.Percentage,
				width: 120,
			},
			{
				text: 'Actions',
				value: 'actions',
				type: TreeViewTableHeaderEnum.Actions,
				width: 160,
				sortable: false,
			},
		];
	}

	get flatDownloadRows(): DownloadTaskDTO[] {
		return [
			this.downloadRows,
			this.downloadRows.map((x) => x.children),
			this.downloadRows.map((x) => x.children?.map((y) => y.children)),
			this.downloadRows.map((x) => x.children?.map((y) => y.children?.map((z) => z.children))),
		]
			.flat(3)
			.filter((x) => !!x) as DownloadTaskDTO[];
	}

	setAvailableActions(downloadTasks: DownloadTaskDTO[]): DownloadTaskDTO[] {
		for (const downloadRow of downloadTasks) {
			downloadRow.actions = this.availableActions(downloadRow.status);
			if (downloadRow.children) {
				downloadRow.children = this.setAvailableActions(downloadRow.children);
			}
		}
		return downloadTasks;
	}

	availableActions(status: DownloadStatus): ButtonType[] {
		const availableActions: ButtonType[] = [ButtonType.Details];
		switch (status) {
			case DownloadStatus.Unknown:
				availableActions.push(ButtonType.Delete);
				break;
			case DownloadStatus.Initialized:
				availableActions.push(ButtonType.Delete);
				break;
			case DownloadStatus.Queued:
				availableActions.push(ButtonType.Start);
				availableActions.push(ButtonType.Delete);
				break;
			case DownloadStatus.Downloading:
				availableActions.push(ButtonType.Pause);
				availableActions.push(ButtonType.Stop);
				break;
			case DownloadStatus.Paused:
				availableActions.push(ButtonType.Start);
				availableActions.push(ButtonType.Stop);
				availableActions.push(ButtonType.Delete);
				break;
			case DownloadStatus.Completed:
				availableActions.push(ButtonType.Clear);
				availableActions.push(ButtonType.Restart);
				break;
			case DownloadStatus.Stopped:
				availableActions.push(ButtonType.Restart);
				availableActions.push(ButtonType.Delete);
				break;
			case DownloadStatus.Merging:
				break;
			case DownloadStatus.Error:
				availableActions.push(ButtonType.Restart);
				availableActions.push(ButtonType.Delete);
				break;
		}
		return availableActions;
	}

	tableAction(payload: { action: string; item: DownloadTaskDTO }) {
		Log.info('command', payload);
		this.$emit('action', payload);
	}

	selectedAction(selected: number[]) {
		// Convert downloadTask keys to downloadTask Ids
		const ids = this.flatDownloadRows.filter((x) => selected.includes(x.key)).map((x) => x.id);
		this.$emit('selected', ids);
	}

	mounted(): void {
		// Retrieve initial download list
		this.$subscribeTo(DownloadService.getDownloadList(this.serverId), (data: DownloadTaskDTO[]) => {
			this.downloadRows = this.setAvailableActions([...data]);
		});
	}
}
</script>
