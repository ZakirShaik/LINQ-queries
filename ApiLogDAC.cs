using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DT.SSO.Entity;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq.Expressions;
using System.Data.SqlClient;

namespace DT.SSO.Data
{
    public class ApiLogDAC : GenericDAC<ApiLog>
    {

        public List<ApiLog> GetLogInfoByFileId(ApiLogFile apiLogFile)
        {
            using (var context = new SSOContext())
            {
                List<ApiLog> f1 = (from f in context.ApiLogs
                                   where f.ApiLogFileId  == apiLogFile.ApiLogFileId
                                   select f).ToList();
                return f1;
            }

        }

        //Get Performance details
        public List<PerformanceDetails> GetLastPerformanceDetailsList(string startdate, string enddate)
        {
            using (var context = new SSOContext())
            {
                IQueryable<ApiLog> query = (from p in context.ApiLogs
                                            select p);
                query = query.Where(TmspFilter(startdate, enddate));
                var GetDetails =  query.GroupBy(g => g.AppObjectId)
                                        .Select(group => new
                                        {
                                            ID = group.Key,
                                            Min = Math.Round(group.Min(t => t.TotalMillisecond / 1000.0), 1),
                                            Max = Math.Round(group.Max(t => t.TotalMillisecond/1000.0),2),
                                            Total = Math.Round(group.Sum(t => t.TotalMillisecond * 0.001),2),
                                            Count = group.Count(),
                                            Avg = Math.Round(((group.Sum(t => t.TotalMillisecond * 0.001)) / group.Count()),2)
                                        });

                var myQuery = (from r in GetDetails
                               join s in context.AppObjects.AsQueryable()
                               on r.ID equals s.AppObjectId
                               orderby r.ID descending
                               select new PerformanceDetails
                               {
                                   AppObjectId = r.ID,
                                   Name = s.Name,
                                   AccessPath = s.AccessPath,
                                   MinimumTime = r.Min,
                                   MaximumTime = r.Max,
                                   TotalTime = r.Total,
                                   AverageTime = r.Avg,
                                   Count = r.Count
                                              });
               
                return myQuery.ToList();
            }
        }

        //Dates filter
        private Expression<Func<ApiLog, bool>> TmspFilter(string startdate, string enddate)
        {
            Expression<Func<ApiLog, bool>> predicate = PredicateBuilder.False<ApiLog>();
            DateTime sd, ed = DateTime.Now;
            if (String.IsNullOrWhiteSpace(startdate)) startdate = "01/01/1990";
            sd = Convert.ToDateTime(startdate);
            if (!String.IsNullOrWhiteSpace(enddate))
                ed = Convert.ToDateTime(enddate);
            return predicate.Or(p => (p.Tmsp >= sd && p.Tmsp <= ed));
        }


        public static List<HourlyDetails> PerformanceDetailsByHour(int AppObjectId, string startdate, string enddate)
        {
            DateTime sd = Convert.ToDateTime(startdate), ed = Convert.ToDateTime(enddate);
            if (String.IsNullOrWhiteSpace(startdate))
            {
                startdate = "01/01/1990";
                sd = Convert.ToDateTime(startdate);
            }
            if (String.IsNullOrWhiteSpace(enddate))
            {
                ed = DateTime.Now;
            }
            //string strSQL = "dbo.GetAPILogSummaryByDay @AppObjectId, @startdate, @enddate";
            string strSQL = "select * from dbo.GetAPILogSummaryByDay(@AppObjectId, @startdate, @enddate) ORDER BY date,hour ASC";
            using (var context = new SSOContext())
            {
                var p1 = new SqlParameter("@AppObjectId", AppObjectId);
                var p2 = new SqlParameter("@startdate", sd);
                var p3 = new SqlParameter("@enddate", ed);

              List<HourlyDetails> myResult = context.Database.SqlQuery<HourlyDetails>(strSQL, p1, p2, p3).ToList();

                return myResult;
            }
            }


    }

    public class PerformanceDetails
    {
        public string Name { get; set; }
        public string AccessPath { get; set; }
        public int AppObjectId { get; set; }
        public double? MinimumTime { get; set; }
        public double? MaximumTime { get; set; }
        public double? TotalTime { get; set; }
        public double? AverageTime { get; set; }
        public int? Count { get; set; }
        public int hour { get; set; }
        public DateTime date { get; set; }
    }

    public class HourlyDetails
    {
        public DateTime date { get; set; }
        public int hour { get; set; }
        public decimal MinimumTime { get; set; }
        public decimal MaximumTime { get; set; }
        public decimal AverageTime { get; set; }
        public int Count { get; set; }
    }
}
