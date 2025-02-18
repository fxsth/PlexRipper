﻿using FluentResults;
using MediatR;

namespace PlexRipper.Application.PlexLibraries
{
    public class UpdatePlexLibraryDefaultDestinationByIdCommand : IRequest<Result>
    {
        public int PlexLibraryId { get; }

        public int FolderPathId { get; }

        public UpdatePlexLibraryDefaultDestinationByIdCommand(int plexLibraryId, int folderPathId)
        {
            PlexLibraryId = plexLibraryId;
            FolderPathId = folderPathId;
        }
    }
}