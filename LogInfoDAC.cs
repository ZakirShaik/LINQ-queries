using DT.SSO.Log.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DT.SSO.Log.Data
{
    public class LogInfoDAC : GenericLogDAC<LogInfo>
    {

        public List<APILogDetailsByDates> getTicketApiLogDetails(string startdate, string enddate)
        {
            using (var context = new SSOLogContext())
            {
                IQueryable<LogInfo> query = (from p in context.LogInfoes
                                             select p);
                query = query.Where(TmspFilter(startdate, enddate));
                var GetDetails = query.GroupBy(g => g.APICallId)
                                        .Select(group => new
                                        {
                                            ID = group.Key,
                                            Min = Math.Round(group.Min(t => (double)(t.milliseconds)*0.001),1),
                                            Max = Math.Round(group.Max(t => (double)t.milliseconds / 1000.0), 1),
                                            Total = Math.Round(group.Sum(t => (double)t.milliseconds * 0.001), 1),
                                            Count = group.Count(),
                                            Avg = Math.Round(((group.Sum(t => (double)t.milliseconds * 0.001)) / group.Count()), 1)
                                        });

                var myQuery = (from r in GetDetails
                               join s in context.APICalls.AsQueryable()
                               on r.ID equals s.ApiCallId
                               orderby r.ID descending
                               select new APILogDetailsByDates
                               {
                                   APICallId = r.ID,
                                   Controller = s.Controller,
                                   Action = s.Action,
                                   Method = s.Method,
                                   MinimumTime = r.Min,
                                   MaximumTime = r.Max,
                                   TotalTime = r.Total,
                                   AverageTime = r.Avg,
                                   Count = r.Count
                               });

                return myQuery.ToList();
            }
        }

        public static List<TimelyDetails> getTicketApiLogDetails(string APICallId, string startdate, string enddate, string ByTime)
        {
            int ApiCallId = Convert.ToInt32(APICallId);
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
            
            using (var context = new SSOLogContext())
            {
                    // Use the connection
                    string query = "GetApiLogDetailsByTimeProcedure @APICallId, @startdate, @enddate, @ByTime";
                    var p1 = new SqlParameter("@APICallId", ApiCallId);
                    var p2 = new SqlParameter("@startdate", sd);
                    var p3 = new SqlParameter("@enddate", ed);
                    var p4 = new SqlParameter("@ByTime", ByTime);
                    var myResult = new List<TimelyDetails>();
                myResult = context.Database.SqlQuery<TimelyDetails>(query, p1, p2, p3, p4).ToList();

                        return myResult;
                }
        }

        //For scatter plot data
        public static List<allTime> getScatterPlotDataOfLogs(string APICallId, string startdate, string enddate)
        {
            int ApiCallId = Convert.ToInt32(APICallId);
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

            string strSQL = "select * from dbo.GetScatterPlotDataOfLogs(@APICallId, @startdate, @enddate) ORDER BY tmsp,seconds ASC";
            using (var context = new SSOLogContext())
            {
                var p1 = new SqlParameter("@APICallId", ApiCallId);
                var p2 = new SqlParameter("@startdate", sd);
                var p3 = new SqlParameter("@enddate", ed);

                List<allTime> myResult = context.Database.SqlQuery<allTime>(strSQL, p1, p2, p3).ToList();

                return myResult;
            }
        }


        //Dates filter
        private Expression<Func<LogInfo, bool>> TmspFilter(string startdate, string enddate)
        {
            Expression<Func<LogInfo, bool>> predicate = PredicateBuilder.False<LogInfo>();
            DateTime sd, ed = DateTime.Now;
            if (String.IsNullOrWhiteSpace(startdate)) startdate = "01/01/1990";
            sd = Convert.ToDateTime(startdate);
            if (!String.IsNullOrWhiteSpace(enddate))
                ed = Convert.ToDateTime(enddate);
            return predicate.Or(p => (p.tmspLocal >= sd && p.tmspLocal <= ed));
        }
    }
    public class APILogDetailsByDates
    {
        public int APICallId { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Method { get; set; }
        public double? MinimumTime { get; set; }
        public double? MaximumTime { get; set; }
        public double? TotalTime { get; set; }
        public double? AverageTime { get; set; }
        public double? Count { get; set; }
    }
    public class TimelyDetails
    {
        public DateTime tmsp { get; set; }
        public int? Week { get; set; }
        public int? Year { get; set; }
        public string Month { get; set; }
        public decimal? MinimumTime { get; set; }
        public decimal? MaximumTime { get; set; }
        public decimal? AverageTime { get; set; }
        public decimal? TotalTime { get; set; }
        public int? Count { get; set; }
    }
    public class allTime
    {
        public DateTime tmsp { get; set; }
        public decimal? seconds { get; set; }
    }
}
