﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CallCeneterFunctions.Models
{

    public class S2TResponse
    {
        public Audiofileresult[] AudioFileResults { get; set; }
    }

    public class Audiofileresult
    {
        public string AudioFileName { get; set; }
        public object AudioFileUrl { get; set; }
        public float AudioLengthInSeconds { get; set; }
        public Combinedresult[] CombinedResults { get; set; }
        public Segmentresult[] SegmentResults { get; set; }
    }

    public class Combinedresult
    {
        public object ChannelNumber { get; set; }
        public string Lexical { get; set; }
        public string ITN { get; set; }
        public string MaskedITN { get; set; }
        public string Display { get; set; }
    }

    public class Segmentresult
    {
        public string RecognitionStatus { get; set; }
        public object ChannelNumber { get; set; }
        public object SpeakerId { get; set; }
        public long Offset { get; set; }
        public int Duration { get; set; }
        public float OffsetInSeconds { get; set; }
        public float DurationInSeconds { get; set; }
        public Nbest[] NBest { get; set; }
    }

    public class Nbest
    {
        public float Confidence { get; set; }
        public string Lexical { get; set; }
        public string ITN { get; set; }
        public string MaskedITN { get; set; }
        public string Display { get; set; }
        public object Sentiment { get; set; }
        public WordInfo[] Words { get; set; }
    }

    public class WordInfo
    {
        public string Word { get; set; }
        public long Offset { get; set; }
        public int Duration { get; set; }
        public float OffsetInSeconds { get; set; }
        public float DurationInSeconds { get; set; }
    }

}
