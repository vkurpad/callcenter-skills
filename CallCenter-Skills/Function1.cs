using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using CallCenterFunctions.Common;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using CallCenterFunctions.Models;
using Newtonsoft.Json.Linq;

namespace CallCenter_Skills
{
    public static class SubmitTranscription
    {

        private const string OneAPIOperationLocationHeaderKey = "Operation-Location";
        [FunctionName("SubmitTranscription")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext executionContext)
        {
            log.LogInformation("Submit S2T Skill: C# HTTP trigger function processed a request.");
            log.LogInformation($"REQUEST: {new StreamReader(req.Body).ReadToEnd()}");
            req.Body.Position = 0;
            try
            {
                string sasToken;
                string skillName = executionContext.FunctionName;
                IEnumerable<WebApiRequestRecord> requestRecords = WebApiSkillHelpers.GetRequestRecords(req);
                if (requestRecords == null)
                {
                    return new BadRequestObjectResult($"{skillName} - Invalid request record array.");
                }
                string storageConnectionString = Environment.GetEnvironmentVariable("StorageConnection");
                log.LogInformation("storageConnectionString= " + storageConnectionString);
                CloudStorageAccount storageAccount;
                if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("transcribed");
                    await cloudBlobContainer.CreateIfNotExistsAsync();
                    sasToken = GetContainerSasUri(cloudBlobContainer);
                }
                else
                {
                    // Otherwise, let the user know that they need to define the environment variable.
                    throw new Exception("Cannot access storage account");
                }


                WebApiSkillResponse response = await WebApiSkillHelpers.ProcessRequestRecordsAsync(skillName, requestRecords,
                    async (inRecord, outRecord) =>
                    {
                        Uri jobId;
                        var recUrl = inRecord.Data["recUrl"] as string;
                        var recSasToken = inRecord.Data["recSasToken"] as string;

                        Transcription tc = new Transcription();

                        tc.recordingsUrls = new string[] { recUrl + recSasToken };
                        tc.models = null;
                        tc.locale = "en-US";
                        tc.name = "foo";
                        tc.description = "bar";
                        tc.properties = new Properties();
                        tc.properties.AddDiarization = "True";
                        tc.properties.AddSentiment = "True";
                        tc.properties.AddWordLevelTimestamps = "True";
                        tc.properties.ProfanityFilterMode = "Masked";
                        tc.properties.PunctuationMode = "DictatedAndAutomatic";
                        tc.properties.TranscriptionResultsContainerUrl = sasToken;


                        var client = new HttpClient();
                        client.Timeout = TimeSpan.FromMinutes(25);
                        client.BaseAddress = new UriBuilder(Uri.UriSchemeHttps, $"{Environment.GetEnvironmentVariable("region")}.cris.ai", 443).Uri;
                        string path = "api/speechtotext/v3.0-beta1/Transcriptions";
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("Ocp-Apim-Subscription-Key"));
                        string res = Newtonsoft.Json.JsonConvert.SerializeObject(tc);
                        StringContent sc = new StringContent(res);
                        sc.Headers.ContentType = JsonMediaTypeFormatter.DefaultMediaType;

                        using (var resp = await client.PostAsync(path, sc))
                        {
                            if (!resp.IsSuccessStatusCode)
                            {
                                throw new HttpRequestException("Failed to create  S2T transcription job");
                            }

                            IEnumerable<string> headerValues;
                            if (resp.Headers.TryGetValues(OneAPIOperationLocationHeaderKey, out headerValues))
                            {
                                if (headerValues.Any())
                                {
                                    jobId = new Uri(headerValues.First());
                                    outRecord.Data["jobId"] = jobId;
                                }
                            }
                        }




                        return outRecord;
                    });
                log.LogInformation(JsonConvert.SerializeObject(response));
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);

            }
            return null;

        }
        private static string GetContainerSasUri(CloudBlobContainer container)
        {

            string sasContainerToken;
            SharedAccessBlobPolicy adHocPolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Create
            };
            sasContainerToken = container.GetSharedAccessSignature(adHocPolicy, null);
            return container.Uri + sasContainerToken;
        }

        [FunctionName("Split")]
        public static IActionResult RunSplit(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log, ExecutionContext executionContext)
        {
            log.LogInformation("Split URL: C# HTTP trigger function processed a request.");
            string skillName = executionContext.FunctionName;
            log.LogInformation($"REQUEST: {new StreamReader(req.Body).ReadToEnd()}");
            req.Body.Position = 0;
            IEnumerable<WebApiRequestRecord> requestRecords = WebApiSkillHelpers.GetRequestRecords(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{skillName} - Invalid request record array.");
            }
            WebApiSkillResponse response = WebApiSkillHelpers.ProcessRequestRecords(skillName, requestRecords,
            (inRecord, outRecord) =>
            {
                string[] textItems = ((JArray)inRecord.Data["text"]).ToObject<string[]>();
                string[] tokens = textItems[0].Split(new char[] { '?' });
                outRecord.Data["result"] = tokens[0];

                return outRecord;
            });
            log.LogInformation(JsonConvert.SerializeObject(response));
            return new OkObjectResult(response);
        }

        [FunctionName("Projection")]
        public static IActionResult RunProjection(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
       ILogger log, ExecutionContext executionContext)
        {
            log.LogInformation("Projection: C# HTTP trigger function processed a request.");
            string skillName = executionContext.FunctionName;
            log.LogInformation($"REQUEST: {new StreamReader(req.Body).ReadToEnd()}");
            req.Body.Position = 0;
            IEnumerable<WebApiRequestRecord> requestRecords = WebApiSkillHelpers.GetRequestRecords(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{skillName} - Invalid request record array.");
            }
            WebApiSkillResponse response = WebApiSkillHelpers.ProcessRequestRecords(skillName, requestRecords,
            (inRecord, outRecord) =>
            {
                Conversation[] turns = JsonConvert.DeserializeObject<Conversation[]>(inRecord.Data["conversation"].ToString());
                List<Conversation> output = turns.ToList<Conversation>();

                var sortedList = output.OrderBy(foo => foo.offset).ToList();
                for (int i = 1; i < sortedList.Count; i++)
                {
                    if (sortedList[i].offset > sortedList[i - 1].offset)
                        sortedList[i].turn = i;

                }
                ConversationSummary summary = new ConversationSummary();
                summary.Turns = turns.Length;
                //If this is a call where thecustomer never speaks .... set to 0
                if (sortedList.Where(c => c.speaker == "1").FirstOrDefault() == null)
                {
                    summary.AverageSentiment = 0;
                    summary.LowestSentiment = 0;
                    summary.HighestSentiment = 0;
                    summary.MaxChange = new Tuple<int, float>(0, 0.0f);
                }
                else
                {
                    summary.AverageSentiment = sortedList.Where(c => c.speaker == "1").Select(a => a.sentiment).Average();
                    summary.LowestSentiment = sortedList.Where(c => c.speaker == "1").Select(a => a.sentiment).Min();
                    summary.HighestSentiment = sortedList.Where(c => c.speaker == "1").Select(a => a.sentiment).Max();
                    summary.MaxChange = ConversationSummary.MaxDiff(sortedList);

                }

                outRecord.Data["result"] = sortedList;
                outRecord.Data["summary"] = summary;
                return outRecord;
            });
            log.LogInformation(JsonConvert.SerializeObject(response));
            return new OkObjectResult(response);
        }

    }




}