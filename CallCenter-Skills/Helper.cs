using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CallCenterFunctions.Common;
using System.Collections.Generic;

namespace CallCenterFunctions.Models
{
    public class Transcription
    {
        public Project project { get; set; }
        public string[] recordingsUrls { get; set; }
        public Model[] models { get; set; }
        public string locale { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public Properties properties { get; set; }
    }

    public class Project
    {
        public string id { get; set; }
    }

    public class Properties
    {
        public string AddWordLevelTimestamps { get; set; }
        public string AddDiarization { get; set; }
        public string AddSentiment { get; set; }
        public string ProfanityFilterMode { get; set; }
        public string PunctuationMode { get; set; }
        public string TranscriptionResultsContainerUrl { get; set; }
    }

    public class Model
    {
        public string id { get; set; }
    }
}
