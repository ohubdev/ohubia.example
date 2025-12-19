//using Ohd.AI.Core.Util;
//using Ohd.AI.OpenAi;
//using System.Diagnostics;
//using System.Text.Json;
//using Supabase;
//using Supabase.Storage;
//using Oha.Agent.Basic.Tools;

//namespace Oha.Agent.Basic.Tools
//{

//    public class CloneAndDiffTool : ChatTool
//    {
//        private readonly DevOpsToolOptions _toolOptions;
//        private readonly SupabaseToolOptions _supabaseOptions; // Add Supabase options
//        private Supabase.Client _supabaseClient;  // Add Supabase client

//        public CloneAndDiffTool(DevOpsToolOptions toolOptions, SupabaseToolOptions supabaseOptions) // Add SupabaseOptions to the constructor
//        {
//            this.Description = "Clones a repository and finds the exact code changes for a given commit. Uploads the diff to Supabase Storage and provides a temporary signed URL.";
//            this.ParametersSchema = new ParameterSchema(new
//            {
//                type = "object",
//                properties = new
//                {
//                    repositoryId = new
//                    {
//                        type = "string",
//                        description = "The repository ID for reference."
//                    },
//                    commitId = new
//                    {
//                        type = "string",
//                        description = "The commit ID to compare changes."
//                    },
//                    expiresIn = new
//                    {
//                        type = "integer",
//                        description = "The expiration time (in seconds) for the signed URL. Defaults to 3600 seconds (1 hour).",
//                        @default = 3600
//                    }

//                },
//                required = new[] { "repositoryId", "commitId" }
//            });

//            this._toolOptions = toolOptions;
//            this._supabaseOptions = supabaseOptions; // Assign Supabase options

//            // Initialize Supabase client in the constructor.  Handle potential errors
//            try
//            {
//                _supabaseClient = new Supabase.Client(_supabaseOptions.SupabaseUrl, _supabaseOptions.SupabaseKey, new SupabaseOptions()
//                {
//                    AutoRefreshToken = true,
//                    AutoConnectRealtime = false
//                });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error initializing Supabase client: {ex.Message}");
//                // Consider throwing an exception or setting a flag to indicate that Supabase is not available.
//                _supabaseClient = null; //Important, so you don't try to use it later.
//            }
//        }

//        public async Task<string> ExecuteAsync(string repositoryId, string commitId, int expiresIn = 3600)  //Add expiresIn parameter with a default value
//        {
//            string repoPath = Path.Combine(Path.GetTempPath(), repositoryId);
//            string diffOutput = null; // Declare diffOutput outside the try block.

//            try
//            {
//                //if (Directory.Exists(repoPath))
//                //    TryDelete(repoPath);

//                //// **Clonar o repositório**
//                //string authUrl = $"https://{_toolOptions.PersonalAccessToken}@dev.azure.com/{_toolOptions.Organization}/{_toolOptions.Project}/_git/{repositoryId}";
//                //RunGitCommand($"clone {authUrl} {repoPath}");

//                // **Navegar até o repositório**
//                Directory.SetCurrentDirectory(repoPath);

//                // **Buscar a diferença do commit**
//                string diffCommand = $"diff --unified=0 {commitId}^ {commitId} --";
//                diffOutput = RunGitCommand(diffCommand); // $"diff {commitId}^ {commitId}");
//                string fileDiffChanges = RunGitCommand($"diff --name-only {commitId}^ {commitId} --");

//                if (string.IsNullOrWhiteSpace(diffOutput))
//                {
//                    return JsonSerializer.Serialize(new { repositoryId, commitId, changes = "Nenhuma alteração encontrada." });
//                }

//                string base64EncodedContent = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(diffOutput));

//                // **Upload to Supabase Storage**
//                string supabaseStoragePath = $"diffs/{repositoryId}/{commitId}.diff"; // Define the path in Supabase Storage
//                string supabaseUrl = null;  //Will hold the signed URL
//                bool uploadSuccess = false; //Flag to check successful upload for creating signed url

//                if (_supabaseClient != null) // Check if Supabase client is initialized
//                {
//                    try
//                    {
//                        using (MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(diffOutput)))
//                        {
//                            var uploadResult = await _supabaseClient
//                                .Storage
//                                .From("diffs") // Replace with your Supabase Storage bucket name (e.g., "diffs")
//                                .Upload(stream.ToArray(), supabaseStoragePath, new Supabase.Storage.FileOptions { ContentType = "text/plain" });  //Explicitly set content type

//                            if (uploadResult != null)
//                            {
//                                supabaseUrl = "Upload to Supabase failed: " + uploadResult;  //Set an error message rather than throw exception.
//                            }
//                            else
//                            {
//                                uploadSuccess = true;  //Set upload success flag

//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"Supabase Upload Error: {ex.Message}");
//                        supabaseUrl = "Upload to Supabase failed: " + ex.Message;  //Set an error message rather than throw exception.
//                    }

//                    // Get the signed URL if upload was successful
//                    if (uploadSuccess)
//                    {
//                        try
//                        {
//                            var signedUrlResult = await _supabaseClient
//                                .Storage
//                                .From("diffs") // Replace with your Supabase Storage bucket name
//                                .CreateSignedUrl(supabaseStoragePath, expiresIn); // expiresIn in seconds.

//                            if (signedUrlResult.Error != null)
//                            {
//                                Console.WriteLine($"Supabase CreateSignedUrl Error: {signedUrlResult.Error.Message}");
//                                supabaseUrl = "Could not retrieve signed URL: " + signedUrlResult.Error.Message;
//                            }
//                            else
//                            {
//                                supabaseUrl = signedUrlResult.Data;
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            Console.WriteLine($"Supabase CreateSignedUrl Error: {ex.Message}");
//                            supabaseUrl = "Could not retrieve signed URL: " + ex.Message;

//                        }

//                    }


//                }
//                else
//                {
//                    supabaseUrl = "Supabase client not initialized.  Check configuration.";
//                }

//                return JsonSerializer.Serialize(new
//                {
//                    repositoryId,
//                    commitId,
//                    fileChanges = fileDiffChanges,
//                    changesBase64 = base64EncodedContent,
//                    supabaseUrl // Include the Supabase URL in the result
//                });
//            }
//            catch (Exception ex)
//            {
//                return JsonSerializer.Serialize(new { repositoryId, commitId, error = ex.Message });
//            }
//            finally
//            {
//                TryDelete(repoPath);
//            }
//        }

//        private void TryDelete(string repoPath)
//        {
//            try
//            {
//                if (Directory.Exists(repoPath))
//                    Directory.Delete(repoPath, true);
//            }
//            catch { }
//        }

//        private string RunGitCommand(string arguments)
//        {
//            ProcessStartInfo processInfo = new ProcessStartInfo
//            {
//                FileName = "git",
//                Arguments = arguments,
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                UseShellExecute = false,
//                CreateNoWindow = true
//            };

//            using (Process process = new Process { StartInfo = processInfo })
//            {
//                process.Start();
//                string output = process.StandardOutput.ReadToEnd();
//                string error = process.StandardError.ReadToEnd();
//                process.WaitForExit();

//                if (process.ExitCode != 0)
//                    throw new Exception($"Erro ao executar git: {error}");

//                return output;
//            }

//        }
//    }

//    // Create a class to hold your Supabase options
//    public class SupabaseToolOptions
//    {
//        public string SupabaseUrl { get; set; }
//        public string SupabaseKey { get; set; }
//    }
//}