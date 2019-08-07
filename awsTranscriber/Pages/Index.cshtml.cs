using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.TranscribeService;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Amazon.TranscribeService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace awsTranscriber.Pages
{
   
    public class IndexModel : PageModel
    {
        private readonly IHostingEnvironment _env;

        public IndexModel(IHostingEnvironment env)
        {
            _env = env;
        }
        public void OnGet()
        {
            
        }

    }
}
