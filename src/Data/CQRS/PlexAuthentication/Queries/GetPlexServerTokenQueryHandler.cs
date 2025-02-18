﻿using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PlexRipper.Application.PlexAuthentication.Queries;
using PlexRipper.Data.Common;

namespace PlexRipper.Data.CQRS.PlexAuthentication
{
    public class GetPlexServerTokenQueryValidator : AbstractValidator<GetPlexServerTokenQuery>
    {
        public GetPlexServerTokenQueryValidator()
        {
            RuleFor(x => x.PlexServerId).GreaterThan(0);
        }
    }

    public class GetPlexServerTokenHandler : BaseHandler, IRequestHandler<GetPlexServerTokenQuery, Result<string>>
    {
        public GetPlexServerTokenHandler(PlexRipperDbContext dbContext) : base(dbContext) { }

        public async Task<Result<string>> Handle(GetPlexServerTokenQuery request, CancellationToken cancellationToken)
        {
            // Attempt to find a non-main account token first
            if (request.PlexAccountId == 0)
            {
                var nonMainServerToken = await _dbContext.PlexAccountServers.Include(x => x.PlexAccount)
                    .FirstOrDefaultAsync(x => x.PlexServerId == request.PlexServerId && !x.PlexAccount.IsMain);

                // Check if we have access with a non-main account
                if (nonMainServerToken != null)
                {
                    return Result.Ok(nonMainServerToken.AuthToken);
                }

                // Fallback to a main-account access
                var mainServerToken = await _dbContext.PlexAccountServers.Include(x => x.PlexAccount)
                    .FirstOrDefaultAsync(x => x.PlexServerId == request.PlexServerId);
                if (mainServerToken != null)
                {
                    return Result.Ok(mainServerToken.AuthToken);
                }

                return Result.Fail($"Could not find any authenticationToken for PlexServer with id: {request.PlexServerId}").LogError();
            }

            var authToken = await _dbContext.PlexAccountServers.FirstOrDefaultAsync(
                x => x.PlexAccountId == request.PlexAccountId && x.PlexServerId == request.PlexServerId, cancellationToken);

            if (authToken != null)
            {
                return Result.Ok(authToken.AuthToken);
            }

            return Result.Fail(new Error(
                $"Could not find an authenticationToken for PlexAccount with id: {request.PlexAccountId} and PlexServer with id: {request.PlexServerId}"));
        }
    }
}