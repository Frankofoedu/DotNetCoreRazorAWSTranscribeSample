using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace awsTranscriber.Pages.Shared
{

    public class TranscriberModel : PageModel
    {
        private const string bucketName = "*** bucket name ***";
        // For simplicity the example creates two objects from the same file.
        // You specify key names for these objects.
        private const string keyName1 = "*** key name for first object created ***";
        private const string keyName2 = "*** key name for second object created ***";
        private const string filePath = @"*** file path ***";
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.USGovCloudWest1;


        public string Error { get; set; }
        public string Message { get; set; }
        public void OnGet()
        {

        }


        public void UploadImage()
        {
            try
            {


            }
            catch (AmazonS3Exception e)
            {
                ///TODO: Implement logging to file
                Error = $"Error encountered upload server. Message:'{e.Message}' when writing an object";
            }
            catch (Exception e)
            {
                ///TODO: Implement logging to file
                Error = $"Error encountered on server,  Message:'{e.Message}' when writing an object";

            }
        }
           

    
    }
}