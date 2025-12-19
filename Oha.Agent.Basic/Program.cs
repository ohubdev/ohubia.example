using ConsoleApp2.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ohd.AI.Core;
using Ohd.AI.Core.Interfaces;
using Ohd.AI.OpenAi;
using Ohd.AI.OpenAi.Agents;
using Ohd.AI.OpenAi.Agents.Interfaces;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;

// Definições Variaveis de Ambiente
//Environment.SetEnvironmentVariable("OhdAiEnabledTelemetryLogs", "true");
//Environment.SetEnvironmentVariable("OhdAiRequestRateLimit", "5"); //In Seconds

string appDirectory = AppContext.BaseDirectory;
string filePath = Path.Combine(appDirectory, "text.txt");

var configuration = new ConfigurationBuilder()
                            .AddEnvironmentVariables()
                                .Build();

//OpenAi
//string baseUrl = "https://api.openai.com/v1";
//string apiToken = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
//string model = "gpt-4o-mini";

//GROQ
//string baseUrl = "https://api.groq.com/openai/v1";
//string apiToken = Environment.GetEnvironmentVariable("GROQ_API_KEY")!;
//string model = "llama-3.3-70b-versatile";

//Gemini
//string baseUrl = "https://generativelanguage.googleapis.com/v1beta/openai";
//string apiToken = Environment.GetEnvironmentVariable("GEMINI_API_KEY")!;
//string model = "gemini-2.0-flash-exp";

var licenseFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dev.license.lic");
OhdAiLicense.SetLicense(File.ReadAllText(licenseFileName));

services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton(new FetchTextFileToolOptions(Path.Combine(appDirectory, "text.txt")));

services.AddOhdAIOpenAiAgents();
services.AddOhdAgentTools();

var serviceProvider = services.BuildServiceProvider();

#region [ AGENT BASIC ] 

using (var scope = serviceProvider.CreateScope())
{
    var basicFactory = scope.ServiceProvider.GetRequiredService<IBasicAgentFactory>();

    string content = Path.Combine(appDirectory, "BasicAgent.yml");
    string outFile = Path.Combine(appDirectory, "result.txt");
    var agentBasic = basicFactory.CreateAgentProcessByContent(File.ReadAllText(content));

    var files = Directory.GetFiles("C:\\Projetos\\Ohubev\\DotnetMoqHelper\\", "*.cs", SearchOption.AllDirectories);

    foreach (var file in files)
    {
        agentBasic.Context.Clear();
        var result = await agentBasic.ExecuteAsync(@$"
            1. Obter o arquivo **'{file}'**.\n
              - Decodificar o conteúdo em base64 para texto.
            2. **Com base no código, identificar e sugerir melhorias conforme as melhores práticas de desenvolvimento em C#**.\n
              - Aplicar princípios **SOLID, Clean Code, DDD e Onion Architecture** quando necessário.\n
              - Identificar possíveis refatorações, melhorias de performance e código redundante.\n
            3. A resposta **deve seguir rigorosamente o formato do exemplo abaixo**:\n
                ### Exemplo de resposta:\n
                **[nome da classe]**
                **Sugestões de Melhoria:**\n
                - [Explicação detalhada da melhoria proposta]\n

            4. **Certifique-se de que todas as análises e sugestões sigam as boas práticas de desenvolvimento em C#, sejam objetivas e claras.**\n
            5. **A resposta deve ser em português do Brasil**.\n
        ");

        File.AppendAllText(outFile, result.Response);
        File.AppendAllText(outFile, "#######");
        Console.WriteLine(result.Response);
    }
}

#endregion [ AGENT BASIC ] 


#region [ CHUNKTEXT ]

//using (var scope = serviceProvider.CreateScope())
//{
//    var fileChunkerFactory = scope.ServiceProvider.GetRequiredService<IFileChunkerFactory>();
//    var chunker = fileChunkerFactory.GetChunker(".cs");

//    var resp = chunker.Chunk("");
//}

#endregion [ CHUNKTEXT ]


Console.WriteLine("Fim...");