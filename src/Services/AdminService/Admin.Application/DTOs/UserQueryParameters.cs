namespace Admin.Application.DTOs;

public class UserQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Search { get; set; }
    public string? Status { get; set; } // "Active", "Deleted", or "All"
    public string? Department { get; set; }
    public string? Branch { get; set; }
    public string? SortBy { get; set; } // "JoinedDate", "Name", "Email", "Department", "Branch", "Status"
    public bool SortDesc { get; set; } = false;
}
