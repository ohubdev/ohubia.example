using Ohd.AI.Core;
using Ohd.AI.Core.Util;
using Ohd.AI.OpenAi;
using System.Text.Json;

namespace Oha.Agent.Supervisor.Tools
{
    public class FetchTextFileToolOptions
    {
        public string FilePath { get; private set; }

        public FetchTextFileToolOptions(string filePath)
        {
            FilePath = filePath;
        }
    }

    public class FetchTextFileTool : ChatTool
    {
        private readonly FetchTextFileToolOptions _toolOptions;

        public FetchTextFileTool(FetchTextFileToolOptions toolOptions)
        {
            this.Description = "Fetches a text file and returns its contents encoded in Base64 for agent processing.";
            this.ParametersSchema = new ParameterSchema(new
            {
                type = "object",
                properties = new
                {
                    fileName = new
                    {
                        type = "string",
                        description = "Name of the text file to retrieve and encode in Base64."
                    }
                },
                required = new[] { "fileName" }
            });
            this._toolOptions = toolOptions;
        }

        public async Task<string> ExecuteAsync(string fileName)
        {
            string fileContent = await File.ReadAllTextAsync(fileName);
            string base64EncodedContent = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fileContent));

            return JsonSerializer.Serialize(new
            {
                fileName,
                contentBase64 = base64EncodedContent
            });
        }
    }


}
