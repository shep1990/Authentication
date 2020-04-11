using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Authentication.Domain.Model
{
    public class PlatformRole : IdentityRole<Guid>
    {
        public string Description { get; set; }
    }
}
