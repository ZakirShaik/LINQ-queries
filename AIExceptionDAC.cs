using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DT.SSO.Entity;


namespace DT.SSO.Data
{


    public class AIExceptionDAC : GenericDAC<AppInstanceException>
    {

        public static List<AppInstanceException> GetLastExceptionList(int Qty)
        {
            using (var context = new SSOContext())
            {
                List<AppInstanceException> myTopNExceptions = (from m in context.AppInstanceExceptions
                                                               select m).Take(Qty).ToList();
                return myTopNExceptions;
            }
        }

        public static List<AppInstanceException> GetLastExceptionList(string Search, int Qty)
        {
            using (var context = new SSOContext())
            {

                List<AppInstanceException> myTopNWithSearchExceptions = (from m in context.AppInstanceExceptions
                                                                         where m.MachineName.Contains(Search)
                                                                         || m.Message.Contains(Search)
                                                                         || m.Source.Contains(Search)
                                                                         select m).Take(Qty).ToList();
                return myTopNWithSearchExceptions;
            }
        }

        //Using IQueryable interface
        public List<AppInstanceException> GetLastExceptionList(string startdate, string enddate,
            string search, string qty, string AIEId)
        {
            using (var context = new SSOContext())
            {
                int Quantity = Convert.ToInt32(qty);
                IQueryable<AppInstanceException> myQuery = (from r in context.AppInstanceExceptions
                                                            select r);
                myQuery = myQuery.Where(SearchFilter(search));
                myQuery = myQuery.Where(TmspFilter(startdate, enddate));
                myQuery = myQuery.Where(PipeFilter(AIEId));
                if (String.IsNullOrWhiteSpace(qty))
                    return myQuery.ToList();
                else return myQuery.Take(Quantity).ToList();
            }

        }
        //Used expression trees with delegates.
        private Expression<Func<AppInstanceException, bool>> SearchFilter(string search)
        {
            Expression<Func<AppInstanceException, bool>> predicate = null;
            if (!String.IsNullOrWhiteSpace(search))
            {
                predicate = PredicateBuilder.False<AppInstanceException>();
                foreach (string keyword in search.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    predicate = predicate.Or(p => p.MachineName.Contains(keyword))
                                     .Or(p => p.Message.Contains(keyword))
                                     .Or(p => p.Source.Contains(keyword));
                }
            }
            else predicate = PredicateBuilder.True<AppInstanceException>();
            return predicate;
        }

        private Expression<Func<AppInstanceException, bool>> TmspFilter(string startdate, string enddate)
        {
            Expression<Func<AppInstanceException, bool>> predicate = PredicateBuilder.False<AppInstanceException>();
            DateTime sd, ed = DateTime.Now;
            if (String.IsNullOrWhiteSpace(startdate)) startdate = "01/01/1990";
            sd = Convert.ToDateTime(startdate);
            if (!String.IsNullOrWhiteSpace(enddate))
                ed = Convert.ToDateTime(enddate);
            return predicate.Or(p => (p.tmsp >= sd && p.tmsp <= ed));
        }

        private Expression<Func<AppInstanceException, bool>> PipeFilter(string AIEId)
        {
            Expression<Func<AppInstanceException, bool>> predicate = PredicateBuilder.False<AppInstanceException>();
            int Aieid = Convert.ToInt32(AIEId);
            if (!String.IsNullOrWhiteSpace(AIEId))
                predicate = predicate.Or(p => p.AppInstanceExceptionid > Aieid);
            else predicate = PredicateBuilder.True<AppInstanceException>();
            return predicate;
        }
    }
}
