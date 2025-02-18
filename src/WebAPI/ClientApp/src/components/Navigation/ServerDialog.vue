<template>
	<v-dialog :value="serverId > 0" max-width="1000" @click:outside="close">
		<v-card v-if="plexServer">
			<v-card-title class="headline">{{ $t('components.server-dialog.header', { serverName: plexServer.name }) }} </v-card-title>

			<v-card-text>
				<v-tabs vertical>
					<!--	Server Data	Tab Header-->
					<v-tab>
						<v-icon left> mdi-server </v-icon>
						{{ $t('components.server-dialog.tabs.server-data.header') }}
					</v-tab>
					<!--	Library Destinations Tab Header	-->
					<v-tab>
						<v-icon left> mdi-folder-edit-outline </v-icon>
						{{ $t('components.server-dialog.tabs.download-destinations.header') }}
					</v-tab>

					<!--	Server Commands Tab Header	-->
					<v-tab>
						<v-icon left> mdi-console </v-icon>
						{{ $t('components.server-dialog.tabs.server-commands.header') }}
					</v-tab>

					<!--	Server Data Tab Content	-->
					<v-tab-item>
						<v-simple-table class="section-table">
							<tbody>
								<tr>
									<td style="width: 25%">{{ $t('components.server-dialog.tabs.server-data.server-url') }}:</td>
									<td>{{ plexServer.serverUrl }}</td>
								</tr>
								<tr>
									<td>{{ $t('components.server-dialog.tabs.server-data.machine-id') }}:</td>
									<td>{{ plexServer.machineIdentifier }}</td>
								</tr>
								<tr>
									<td>{{ $t('components.server-dialog.tabs.server-data.plex-version') }}:</td>
									<td>{{ plexServer.version }}</td>
								</tr>
								<tr>
									<td>{{ $t('components.server-dialog.tabs.server-data.created-on') }}:</td>
									<td><date-time short-date :text="plexServer.createdAt" /></td>
								</tr>
								<tr>
									<td>{{ $t('components.server-dialog.tabs.server-data.last-updated-on') }}:</td>
									<td><date-time short-date :text="plexServer.updatedAt" /></td>
								</tr>
								<tr v-if="serverStatus">
									<td>{{ $t('components.server-dialog.tabs.server-data.current-status') }}:</td>
									<td>
										<status pulse :value="serverStatus.isSuccessful" />
										{{ serverStatus.statusCode }} -
										{{ serverStatus.statusMessage }}
									</td>
								</tr>
								<!--	Server Status	-->
								<tr v-if="serverStatus">
									<td>{{ $t('components.server-dialog.tabs.server-data.last-checked-on') }}:</td>
									<td><date-time short-date :text="serverStatus.lastChecked" /></td>
								</tr>
							</tbody>
						</v-simple-table>
						<!--	Close action	-->
						<v-card-actions>
							<v-spacer></v-spacer>
							<p-btn text-id="check-server-status" @click="checkServer" />
						</v-card-actions>
					</v-tab-item>

					<!--	Library Download Destinations	Tab Content -->
					<v-tab-item>
						<v-simple-table class="section-table">
							<tbody>
								<!--	Download Destinations	-->
								<tr v-for="library in plexLibraries" :key="library.id">
									<td style="width: 50%"><media-type-icon :media-type="library.type" class="mx-3" />{{ library.title }}</td>
									<td>
										<p-select
											:value="library.defaultDestinationId"
											item-text="displayName"
											item-value="id"
											:items="getFolderPathOptions(library.type)"
											@change="updateDefaultDestination(library.id, $event)"
										/>
									</td>
								</tr>
							</tbody>
						</v-simple-table>
					</v-tab-item>
					<!--	Server Commands -->
					<v-tab-item>
						<v-simple-table class="section-table">
							<tbody>
								<tr>
									<td>{{ $t('components.server-dialog.tabs.server-commands.re-sync-server') }}</td>
									<td>
										<p-btn text-id="sync-server-libraries" @click="syncServerLibraries" />
									</td>
								</tr>
							</tbody>
						</v-simple-table>
					</v-tab-item>
				</v-tabs>
			</v-card-text>
		</v-card>
	</v-dialog>
</template>

<script lang="ts">
import { Component, Prop, Vue } from 'vue-property-decorator';
import { FolderPathDTO, FolderType, PlexLibraryDTO, PlexMediaType, PlexServerDTO, PlexServerStatusDTO } from '@dto/mainApi';
import { FolderPathService, LibraryService, ServerService } from '@service';
import { map, switchMap } from 'rxjs/operators';
import { combineLatest } from 'rxjs';
import { syncPlexServer } from '@api/plexServerApi';

@Component
export default class ServerDialog extends Vue {
	@Prop({ required: true, type: Number, default: 0 })
	readonly serverId!: number;

	show: boolean = false;

	plexServer: PlexServerDTO | null = null;
	folderPaths: FolderPathDTO[] = [];
	plexLibraries: PlexLibraryDTO[] = [];

	get serverStatus(): PlexServerStatusDTO | null {
		return this.plexServer?.status ?? null;
	}

	getFolderPathOptions(type: PlexMediaType): FolderPathDTO[] {
		switch (type) {
			case PlexMediaType.Movie:
				return this.folderPaths.filter((x) => x.folderType === FolderType.MovieFolder);
			case PlexMediaType.TvShow:
				return this.folderPaths.filter((x) => x.folderType === FolderType.TvShowFolder);
			default:
				return this.folderPaths;
		}
	}

	checkServer(): void {
		ServerService.checkServer(this.serverId);
	}

	close(): void {
		this.$emit('close');
	}

	updateDefaultDestination(libraryId: number, folderPathId: number): void {
		LibraryService.updateDefaultDestination(libraryId, folderPathId);
	}

	syncServerLibraries(): void {
		syncPlexServer(this.serverId, true).subscribe();
	}

	mounted(): void {
		this.$subscribeTo(
			this.$watchAsObservable('serverId').pipe(
				map((x: { oldValue: number; newValue: number }) => x.newValue),
				switchMap((value) =>
					combineLatest([
						ServerService.getServer(value),
						LibraryService.getLibrariesByServerId(value),
						FolderPathService.getFolderPaths(),
					]),
				),
			),
			([plexServer, plexLibraries, folderPaths]: [PlexServerDTO | null, PlexLibraryDTO[], FolderPathDTO[]]) => {
				if (plexServer) {
					this.plexServer = plexServer;
				}
				if (plexLibraries) {
					this.plexLibraries = plexLibraries;
				}
				if (folderPaths) {
					this.folderPaths = folderPaths;
				}
			},
		);
	}
}
</script>
