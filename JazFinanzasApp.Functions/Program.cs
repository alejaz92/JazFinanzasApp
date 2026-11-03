using JazFinanzasApp.Functions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(config =>
    {
        // El appsettings.json de la API viaja con la función (modelo, precios y umbrales del asistente). Las variables de entorno
        // —la connection string, la clave de Anthropic— se vuelven a agregar al final para que le ganen al archivo.
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) => services.AddFunctionsServices(context.Configuration))
    .Build();

host.Run();
