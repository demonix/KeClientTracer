﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogProcessors.Caches;
using LogProcessors.CertificateHelper;
using LogProcessors.TokenHelper;
using LogReader;

namespace LogProcessors
{
    public class KeFrontLogProcessor
    {
        NginxLogLine nginxLogLine = new NginxLogLine();

        public bool Process(string line, out string meta, out string requestData, out string error)
        {
            bool result = true;
            meta = null;
            requestData = null;
            error = null;
            nginxLogLine.FillFromString(line);
            

            //Console.WriteLine("on msg");
            //Console.ReadKey();););
            if (String.IsNullOrEmpty(nginxLogLine.Token))
            {
                error = "no token in request";
            }
            Token token = null;
            OrganizationCertificateDescription ocd = null;
            if (!String.IsNullOrEmpty(nginxLogLine.Token))
                try
                {
                    token = TokenCache2.Get(nginxLogLine.Token);
                    ocd = CertificateCache2.Get(token.Thumbprint);
                    if (ocd == null)
                    {
                        error = "cannot find cert with thumprint = [" + token.Thumbprint + "]";
                    }
                }
                catch (Exception ex)
                {
                    error = String.Format("Error while parsing token {0}\r\n", nginxLogLine.Token);
                    if (token !=null)
                        error += String.Format("A:{0}\r\nU:{1}\r\nTh:{2};\r\nDt:{3}\r\n",token.Abon,token.User,token.Thumbprint,token.ValidTo);
                    error += ex.ToString();
                    result = false;
                }


            meta = String.Format("{0:dd.MM.yyyy}\t{1}\t{2}\t{3}\t{4}",
                                         nginxLogLine.RequestDateTime.ToLocalTime(),
                                         nginxLogLine.Host,
                                         nginxLogLine.ClientIP,
                                         ocd == null? "" : ocd.OrganizationId,
                                         nginxLogLine.SessionId);
            requestData =
                String.Format(
                    //"{0:dd.MM.yyyy H:mm:ss.fff zzz}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                    "{0:HH:mm:ss.fff K}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                    nginxLogLine.RequestDateTime.ToLocalTime(),
                    nginxLogLine.Method,
                    nginxLogLine.Uri,
                    nginxLogLine.QueryString,
                    nginxLogLine.Result,
                    nginxLogLine.TimeTaken,
                    nginxLogLine.Sid,
                    nginxLogLine.Backend);
            return result;
        }
    }
}
