using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oha.Agent.Supervisor.Tools;
using Ohd.AI.Core;
using Ohd.AI.Core.Interfaces;
using Ohd.AI.OpenAi;
using Ohd.AI.OpenAi.Agents;
using Ohd.AI.OpenAi.Agents.Events;
using Ohd.AI.OpenAi.Agents.Interfaces;

Environment.SetEnvironmentVariable("OhdAiRequestRateLimit", "10"); //In Seconds

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;
string appDirectory = AppContext.BaseDirectory;
var configuration = new ConfigurationBuilder()
                            .AddEnvironmentVariables()
                                .Build();

string filePath = Path.Combine(appDirectory, "text.txt");

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
services.AddSingleton(new FetchTextFileToolOptions(filePath));
services.AddOhdAIOpenAiAgents();
services.AddOhdAgentTools();

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var supervisorAgentFactory = scope.ServiceProvider.GetRequiredService<ISupervisorAgentFactory>();

    var task = @$"
        1. Obter o conteúdo do arquivo localizado em '{filePath}'.
        2. Transformar o texto extraído em OKRs bem definidos.
        3. Gerar resposta conforme exemplos.
        
        **Formato obrigatório da resposta:**
        A resposta deve seguir exatamente o modelo abaixo, sem omitir ou adicionar informações desnecessárias:

        ---
        **Exemplo de resposta:**
        **Objetivo 1:** [Descrição clara do objetivo]
        - **KR 1:** [Resultado-chave associado ao objetivo]
        - **KR 2:** [Outro resultado-chave, se aplicável]
    
        **Objetivo 2:** [Descrição clara do objetivo]
        - **KR 1:** [Resultado-chave associado ao objetivo]
        - **KR 2:** [Outro resultado-chave, se aplicável]
        ---

        **Se houver qualquer erro no processo, o sistema deve retornar uma mensagem amigável e explicativa.**
    ";

    string content = Path.Combine(appDirectory, "Supervisor.yml");
    var supervisorAgente = supervisorAgentFactory.CreateAgentProcessByPath(content);

    supervisorAgente.SupervisorMessages += (object? sender, AgentEvent e) =>
    {
        var fColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"{e.Role}:");
        Console.ForegroundColor = fColor;
        Console.WriteLine($"{e.Response}\n");
    };

    var result = await supervisorAgente.ExecuteAsync(task);
}

Console.ReadLine();
Console.WriteLine("Fim...");