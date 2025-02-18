﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PlexRipper.Application.PlexTvShows;
using PlexRipper.Data.Common;
using PlexRipper.Domain;

namespace PlexRipper.Data.CQRS.PlexTvShows
{
    public class GetMultiplePlexTvShowSeasonsByIdsWithEpisodesQueryValidator : AbstractValidator<GetMultiplePlexTvShowSeasonsByIdsWithEpisodesQuery>
    {
        public GetMultiplePlexTvShowSeasonsByIdsWithEpisodesQueryValidator()
        {
            RuleFor(x => x.Ids.Count).GreaterThan(0);
        }
    }

    public class GetMultiplePlexTvShowSeasonsByIdsWithEpisodesQueryHandler : BaseHandler,
        IRequestHandler<GetMultiplePlexTvShowSeasonsByIdsWithEpisodesQuery, Result<List<PlexTvShowSeason>>>
    {
        public GetMultiplePlexTvShowSeasonsByIdsWithEpisodesQueryHandler(PlexRipperDbContext dbContext) : base(dbContext) { }

        public async Task<Result<List<PlexTvShowSeason>>> Handle(GetMultiplePlexTvShowSeasonsByIdsWithEpisodesQuery request,
            CancellationToken cancellationToken)
        {
            var query = PlexTvShowSeasonsQueryable;

            if (request.IncludeData)
            {
                query = query
                    .Include(x => x.TvShow)
                    .Include(x => x.Episodes);
            }

            if (request.IncludeLibrary)
            {
                query = query.IncludePlexLibrary();
            }

            if (request.IncludeServer)
            {
                query = query.IncludeServer();
            }

            var plexTvShowSeason = await query
                .Where(x => request.Ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            return Result.Ok(plexTvShowSeason);
        }
    }
}