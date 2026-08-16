namespace ZapChat.Shared.Results;

/// <summary>Offset pagination, for admin lists where a total count is wanted.</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public long TotalCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> Empty(int page = 1, int pageSize = 50)
        => new() { Page = page, PageSize = pageSize };
}

/// <summary>
/// Cursor pagination, for message history. Correct where offset pagination is not:
/// new messages arriving while a user scrolls do not shift the window and cause
/// duplicates or gaps.
/// </summary>
public sealed class CursorPage<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>Pass back as ?before= to fetch the next older page. Null at the start of history.</summary>
    public string? NextCursor { get; init; }

    public bool HasMore { get; init; }

    public static CursorPage<T> Empty() => new();
}

/// <summary>
/// Explicitly models "this could not be computed" so a failed analytics query is
/// never rendered as a zero. Replaces the catch{} -> return 0 pattern.
/// </summary>
public sealed class Availability<T>
{
    public bool IsAvailable { get; private init; }
    public T? Value { get; private init; }
    public string? Reason { get; private init; }

    public static Availability<T> Available(T value) => new() { IsAvailable = true, Value = value };

    public static Availability<T> Unavailable(string reason) =>
        new() { IsAvailable = false, Reason = reason };
}
