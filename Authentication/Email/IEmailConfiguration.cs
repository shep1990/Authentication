using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Authentication.Email
{
    public interface IEmailConfiguration
    {
        string Host { get; }
        int Port { get; }
        string Username { get; set; }
        string Password { get; set; }
    }
}
