using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabTask
{
    public class Site
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Channel { get; set; }
    }
    public class ReportSummary
    {
        private IList<Report> report = new List<Report>();
        private IList<Task> task = new List<Task>();

        public IList<Report> Report
        {
            get { return report; }
            set { report = value; }
        }
        public string UnAssignedTaskListUrl { get; set; }
        public IList<Task> Task
        {
            get { return task; }
            set { task = value; }
        }
    }

    public class Report
    {
        public string Name { get; set; }
        public string Open { get; set; }
        public string OpenUrl { get; set; }
        public string OnHold { get; set; }
        public string OnHoldUrl { get; set; }
        public string OverDue { get; set; }
        public string OverDueUrl { get; set; }

    }

    public class Task
    {
        public string Message { get; set; }
        public string Url { get; set; }
    }
}
