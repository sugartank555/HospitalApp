// Models/ViewModels/AppointmentScheduleIndexVM.cs
using System.Collections.Generic;
using HospitalApp.Models;

namespace HospitalApp.Models.ViewModels
{
    public class PagingInfo
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)System.Math.Ceiling((double)TotalItems / PageSize);

        // để giữ trạng thái khi bấm trang
        public string? Search { get; set; }
        public string? SortOrder { get; set; }
    }

    public class AppointmentScheduleIndexVM
    {
        public IEnumerable<AppointmentSchedule> Items { get; set; } = new List<AppointmentSchedule>();
        public PagingInfo Paging { get; set; } = new PagingInfo();
    }
}
