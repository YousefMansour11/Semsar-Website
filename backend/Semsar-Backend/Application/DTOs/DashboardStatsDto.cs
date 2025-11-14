namespace Application.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalProperties { get; set; }
        public int TotalProjects { get; set; }

        public int RentalProperties { get; set; }
        public int ResaleProperties { get; set; }

        public int ProjectUnits { get; set; }

        public int TotalLeads { get; set; }
        public int TotalBookings { get; set; }
        public int TotalLandRequests { get; set; }

        public int FeaturedProperties { get; set; }
    }
}