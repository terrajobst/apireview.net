﻿using System;

namespace ApiReview.Data
{
    public sealed class ApiReviewFeedback
    {
        public ApiReviewIssue Issue { get; set; }
        public DateTimeOffset FeedbackDateTime { get; set; }
        public int? FeedbackId { get; set; }
        public string FeedbackUrl { get; set; }
        public string FeedbackStatus { get; set; }
        public string FeedbackMarkdown { get; set; }
        public string VideoUrl { get; set; }
    }
}
