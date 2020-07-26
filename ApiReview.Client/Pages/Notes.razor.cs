using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

using ApiReview.Client.Services;
using ApiReview.Shared;

namespace ApiReview.Client.Pages
{
    [Authorize(Roles = ApiReviewConstants.ApiApproverRole)]
    public partial class Notes
    {
        [Inject]
        private NotesService NotesService { get; set; }

        private CancellationTokenSource _cts;
        private DateTimeOffset _start;
        private DateTimeOffset _end;

        private DateTimeOffset Date
        {
            get => _start;
            set
            {
                _start = value.Date.Add(_start.TimeOfDay);
                _end = value.Date.Add(_end.TimeOfDay);
                UpdateValidation();
            }
        }

        private DateTimeOffset Start
        {
            get => _start;
            set
            {
                _start = _start.Date.Add(value.TimeOfDay);
                UpdateValidation();
            }
        }

        private DateTimeOffset End
        {
            get => _end;
            set
            {
                _end = _end.Date.Add(value.TimeOfDay);
                UpdateValidation();
            }
        }

        private string DateValidationMessage { get; set; }
        private string StartValidationMessage { get; set; }
        private string EndValidationMessage { get; set; }
        private bool HasValidationErrors => DateValidationMessage != null ||
                                            StartValidationMessage != null ||
                                            EndValidationMessage != null;
        private bool CanSearch => !HasValidationErrors && !IsLoading;

        private bool IncludeVideo { get; set; }

        private IReadOnlyList<ApiReviewVideo> Videos { get; set; } = Array.Empty<ApiReviewVideo>();
        private ApiReviewVideo SelectedVideo { get; set; }

        private bool IsLoading { get; set; }
        private ApiReviewSummary Summary { get; set; }

        private ApiReviewPublicationResult PublicationResult { get; set; }

        protected override void OnInitialized()
        {
            _start = DateTime.Now.Date;
            _end = _start.AddHours(23).AddMinutes(59);
            IncludeVideo = true;
        }

        private void UpdateValidation()
        {
            if (Start.Date > DateTimeOffset.Now.Date)
            {
                DateValidationMessage = "Date cannot be in the future";
                StartValidationMessage = null;
            }
            else
            {
                DateValidationMessage = null;

                if (Start > DateTimeOffset.Now)
                {
                    StartValidationMessage = "Start cannot be in the future";
                }
                else
                {
                    StartValidationMessage = null;
                }
            }

            if (End <= Start)
            {
                EndValidationMessage = "End Time must be after Start Time";
            }
            else
            {
                EndValidationMessage = null;
            }
        }

        private async Task FindIssuesAsync()
        {
            IsLoading = true;
            Videos = Array.Empty<ApiReviewVideo>();
            SelectedVideo = null;
            Summary = null;
            PublicationResult = null;
            StateHasChanged();

            if (_cts != null)
                _cts.Cancel();

            _cts = new CancellationTokenSource();

            var token = _cts.Token;

            IReadOnlyList<ApiReviewVideo> videos = Array.Empty<ApiReviewVideo>();

            if (IncludeVideo)
            {
                videos = await NotesService.GetVideos(Start, End);

                if (token.IsCancellationRequested)
                    return;
            }

            Videos = videos;
            SelectedVideo = videos.FirstOrDefault();
            StateHasChanged();

            ApiReviewSummary summary;

            if (SelectedVideo != null)
                summary = await NotesService.IssuesForVideo(SelectedVideo.Id);
            else
                summary = await NotesService.IssuesForRange(Start, End);

            if (token.IsCancellationRequested)
                return;

            Summary = summary;
            IsLoading = false;
        }

        private async Task SelectVideoAsync(ApiReviewVideo video)
        {
            if (video == SelectedVideo)
                return;

            SelectedVideo = video;
            Summary = null;
            PublicationResult = null;
            IsLoading = true;
            StateHasChanged();

            if (_cts != null)
                _cts.Cancel();

            _cts = new CancellationTokenSource();

            var token = _cts.Token;

            var summary = await NotesService.IssuesForVideo(video.Id);
            if (token.IsCancellationRequested)
                return;

            Summary = summary;
            IsLoading = false;
        }

        private async Task PublishNotesAsync()
        {
            IsLoading = true;

            PublicationResult = await NotesService.PublishNotesAsync(Summary);

            IsLoading = false;
        }
    }
}
