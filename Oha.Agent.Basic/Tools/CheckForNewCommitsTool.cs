using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Ohd.AI.Core.Util;
using Ohd.AI.OpenAi;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Oha.Agent.Basic.Tools
{
    public class DevOpsToolOptions
    {
        public string Organization { get; private set; }
        public string Project { get; private set; }
        public string PersonalAccessToken { get; private set; }

        public DevOpsToolOptions(IConfiguration configuration)
        {
            Organization = configuration["AzureDevOps:Organization"];
            Project = configuration["AzureDevOps:Project"];
            PersonalAccessToken = configuration["AzureDevOps:PersonalAccessToken"];
        }
    }

    public class CheckForNewCommitsTool : ChatTool
    {
        private readonly DevOpsToolOptions _toolOptions;
        private static string _lastCommitSha = ""; // Armazena o último commit processado

        public CheckForNewCommitsTool(DevOpsToolOptions toolOptions)
        {
            this.Description = "Checks for new commits in an Azure DevOps repository.";
            this.ParametersSchema = new ParameterSchema(new
            {
                type = "object",
                properties = new
                {
                    repositoryId = new
                    {
                        type = "string",
                        description = "The ID of the repository to check for new commits."
                    }
                },
                required = new[] { "repositoryId" }
            });

            this._toolOptions = toolOptions;
        }

        public async Task<string> ExecuteAsync(string repositoryId)
        {
            string organization = _toolOptions.Organization;
            string project = _toolOptions.Project;
            string personalAccessToken = _toolOptions.PersonalAccessToken;

            string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/commits?api-version=7.1-preview.1";

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}")));

            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                JArray commits = JObject.Parse(json)["value"] as JArray;

                if (commits.Count > 0)
                {
                    string latestCommit = commits[0]["commitId"].ToString();
                    if (latestCommit != _lastCommitSha)
                    {
                        _lastCommitSha = latestCommit;
                        return JsonSerializer.Serialize(new
                        {
                            repositoryId,
                            newCommitDetected = true,
                            commitId = latestCommit,
                            author = commits[0]["author"]["name"].ToString(),
                            message = commits[0]["comment"].ToString(),
                            date = commits[0]["author"]["date"].ToString()
                        });
                    }
                    else
                    {
                        return JsonSerializer.Serialize(new
                        {
                            repositoryId,
                            newCommitDetected = false
                        });
                    }
                }
            }

            return JsonSerializer.Serialize(new
            {
                repositoryId,
                error = "Failed to fetch commits"
            });
        }

    }
}
