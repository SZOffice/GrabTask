
using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace GrabTask
{
    class Program
    {
        static string token = ConfigurationManager.AppSettings["token"].ToString();
        static string username = ConfigurationManager.AppSettings["username"].ToString();
        static string password = ConfigurationManager.AppSettings["password"].ToString();
        static int interval = Convert.ToInt32(ConfigurationManager.AppSettings["Interval"].ToString());
        static string reportDataUrl = "https://helpdesk.seekasia.com/reports/CreateReportTable.jsp?viewFullReport=true&site={0}";

        static string attachmentsTemplate = "\"pretext\": \"---------------\",\"author_name\": \"{0}\",\"title\": \"{1}\",\"text\": \"{2}\",\"color\": \"{3}\"";
        static string attachmentsDetailTemplate = "\"author_name\": \"New ME ticket\",\"title\": \"{0}\",\"title_link\": \"{1}\",\"text\": \"{2}\",\"color\": \"%23ff0000\"";
        //static string cookie = ConfigurationManager.AppSettings["cookie"].ToString();
        static void Main(string[] args)
        {
            while (true)
            {
                mainAction();
                System.Threading.Thread.Sleep(interval * 1000 * 60);
            }
        }

        private static void sendSlackMessage(string url)
        {
            using (WebClient wc = new WebClient())
            {
                var reqparm = new System.Collections.Specialized.NameValueCollection();
                byte[] responsebytes = wc.UploadValues(url, "POST", reqparm);
            }
        }

        private static void login(CookieAwareWebClient wc)
        {
            wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.62 Safari/537.36";
            //wc.Headers.Add(HttpRequestHeader.Cookie, "JSESSIONID=88E39FF3D8A2749C1FCB902C22A00306");
            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("AdEnable", "true");
            reqparm.Add("domain", "1");
            reqparm.Add("DOMAIN_NAME", "SEEKASIA");
            reqparm.Add("dynamicUserAddition_status", "true");
            reqparm.Add("j_password", password);
            reqparm.Add("j_username", username);
            reqparm.Add("LDAPEnable", "No");
            reqparm.Add("localAuthEnable", "true");
            reqparm.Add("LocalAuthWithDomain", "SEEKASIA");
            reqparm.Add("loginButton", "Login");
            reqparm.Add("logonDomainName", "SEEKASIA");
            byte[] responsebytes1 = wc.UploadValues("https://helpdesk.seekasia.com/j_security_check", "POST", reqparm);
        }

        private static string getUrlFromOnClick(string clickEvent)
        {
            //openInParent('url',true)
            var removeLeft = clickEvent.Substring(14);
            return removeLeft.Substring(0, removeLeft.Length - 7);
        }

        private static ReportSummary getDataFromMEWeb(Site site)
        {
            ReportSummary reportSummary = new ReportSummary();
            HtmlAgilityPack.HtmlDocument list_doc = new HtmlAgilityPack.HtmlDocument();
            using (CookieAwareWebClient wc = new CookieAwareWebClient())
            {
                login(wc);
                var reqparm1 = new System.Collections.Specialized.NameValueCollection();
                byte[] responsebytes = wc.UploadValues(string.Format(reportDataUrl, site.ID), "POST", reqparm1);
                string responsebody = Encoding.Default.GetString(responsebytes);
                list_doc.LoadHtml(responsebody);

                ////=======================================
                HtmlNodeCollection nodes = list_doc.DocumentNode.SelectNodes("//table[@class='dataCountTable']//td[@class='alignLeft col1']");
                if (nodes == null)
                {
                    
                }
                foreach (HtmlNode node in nodes)
                {
                    Report report = new Report();
                    report.Name = node.InnerText;
                    report.Open = node.NextSibling.NextSibling.InnerText.Trim();
                    report.OnHold = node.NextSibling.NextSibling.NextSibling.NextSibling.InnerText.Trim();
                    report.OverDue = node.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.InnerText.Trim();
                    if (!report.Open.StartsWith("0"))
                    {
                        HtmlNode listNode = node.NextSibling.NextSibling.SelectSingleNode("./a");
                        report.OpenUrl = "https://helpdesk.seekasia.com" + getUrlFromOnClick(listNode.Attributes["onclick"].Value);
                    }
                    if (!report.OnHold.StartsWith("0"))
                    {
                        HtmlNode listNode = node.NextSibling.NextSibling.NextSibling.NextSibling.SelectSingleNode("./a");
                        report.OnHoldUrl = "https://helpdesk.seekasia.com" + getUrlFromOnClick(listNode.Attributes["onclick"].Value);
                    }
                    if (!report.OverDue.StartsWith("0"))
                    {
                        HtmlNode listNode = node.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.SelectSingleNode("./a");
                        report.OverDueUrl = "https://helpdesk.seekasia.com" + getUrlFromOnClick(listNode.Attributes["onclick"].Value);
                    }

                    if (node.InnerText.IndexOf("Unassigned") != -1 && !report.Open.StartsWith("0"))
                    {
                        reportSummary.UnAssignedTaskListUrl = report.OpenUrl;
                    }
                    reportSummary.Report.Add(report);
                }

                if (!string.IsNullOrEmpty(reportSummary.UnAssignedTaskListUrl))
                {
                    var reqparm2 = new System.Collections.Specialized.NameValueCollection();
                    //reqparm.Add("h_page", pageindex.ToString());
                    byte[] responsebytes1 = wc.UploadValues(reportSummary.UnAssignedTaskListUrl, "POST", reqparm2);
                    string responsebody1 = Encoding.Default.GetString(responsebytes1);
                    list_doc.LoadHtml(responsebody1);
                    HtmlNodeCollection detailNodes = list_doc.DocumentNode.SelectNodes("//table[@id='RequestsView_TABLE']//a[@rel='uitooltip-track']");
                    foreach (HtmlNode node in detailNodes)
                    {
                        if (site.Name != "Hirer Experience Sycee" || (site.Name == "Hirer Experience Sycee" && node.ParentNode.ParentNode.InnerText.IndexOf("Acquisiti") != -1))
                        {
                            Task task = new Task();
                            StringBuilder sbDetail = new StringBuilder();
                            task.Url = HttpUtility.UrlEncode("https://helpdesk.seekasia.com/" + node.Attributes["href"].Value);
                            task.Message = HttpUtility.UrlEncode(node.InnerText.Trim());
                            reportSummary.Task.Add(task);
                        }
                    }
                }
            }

            return reportSummary;
        }

        private static void mainAction()
        {
            string sFilePath = AppDomain.CurrentDomain.BaseDirectory + "/msg_{0}.log";
            Hashtable htTeamInfo = (Hashtable)ConfigurationManager.GetSection("TeamInfo");
            var listSite = getSite(htTeamInfo);
            foreach (Site site in listSite)
            {
                ReportSummary reportSummary = getDataFromMEWeb(site);

                StringBuilder sb = new StringBuilder();
                string text = "Please have a look your ME!!!";
                string color = "%23ff0000";
                string attachments = "";
                string link = "http://slack.com/api/chat.postMessage?token={0}&channel={1}&text={3}&attachments={2}";
                string detailAttachments = "";
                StringBuilder sb_name = new StringBuilder();
                foreach (Report report in reportSummary.Report)
                {
                    sb_name.Append("{\"text\":\"" + report.Name + "\",\"value\":\"" + report.Name + "\"},");
                    if (report.Open.StartsWith("0") && report.OnHold.StartsWith("0") && report.OverDue.StartsWith("0"))
                    {
                        text = "You are good";
                        color = "%237CD197";
                    }
                    else
                    {
                        text = "Please have a look your ME!!!";
                        color = "%23ff0000";
                    }
                    attachments = string.Format(attachmentsTemplate, report.Name, "open:" + report.Open + " | On Hold:" + report.OnHold + " | OverDue:" + report.OverDue, text, color);
                    sb.Append("{" + attachments + "},");
                }
                string message = sb.ToString().Trim();
                string content = "";
                string filePath = string.Format(sFilePath, site.Name);
                if (File.Exists(filePath))
                {
                    content = File.ReadAllText(filePath, Encoding.UTF8);
                }
                if (content.Equals(message, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }
                File.WriteAllText(filePath, message);
                string taskLink = string.Format(link, token, site.Channel, "[" + message.Trim(',') + "]", "@here , This is ME notification");
                sendSlackMessage(taskLink);

                //send detail link:
                if (string.IsNullOrEmpty(reportSummary.UnAssignedTaskListUrl))
                {
                    continue;
                }

                foreach (Task task in reportSummary.Task)
                {
                    StringBuilder sbDetail = new StringBuilder();
                    detailAttachments = string.Format(attachmentsDetailTemplate, task.Message, task.Url, task.Url + "\nIf you doing that, please check it!");
                    sbDetail.Append("{" + detailAttachments + "},");
                    string detailPostLink = string.Format(link, token, site.Channel, "[" + sbDetail.ToString().Trim(',') + "]", "Hi team, Who handle this ME");
                    sendSlackMessage(detailPostLink);
                }
            }
        }

        private static IList<Site> getSite(Hashtable htTeamInfo)
        {
            var sites = new ArrayList(htTeamInfo.Keys);

            XDocument xdoc = XDocument.Load("site.xml");
            var listSite = from x in xdoc.Descendants("ul").Descendants("li").Descendants("a")
                           where sites.Contains(x.Attribute("title").Value)
                           select new Site
                           {
                               ID = x.Attribute("href").Value,
                               Name = x.Attribute("title").Value,
                               Channel = htTeamInfo[x.Attribute("title").Value].ToString()
                           };

            return listSite.ToList();
        }
    }

}
