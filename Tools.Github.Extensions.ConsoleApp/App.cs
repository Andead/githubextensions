using System;
using System.IO;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Tools.Github.Extensions.ConsoleApp
{
    internal sealed class App
    {
        private readonly IContainer _container;
        private readonly Options _options;

        private App(IContainer container, Options options)
        {
            _container = container;
            _options = options;
        }

        internal static App Build(Options options)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            options = ValidateOptions(options, configuration);
            
            var builder = new ContainerBuilder();
            builder.RegisterInstance(new AutocompleteConfiguration
            {
                PollingInterval = TimeSpan.FromSeconds(options.Interval)
            });

            builder.RegisterInstance(new GitHubConfiguration
            {
                OAuthAccessToken = options.Token,
                Server = options.Server
            });

            ILogger logger = new ConsoleLoggerProvider((s, level) => {
                #if DEBUG
                return true;
                #else
                return level > LogLevel.Debug;
                #endif
            }, false).CreateLogger("default");
            builder.RegisterInstance(logger).As<ILogger>();

            builder.RegisterType<ConsoleUserInterface>().As<IUserInterface>();

            builder.RegisterType<GitHubClient>().As<IGitHubClient>();
            builder.RegisterType<AutocompleteService>().As<IAutocompleteService>();
            builder.RegisterType<ExtensionsService>();

            IContainer container = builder.Build();

            return new App(container, options);
        }

        private static Options ValidateOptions(Options options, IConfigurationRoot configuration)
        {
            IConfigurationSection github = configuration.GetSection("github");

            return new Options
            {
                Interval = options.Interval,
                Id = options.Id,
                Token = options.Token ?? github["oauthtoken"],
                Server = options.Server ?? github["server"],
                Repo = options.Repo ?? github["repo"],
                Owner = options.Owner ?? github["owner"]
            };
        }

        public static void RunWithOptions(Options options)
        {
            Build(options).Run();
        }

        public void Run()
        {
            try
            {
                _container.Resolve<ExtensionsService>()
                    .Autocomplete(_options.Owner, _options.Repo, _options.Id)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception exception)
            {
                _container.Resolve<ILogger>()
                    .LogCritical(exception, "Unhandled exception when running the application.");
            }
        }
    }
}