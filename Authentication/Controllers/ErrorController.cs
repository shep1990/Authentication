using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(ErrorController));

        public IActionResult Exception()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            _logger.Error(exceptionFeature.Error);
            _logger.Error(exceptionFeature.Path);
            return View();
        }
    }
}