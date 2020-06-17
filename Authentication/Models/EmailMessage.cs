using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Authentication.Models
{
	public class EmailMessage
	{
		public EmailAddress ToAddresses { get; set; }
		public EmailAddress FromAddresses { get; set; }
		public string Subject { get; set; }
		public string Content { get; set; }
	}
}
