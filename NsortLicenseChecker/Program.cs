using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Common;
using S22.Imap;

namespace NsortLicenseChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            Settings settings = Settings.GetInstance();
            var imapServer = settings.TryGetValue("ImapServer");
            var imapUserName = settings.TryGetValue("ImapUserName");
            var imapPassword = settings.TryGetValue("ImapPassword");
            var nsortPath = settings.TryGetValue("NsortPath");
            var nsortLicenseFilePath = settings.TryGetValue("NsortLicenseFilePath");
            if (String.IsNullOrEmpty(imapServer) || String.IsNullOrEmpty(imapUserName) ||
                String.IsNullOrEmpty(imapPassword) || String.IsNullOrEmpty(nsortPath))
            {
                throw new Exception("Не задан нужный параметр, догадайся, какой =)");
            }
            //ProcessStartInfo nsortProcessStartInfo = new ProcessStartInfo("echo a | \"" + nsortPath + "\"");
            ProcessStartInfo nsortProcessStartInfo = new ProcessStartInfo("cmd.exe");
            nsortProcessStartInfo.Arguments = "/c echo a | \"" + nsortPath + "\"";
            nsortProcessStartInfo.RedirectStandardOutput = true;
            nsortProcessStartInfo.RedirectStandardError = true;
            nsortProcessStartInfo.UseShellExecute = false;
            Process nsortProcess = new Process();
            nsortProcess.StartInfo = nsortProcessStartInfo;
            
            nsortProcess.Start();
            nsortProcess.WaitForExit();
            string outputText = nsortProcess.StandardOutput.ReadToEnd();
            outputText += nsortProcess.StandardError.ReadToEnd();

            string newLicenseInfo = "";
            string errorText = "";
            if (outputText.Contains("LICENSE_FAILURE"))
            {
                string[] outputLines = outputText.Split('\r', '\n');
                foreach (var line in outputLines)
                {
                    if (line.StartsWith("New license info for"))
                        newLicenseInfo = line;
                }
                if (String.IsNullOrEmpty(newLicenseInfo))
                {
                    SendMailNotification("Лицензия протухла, но не найдена информация для запроса новой лицензии. Вывод nsort'а: \r\n" + outputText);
                }
                else
                {
                    var license = GetNewLicense(newLicenseInfo);
                    if (!String.IsNullOrEmpty(license))
                    {
                        File.WriteAllText(nsortLicenseFilePath,license);
                        SendMailNotification("Обновлена лицензия на nsort:\r\n" + license);
                    }
                    
                }
            }
            else
            {
                return;
            }
        }

        private static string GetNewLicense(string newLicenseInfo)
        {
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.ordinal.com/temporary.cgi");
            request.Method = "POST";
            StreamWriter requestStreamWriter = new StreamWriter(request.GetRequestStream());
            string requestData = "ErrMsg=" + HttpUtility.UrlEncode(newLicenseInfo) +
                                 "&Name=Name&Email=kit_nsort@mail.ru&Phone=111&Company=Company&Reason=need+more+time+to+generate+P.O.&Agree=Yes";
            requestStreamWriter.WriteLine(requestData);
            requestStreamWriter.Close();
            request.ContentType = "application/x-www-form-urlencoded";
            request.Referer = "http://www.ordinal.com/temporary.cgi";
            WebResponse response = null;
            try
            {
                response = request.GetResponse();
            }
            catch (Exception ex)
            {
                SendMailNotification("Ошибка при выполении запроса к www.ordinal.com:\r\n"+ex.ToString());
                return "";
            }
            
            Settings settings = Settings.GetInstance();
            var imapServer = settings.TryGetValue("ImapServer");
            var imapUserName = settings.TryGetValue("ImapUserName");
            var imapPassword = settings.TryGetValue("ImapPassword");
            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep(60000);
                using (ImapClient imapCient = new ImapClient(imapServer, 993, imapUserName, imapPassword, AuthMethod.Login, true))
                {
                    var uids = imapCient.Search(SearchCondition.Since(DateTime.Now.AddHours(-6)));
                    List<MailMessage> messages = imapCient.GetMessages(uids).ToList();
                    var neededMessage = messages.Where(m => m.From.Address == "licensing@ordinal.com")
                        .OrderByDescending(m => m.Date()).FirstOrDefault();
                    if (neededMessage != null)
                    {
                        var match = Regex.Match(neededMessage.Body, @"-license=\r\n"".*""", RegexOptions.IgnoreCase | RegexOptions.Multiline).Value;
                        if (String.IsNullOrEmpty(match))
                        {
                            SendMailNotification("Ordinal прислал какую то фигню вместо лицензии:\r\n" + neededMessage.Body);
                            return "";
                        }
                        return match;
                    }
                }
            }
            SendMailNotification("Ordinal ничего не прислал в разумный срок");
            return "";
        }

        
        private static void SendMailNotification(string messageText)
        {
            using (SmtpClient smtpClient = new SmtpClient("mail"))
            {
                MailMessage message = new MailMessage("nsort_updater@sys-msk.kontur-extern.ru", "kit@kontur.ru");
                message.Body = messageText;
                smtpClient.Send(message);
            }
            
            

        }
    }
}
