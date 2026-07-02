using System.Net.Http.Json;
using Admin.Application.DTOs;
using Admin.Application.Interfaces;
using Admin.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Admin.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IReportRepository _reportRepository;
    private readonly ServiceUrlsOptions _serviceUrls;

    public AnalyticsService(
        IHttpClientFactory httpClientFactory,
        IReportRepository reportRepository,
        IOptions<ServiceUrlsOptions> serviceUrls)
    {
        _httpClientFactory = httpClientFactory;
        _reportRepository = reportRepository;
        _serviceUrls = serviceUrls.Value;
    }

    // ─── Legacy chart-point methods (kept for existing controller endpoints) ──

    public async Task<IEnumerable<ChartDataPointDto>> GetMostActiveRoomsAsync(int top = 10)
    {
        var rooms = await GetActiveRoomsAsync(top);
        return rooms.Select(r => new ChartDataPointDto
        {
            Label = r.RoomName,
            Value = r.MessageCount
        });
    }

    public async Task<IEnumerable<ChartDataPointDto>> GetMostActiveUsersAsync(int top = 10)
    {
        var users = await GetActiveUsersAsync(top);
        return users.Select(u => new ChartDataPointDto
        {
            Label = u.AnonymousName,
            Value = u.MessageCount
        });
    }

    public async Task<IEnumerable<ChartDataPointDto>> GetDailyMessagesAsync(int days = 30)
    {
        var series = await GetDailySeriesFromService(
            $"{_serviceUrls.ChatService}/api/admin/analytics/daily-messages?days={days}", days);
        return series.Select(x => new ChartDataPointDto { Label = x.Date, Value = x.Count });
    }

    public async Task<IEnumerable<ChartDataPointDto>> GetDailyPollsAsync(int days = 30)
    {
        var series = await GetDailySeriesFromService(
            $"{_serviceUrls.PollService}/api/admin/analytics/daily-polls?days={days}", days);
        return series.Select(x => new ChartDataPointDto { Label = x.Date, Value = x.Count });
    }

    public async Task<IEnumerable<ChartDataPointDto>> GetDailyNotificationsAsync(int days = 30)
    {
        var series = await GetDailySeriesFromService(
            $"{_serviceUrls.NotificationService}/api/admin/analytics/daily-notifications?days={days}", days);
        return series.Select(x => new ChartDataPointDto { Label = x.Date, Value = x.Count });
    }

    public async Task<IEnumerable<ChartDataPointDto>> GetDailyReportsAsync(int days = 30)
    {
        var trends = await GetReportTrendsAsync(days);
        return trends.Select(x => new ChartDataPointDto { Label = x.Date, Value = x.Count });
    }

    public async Task<IEnumerable<ChartDataPointDto>> GetUserGrowthAsync(int days = 30)
    {
        var series = await GetDailySeriesFromService(
            $"{_serviceUrls.AuthService}/api/admin/analytics/user-growth?days={days}", days);
        return series.Select(x => new ChartDataPointDto { Label = x.Date, Value = x.Count });
    }

    // ─── New typed methods ────────────────────────────────────────────────────

    public async Task<IEnumerable<ActiveRoomDto>> GetActiveRoomsAsync(int top = 10)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"{_serviceUrls.ChatService}/api/admin/analytics/active-rooms?top={top}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content
                    .ReadFromJsonAsync<List<ActiveRoomServiceDto>>();
                return data?.Select(r => new ActiveRoomDto
                {
                    RoomId = r.roomId,
                    RoomName = r.roomName,
                    MessageCount = r.messageCount
                }) ?? Enumerable.Empty<ActiveRoomDto>();
            }
        }
        catch { }
        return Enumerable.Empty<ActiveRoomDto>();
    }

    public async Task<IEnumerable<ActiveUserDto>> GetActiveUsersAsync(int top = 10)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"{_serviceUrls.ChatService}/api/admin/analytics/active-users?top={top}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content
                    .ReadFromJsonAsync<List<ActiveUserServiceDto>>();
                return data?.Select(u => new ActiveUserDto
                {
                    AnonymousName = u.anonymousName,
                    MessageCount = u.messageCount
                }) ?? Enumerable.Empty<ActiveUserDto>();
            }
        }
        catch { }
        return Enumerable.Empty<ActiveUserDto>();
    }

    public async Task<IEnumerable<DailyCountDto>> GetPrivateChatVolumeAsync(int days = 30)
    {
        return await GetDailySeriesFromService(
            $"{_serviceUrls.PrivateChatService}/api/admin/analytics/private-chat-volume?days={days}", days);
    }

    public async Task<IEnumerable<MostVotedPollDto>> GetMostVotedPollsAsync(int top = 10)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"{_serviceUrls.PollService}/api/admin/analytics/most-voted-polls?top={top}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content
                    .ReadFromJsonAsync<List<MostVotedPollServiceDto>>();
                return data?.Select(p => new MostVotedPollDto
                {
                    PollId = p.pollId,
                    Question = p.question,
                    TotalVotes = p.totalVotes,
                    CreatedAt = p.createdAt
                }) ?? Enumerable.Empty<MostVotedPollDto>();
            }
        }
        catch { }
        return Enumerable.Empty<MostVotedPollDto>();
    }

    public async Task<IEnumerable<ReportReasonDto>> GetReportReasonsAsync()
    {
        var allReports = await _reportRepository.GetAllAsync(page: 1, pageSize: 10000);
        return allReports
            .GroupBy(r => r.Reason.Trim())
            .Select(g => new ReportReasonDto { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count);
    }

    public async Task<IEnumerable<DailyCountDto>> GetReportTrendsAsync(int days = 30)
    {
        var dailyCounts = await _reportRepository.GetDailyCountsAsync(days);
        var since = DateTime.UtcNow.AddDays(-days).Date;
        var lookup = dailyCounts.ToDictionary(x => x.Date.Date, x => x.Count);

        return Enumerable.Range(0, days).Select(offset =>
        {
            var date = since.AddDays(offset);
            return new DailyCountDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Count = lookup.TryGetValue(date, out var c) ? c : 0
            };
        });
    }

    // ─── New analytics methods ────────────────────────────────────────────────

    /// <summary>
    /// Room Health: fetches active-rooms from ChatService, then cross-references with the local
    /// Reports table (MessageType=Room) using ChatService message IDs to compute per-room report counts.
    /// Because Report.MessageId refers to a chat message ID (not a room ID), we ask ChatService
    /// for the room-health data it can compute (it knows both messages and rooms).
    /// AdminService calls a dedicated ChatService endpoint that returns room message counts + report
    /// counts computed from the ChatService side. The ChatService does NOT have access to the
    /// Admin Reports table, so we pass reported message IDs from the Admin DB to the ChatService call.
    /// Simpler alternative: AdminService queries its own Reports WHERE MessageType=Room, groups by
    /// MessageId (each MessageId is a room message), then calls ChatService active-rooms for message
    /// counts, and approximates report count per room.
    /// 
    /// Practical approach for POC: AdminService has all the data it needs:
    ///   1. Reports WHERE MessageType=Room — gives count of room message reports
    ///   2. ChatService /active-rooms — gives (roomId, roomName, messageCount)
    ///   We can group room-level reports from AdminDB by nothing meaningful since MessageId ≠ RoomId.
    ///   Instead we use a simpler heuristic: total room reports / total room messages per room name.
    ///   ChatService must provide per-room report counts via the new endpoint.
    /// </summary>
    public async Task<IEnumerable<RoomHealthDto>> GetRoomHealthAsync(int top = 10)
    {
        try
        {
            // Fetch all room-type reported message IDs from the Admin DB Reports table
            var allReports = await _reportRepository.GetAllAsync(page: 1, pageSize: 50000);
            var reportedMessageIds = allReports
                .Where(r => r.MessageType == Admin.Domain.Enums.MessageType.Room)
                .Select(r => r.MessageId)
                .Distinct()
                .ToList();

            // POST to ChatService which can correlate message IDs → rooms
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{_serviceUrls.ChatService}/api/admin/analytics/room-health?top={top}",
                new { ReportedMessageIds = reportedMessageIds });

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content
                    .ReadFromJsonAsync<List<RoomHealthServiceDto>>();
                return data?.Select(r => new RoomHealthDto
                {
                    RoomName     = r.roomName,
                    MessageCount = r.messageCount,
                    ReportCount  = r.reportCount,
                    ReportRate   = r.reportRate,
                    Health       = r.health
                }) ?? Enumerable.Empty<RoomHealthDto>();
            }
        }
        catch { }
        return Enumerable.Empty<RoomHealthDto>();
    }


    /// <summary>
    /// Poll Participation: fetches top polls from PollService, calculates participation rate
    /// using total user count from AuthService.
    /// </summary>
    public async Task<IEnumerable<PollParticipationDto>> GetPollParticipationAsync(int top = 6)
    {
        try
        {
            var pollClient = _httpClientFactory.CreateClient();
            var authClient = _httpClientFactory.CreateClient("AuthService");

            // Fetch polls and total user count in parallel
            var pollTask = pollClient.GetAsync(
                $"{_serviceUrls.PollService}/api/admin/analytics/most-voted-polls?top={top}");
            
            // Use the named authClient which automatically attaches the JWT token
            var userTask = authClient.GetAsync(
                "api/auth/users?excludeAdmin=true&excludeDeleted=true");

            await Task.WhenAll(pollTask, userTask);

            var pollResponse = await pollTask;
            var userResponse = await userTask;

            if (!pollResponse.IsSuccessStatusCode)
                return Enumerable.Empty<PollParticipationDto>();

            var polls = await pollResponse.Content
                .ReadFromJsonAsync<List<MostVotedPollServiceDto>>() ?? new();

            int totalUsers = 0;
            if (userResponse.IsSuccessStatusCode)
            {
                // The /api/auth/users response is an array — just count elements
                var users = await userResponse.Content
                    .ReadFromJsonAsync<List<object>>() ?? new();
                totalUsers = users.Count;
            }

            return polls
                .OrderByDescending(p => p.totalVotes)
                .Take(top)
                .Select(p => new PollParticipationDto
                {
                    PollQuestion      = p.question,
                    TotalVotes        = p.totalVotes,
                    ParticipationRate = totalUsers > 0
                        ? (int)Math.Round((double)p.totalVotes / totalUsers * 100)
                        : 0
                });
        }
        catch { }
        return Enumerable.Empty<PollParticipationDto>();
    }

    /// <summary>
    /// Hourly Activity: fetches message counts grouped by hour from ChatService.
    /// Returns all 24 hours (0–23), filling zeros for hours with no data.
    /// </summary>
    public async Task<IEnumerable<HourlyActivityDto>> GetHourlyActivityAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"{_serviceUrls.ChatService}/api/admin/analytics/hourly-activity");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content
                    .ReadFromJsonAsync<List<HourlyActivityServiceDto>>();
                if (data != null)
                {
                    var lookup = data.ToDictionary(x => x.hour, x => x.messageCount);
                    return Enumerable.Range(0, 24).Select(h => new HourlyActivityDto
                    {
                        Hour         = h,
                        MessageCount = lookup.TryGetValue(h, out var c) ? c : 0
                    });
                }
            }
        }
        catch { }

        // Return empty 24-hour series
        return Enumerable.Range(0, 24).Select(h => new HourlyActivityDto { Hour = h, MessageCount = 0 });
    }

    /// <summary>
    /// Room Sentiment: fetches keyword-scored sentiment percentages per room from ChatService.
    /// </summary>
    public async Task<IEnumerable<RoomSentimentDto>> GetRoomSentimentAsync(int top = 8)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"{_serviceUrls.ChatService}/api/admin/analytics/room-sentiment?top={top}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content
                    .ReadFromJsonAsync<List<RoomSentimentServiceDto>>();
                return data?.Select(r => new RoomSentimentDto
                {
                    RoomName = r.roomName,
                    Positive = r.positive,
                    Neutral  = r.neutral,
                    Negative = r.negative
                }) ?? Enumerable.Empty<RoomSentimentDto>();
            }
        }
        catch { }
        return Enumerable.Empty<RoomSentimentDto>();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task<IEnumerable<DailyCountDto>> GetDailySeriesFromService(string url, int days)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content
                    .ReadFromJsonAsync<List<ServiceDailyDto>>();
                return data?.Select(x => new DailyCountDto { Date = x.date, Count = x.count })
                    ?? GenerateEmptySeries(days);
            }
        }
        catch { }
        return GenerateEmptySeries(days);
    }

    private static IEnumerable<DailyCountDto> GenerateEmptySeries(int days)
    {
        var since = DateTime.UtcNow.AddDays(-days).Date;
        return Enumerable.Range(0, days).Select(offset => new DailyCountDto
        {
            Date = since.AddDays(offset).ToString("yyyy-MM-dd"),
            Count = 0
        });
    }

    // ─── Service-response record types (lowercase matches JSON camelCase) ─────

    private sealed record ServiceDailyDto(string date, int count);
    private sealed record ActiveRoomServiceDto(Guid roomId, string roomName, int messageCount);
    private sealed record ActiveUserServiceDto(string anonymousName, int messageCount);
    private sealed record MostVotedPollServiceDto(Guid pollId, string question, int totalVotes, DateTime createdAt);
    private sealed record RoomHealthServiceDto(string roomName, int messageCount, int reportCount, double reportRate, string health);
    private sealed record HourlyActivityServiceDto(int hour, int messageCount);
    private sealed record RoomSentimentServiceDto(string roomName, int positive, int neutral, int negative);
}
