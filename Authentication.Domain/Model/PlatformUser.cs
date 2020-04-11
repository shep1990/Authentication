using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Authentication.Domain.Model
{
    public class PlatformUser : IdentityUser<Guid>
    {
    }
}
