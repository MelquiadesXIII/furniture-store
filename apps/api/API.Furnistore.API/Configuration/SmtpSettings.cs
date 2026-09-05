using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Furnistore.API.Configuration
{
    public class SmtpSettings
    {
        public string Server { get; set; }

        public int Port { get; set; }

        public string SenderEmail { get; set; }

        public string SenderName { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }
    }
}