using Authentication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Authentication.Email
{
    public interface IEmailService
    {
        void Send(EmailMessage emailMessage);
    }
}
