using System;

namespace ApplicationCore.Interfaces
{
    public interface IDashboardAnalysis
    {
        int GetDateDifference(DateTimeOffset startDate, DateTimeOffset endDate, DateTimeOffset opportunityStarDate);
    }
}
