﻿using AutoMapper;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlexRipper.Application.Common.Interfaces;
using PlexRipper.Domain;
using PlexRipper.Domain.Entities;
using PlexRipper.WebAPI.Common.DTO;
using System;
using System.Threading.Tasks;
using PlexRipper.WebAPI.Common.FluentResult;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PlexRipper.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlexLibraryController : BaseController
    {

        private readonly IPlexLibraryService _plexLibraryService;
        private readonly IMapper _mapper;


        public PlexLibraryController(IPlexLibraryService plexLibraryService, IMapper mapper) : base(mapper)
        {

            _plexLibraryService = plexLibraryService;
            _mapper = mapper;
        }

        // GET api/<PlexLibrary>/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultDTO<PlexLibraryDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResultDTO))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResultDTO))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResultDTO))]
        public async Task<IActionResult> Get(int id, int plexAccountId)
        {
            if (id <= 0) { return BadRequest(id, nameof(id)); }
            if (plexAccountId <= 0) { return BadRequest(plexAccountId, nameof(plexAccountId)); }

            try
            {
                var data = await _plexLibraryService.GetPlexLibraryAsync(id, plexAccountId);

                if (data.IsFailed)
                {
                    return BadRequest(data);
                }

                if (data.Value != null)
                {
                    var result = _mapper.Map<PlexLibraryDTO>(data.Value);
                    Log.Debug($"Found {data.Value.GetMediaCount} in library {data.Value.Title} of type {data.Value.Type}");
                    return Ok(Result.Ok(result));
                }

                string message = $"Could not find a {nameof(PlexLibrary)} with Id: {id}";
                Log.Warning(message);
                return NotFound(message);

            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        // POST api/<PlexLibrary>/refresh
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultDTO<PlexLibraryDTO>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResultDTO))]
        public async Task<IActionResult> RefreshLibrary([FromBody] RefreshPlexLibraryDTO refreshPlexLibraryDto)
        {
            var data = await _plexLibraryService.RefreshLibraryMediaAsync(refreshPlexLibraryDto.PlexAccountId, refreshPlexLibraryDto.PlexLibraryId);

            if (data.IsFailed)
            {
                return InternalServerError(data);
            }

            if (data.Value != null)
            {
                var mapResult = _mapper.Map<PlexLibraryDTO>(data.Value);
                Log.Debug($"Found {data.Value.GetMediaCount} in library {data.Value.Title} of type {data.Value.Type} after refreshing");
                return Ok(Result.Ok(mapResult));
            }

            string msg = $"Could not refresh {nameof(PlexLibrary)} with Id: {refreshPlexLibraryDto.PlexLibraryId}";
            Log.Warning(msg);
            return InternalServerError(Result.Fail(msg));
        }
    }
}
