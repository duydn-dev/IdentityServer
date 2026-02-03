namespace IdentityServerHost.Models.ViewModels;

/// <summary>Model cho partial _Pagination và _PageSizeSelector.</summary>
public class PaginationViewModel
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string ActionName { get; set; } = "Index";
    public string? ControllerName { get; set; }
    /// <summary>Các tham số route bổ sung (search, subjectId, userId, ...). Không cần thêm page, pageSize. Giá trị null/empty sẽ bị bỏ qua.</summary>
    public Dictionary<string, string?> RouteValues { get; set; } = new();
    /// <summary>Các lựa chọn page size. Mặc định: 10, 20, 50, 100.</summary>
    public int[] PageSizeOptions { get; set; } = { 10, 20, 50, 100 };

    public int TotalPages => TotalCount <= 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public int From => TotalCount <= 0 ? 0 : (Page - 1) * PageSize + 1;
    public int To => Math.Min(Page * PageSize, TotalCount);
}
