<template>
	<v-row justify="start">
		<v-col>
			<v-dialog :value="open" persistent max-width="900px">
				<v-card>
					<v-card-title>
						<v-row>
							<v-col cols="12">
								<label>
									{{ $t('components.directory-browser.select-path', { pathName: path.displayName }) }}
								</label>
							</v-col>
							<v-col cols="12" style="max-height: 75px">
								<v-text-field v-model="newDirectory" outlined color="red" placeholder="Start typing or select a path below" />
							</v-col>
						</v-row>
					</v-card-title>
					<v-divider />

					<v-card-text style="height: 100%; overflow-y: hidden">
						<!--	Loading screen	-->
						<template v-if="isLoading">
							<v-row style="height: 50vh; width: 100%" justify="center" align="center">
								<v-col cols="auto">
									<loading-spinner />
								</v-col>
							</v-row>
						</template>
						<!--	Directory Browser	-->
						<template v-else>
							<!--	Directory browser table header -->
							<v-simple-table>
								<template #default>
									<thead>
										<tr>
											<th class="text-left" :width="100">{{ $t('components.directory-browser.type') }}:</th>
											<th class="text-left">{{ $t('components.directory-browser.path') }}:</th>
										</tr>
									</thead>
								</template>
							</v-simple-table>
							<!--	Directory browser table content -->
							<vue-scroll>
								<div style="height: 50vh; width: 100%">
									<v-simple-table>
										<template #default>
											<tbody>
												<tr v-for="(item, i) in items" :key="i" style="cursor: pointer" @click="directoryNavigate(item)">
													<td :width="100">
														<v-icon>{{ getIcon(item.type) }}</v-icon>
													</td>
													<td>{{ item.name }}</td>
												</tr>
											</tbody>
										</template>
									</v-simple-table>
								</div>
							</vue-scroll>
						</template>
					</v-card-text>
					<v-card-actions class="justify-end" style="height: 60px">
						<p-btn :button-type="cancelButtonType" @click="cancel()" />
						<p-btn :button-type="confirmButtonType" @click="confirm()" />
					</v-card-actions>
				</v-card>
			</v-dialog>
		</v-col>
	</v-row>
</template>

<script lang="ts">
import { Component, Prop, Vue, Watch } from 'vue-property-decorator';
import { DataTableHeader } from 'vuetify';
import { getDirectoryPath } from '@api/pathApi';
import type { FileSystemModelDTO, FolderPathDTO } from '@dto/mainApi';
import { FileSystemEntityType } from '@dto/mainApi';
import ButtonType from '@enums/buttonType';
import { debounce, distinctUntilChanged, map } from 'rxjs/operators';
import { timer } from 'rxjs';

@Component
export default class DirectoryBrowser extends Vue {
	@Prop({ required: false, type: Object as () => FolderPathDTO })
	readonly path!: FolderPathDTO;

	@Prop({ required: true, type: Boolean })
	readonly open!: boolean;

	parentPath: string = '';
	newDirectory: string = '';
	isLoading: boolean = true;

	items: FileSystemModelDTO[] = [];

	headers: DataTableHeader[] = [
		{
			text: 'Type',
			value: 'type',
			width: 60,
			class: 'directory-row',
		},
		{
			text: 'Name',
			value: 'name',
			class: 'directory-row',
		},
	];

	getIcon(type: FileSystemEntityType): string {
		switch (type) {
			case FileSystemEntityType.Parent:
				return 'mdi-arrow-left';
			case FileSystemEntityType.Drive:
				return 'mdi-harddisk';
			case FileSystemEntityType.Folder:
				return 'mdi-folder';
			case FileSystemEntityType.File:
				return 'mdi-file';
			default:
				return 'mdi-crosshairs-question';
		}
	}

	@Watch('open', { immediate: true, deep: true })
	onDialogOpen(val: boolean): void {
		if (val) {
			this.newDirectory = this.path?.directory ?? '';
			this.requestDirectories(this.newDirectory);
		}
	}

	get cancelButtonType(): ButtonType {
		return ButtonType.Cancel;
	}

	get confirmButtonType(): ButtonType {
		return ButtonType.Confirm;
	}

	confirm(): void {
		this.path.directory = this.newDirectory;
		this.$emit('confirm', this.path);
	}

	cancel(): void {
		this.$emit('cancel');
	}

	directoryNavigate(dataRow: FileSystemModelDTO): void {
		if (dataRow.path === '..') {
			this.requestDirectories(this.parentPath);
		} else {
			this.requestDirectories(dataRow.path);
		}
	}

	requestDirectories(path: string): void {
		if (path === '' || path === '/') {
			this.isLoading = true;
		}

		this.$subscribeTo(getDirectoryPath(path), (data) => {
			if (data.isSuccess && data.value) {
				this.items = data.value?.directories;

				// Don't add return row if in the root folder
				if (path !== '') {
					this.items.unshift({
						name: '...',
						path: '..',
						type: FileSystemEntityType.Parent,
						extension: '',
						size: 0,
						lastModified: '',
					});
				}
				this.parentPath = data.value?.parent;
				this.newDirectory = path;
				this.isLoading = false;
			}
		});
	}

	mounted(): void {
		// On user input request the path
		this.$subscribeTo(
			this.$watchAsObservable('newDirectory').pipe(
				map((x: { oldValue: string; newValue: string }) => x.newValue),
				debounce(() => timer(1000)),
				distinctUntilChanged(),
			),
			(value) => {
				this.requestDirectories(value);
			},
		);
	}
}
</script>
