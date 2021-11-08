using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VGICancellazioneFile
{
    class Program
    {
        public static IConfiguration config;
        public static bool TestMode { get; set; }
        static void Main(string[] args) 
        {
            //configuration of the app launch
            AppStart();
            Console.WriteLine("Start delete");
            Log.Information("Start delete");
            //begin the job
            StartDeleting();
          
        }

        public static void StartDeleting()
        {
            try
            {
                Log.Information("Read the files from the external configuration file");
                //Read the files from the external configuration file
                List<Folders> cartellaList = config.GetSection("FilesConfiguration").Get<List<Folders>>();
                TestMode =config.GetValue<bool>("TestMode");
                Log.Information(String.Format("Found {0} folders", cartellaList.Count));
                //schedule work across multiple threads based on your system environment
                Parallel.ForEach(cartellaList, cartella =>                {
                    Console.WriteLine(String.Format("Started working with file {0} thread {1}", cartella.Path, Thread.CurrentThread.ManagedThreadId));
                    cartella.DeleteDirectory();
                });
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                Console.WriteLine("Error:" + e.Message);
            }

        }



        public static void AppStart()
        {

            Console.WriteLine("Start the application");
            // Code that runs on application startup
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("Configuration.json")
              .AddEnvironmentVariables();

            config = builder.Build();
            ServiceCollection serviceCollection = new ServiceCollection();
            // Specifying the configuration for serilog
            Log.Logger = new LoggerConfiguration() // initiate the logger configuration
                            .ReadFrom.Configuration(builder.Build()) // connect serilog to our configuration folder
                            .Enrich.FromLogContext() //Adds more information to our logs from built in Serilog 
                            .WriteTo.File(String.Concat(config["Serilog:File"], DateTime.Now.ToString("yyyyMMddhhmmss"), ".txt")) // decide where the logs are going to be shown
                            .CreateLogger(); //initialise the logger

            serviceCollection.AddSingleton(LoggerFactory.Create(builder =>
            {
                builder
                    .AddSerilog(dispose: true);
            }));

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();


        }

    }
}
