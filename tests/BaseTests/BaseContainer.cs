﻿using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PlexRipper.Application.Common;
using PlexRipper.Data;
using PlexRipper.Domain;
using PlexRipper.DownloadManager.Download;
using PlexRipper.WebAPI;
using PlexRipper.WebAPI.Config;

namespace PlexRipper.BaseTests
{
    public class BaseContainer
    {
        protected IContainer AutofacContainer { get; }

        /// <summary>
        /// Creates a Autofac container and sets up a test database.
        /// </summary>
        public BaseContainer()
        {
            Environment.SetEnvironmentVariable("IntegrationTestMode", "true");
            Environment.SetEnvironmentVariable("ResetDB", "true");

            var builder = new ContainerBuilder();
            ContainerConfig.ConfigureContainer(builder);

            // re-create the startup service collection and add to builder.
            var services = new ServiceCollection();
            var startUp = new Startup(GetWebHostEnvironment());
            startUp.ConfigureServices(services);
            builder.Populate(services);

            // SignalR requires the default ILogger
            builder.RegisterInstance(new LoggerFactory()).As<ILoggerFactory>();
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();

            AutofacContainer = builder.Build();
            PlexRipperDbContext.SetupAsync();
        }

        public void SetupTestAccount()
        {
            GetPlexAccountService.CreatePlexAccountAsync(new PlexAccount(Secrets.Account1.Username, Secrets.Account1.Password));
        }

        public static IWebHostEnvironment GetWebHostEnvironment()
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            return mockEnvironment.Object;
        }

        public IPlexAccountService GetPlexAccountService => AutofacContainer.Resolve<IPlexAccountService>();

        public IPlexServerService GetPlexServerService => AutofacContainer.Resolve<IPlexServerService>();

        public IPlexDownloadService GetPlexDownloadService => AutofacContainer.Resolve<IPlexDownloadService>();

        public IPlexLibraryService GetPlexLibraryService => AutofacContainer.Resolve<IPlexLibraryService>();

        public IPlexApiService GetPlexApiService => AutofacContainer.Resolve<IPlexApiService>();

        public IDownloadManager GetDownloadManager => AutofacContainer.Resolve<IDownloadManager>();

        public Func<DownloadTask, PlexDownloadClient> GetPlexDownloadClientFactory =>
            AutofacContainer.Resolve<Func<DownloadTask, PlexDownloadClient>>();

        public PlexRipperDbContext PlexRipperDbContext => AutofacContainer.Resolve<PlexRipperDbContext>();

        public IMediator Mediator => AutofacContainer.Resolve<IMediator>();
    }
}