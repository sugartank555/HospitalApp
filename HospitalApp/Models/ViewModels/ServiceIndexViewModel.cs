using HospitalApp.Models;

namespace HospitalApp.ViewModels
{
    public class PagingInfo
    {
        public int PageIndex { get; set; }      // Trang hiện tại
        public int TotalPages { get; set; }     // Tổng số trang
        public int PageSize { get; set; }       // Số record / trang
        public int TotalItems { get; set; }     // Tổng số record

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class ServiceIndexViewModel
    {
        public IEnumerable<Service> Items { get; set; } = new List<Service>();
        public PagingInfo PagingInfo { get; set; } = new PagingInfo();

        public string? Search { get; set; }
        public string? SortOrder { get; set; }
    }
}
