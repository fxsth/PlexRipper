﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PlexRipper.Application.PlexServers;
using PlexRipper.Data.Common;
using PlexRipper.Domain;

namespace PlexRipper.Data.CQRS.PlexServers
{
    public class GetAllPlexServersQueryValidator : AbstractValidator<GetAllPlexServersQuery> { }

    public class GetAllPlexServersQueryHandler : BaseHandler,
        IRequestHandler<GetAllPlexServersQuery, Result<List<PlexServer>>>
    {
        public GetAllPlexServersQueryHandler(PlexRipperDbContext dbContext) : base(dbContext) { }

        public async Task<Result<List<PlexServer>>> Handle(GetAllPlexServersQuery request,
            CancellationToken cancellationToken)
        {
            if (request.PlexAccountId == 0)
            {
                var query = PlexServerQueryable.Include(x => x.ServerStatus).AsQueryable();

                if (request.IncludeLibraries)
                {
                    query = query.Include(x => x.PlexLibraries);
                }

                var plexServers = await query.ToListAsync(cancellationToken);

                // TODO This might return PlexServers which have no PlexAccounts available that can access them.
                return Result.Ok(plexServers);
            }
            else
            {
                var query = _dbContext.PlexAccountServers
                    .Include(x => x.PlexServer).AsQueryable();

                if (request.IncludeLibraries)
                {
                    query = query.Include(x => x.PlexServer).ThenInclude(x => x.PlexLibraries);
                }

                var plexServers = await query.Where(x => x.PlexAccountId == request.PlexAccountId)
                    .ToListAsync();

                return Result.Ok(plexServers.Select(x => x.PlexServer).ToList());
            }
        }
    }
}