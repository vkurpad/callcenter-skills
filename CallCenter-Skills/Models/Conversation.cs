using Microsoft.AspNetCore.Http.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace CallCenterFunctions.Models
{

    public class Conversation
    {
        public int turn { get; set; }
        public string speaker { get; set; }
        public string text { get; set; }
        public long offset { get; set; }
        public long duration { get; set; }
        public float offset_in_seconds { get; set; }
        public float duration_in_seconds { get; set; }
        public float sentiment { get; set; }
        public string[] key_phrases { get; set; }
        public string[] people { get; set; }
        public object[] locations { get; set; }
        public string[] organizations { get; set; }
    }
    public class ConversationSummary
    {
        public int Turns { get; set; }
        public float LowestSentiment { get; set; }
        public float HighestSentiment { get; set; }
        public Tuple<int, float> MaxChange { get; set; }
        public float AverageSentiment { get; set; }
        public int MaxChangeIndex { get; set; }
        public static Tuple<int, float> MaxDiff(List<Conversation> items)
        {
            Tuple<int, float> curr = new Tuple<int, float>(0, 0.0f);
            float current = 0;
            float result = 0;
            
            foreach(Conversation item in items)
            {
                if (item.speaker == "0") //skip the agent utterances
                    continue;
                if (current == 0)
                {
                    current = item.sentiment;
                    continue;
                }
                    
                if (item.sentiment < current && (current - item.sentiment) > result)
                {

                    result = current - item.sentiment;
                    curr = new Tuple<int, float>(item.turn, item.sentiment);

                }
                current = item.sentiment;


            }

            return curr;
        }
    }

   

}
