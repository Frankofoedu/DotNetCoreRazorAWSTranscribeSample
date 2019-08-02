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

namespace awsTranscriber.Pages
{
   
    public class IndexModel : PageModel
    {
        public readonly IAmazonS3 _client;
        public readonly IAmazonTranscribeService _transcribeService;
        public readonly string bucketname = "aws-transcriber";
        public IndexModel(IAmazonS3 client, IAmazonTranscribeService transcribeService)
        {
            _client = client;
            _transcribeService = transcribeService;
        }
        public async void OnGetAsync()
        {
           // await CreateBucket();
           // await TranscribeFile();
        }

        private async Task UploadFile()
        {
            try
            {
                string filename = "InboundSampleRecording.mp3";
                var ftUtils = new TransferUtility(_client);

                await ftUtils.UploadAsync(filename, bucketname);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

       private async Task TranscribeFile()
        {
            var job_uri = "https://aws-transcriber.s3.us-west-2.amazonaws.com/InboundSampleRecording.mp3";
            var job_name = "aws-transcribe2";
            try
            {
                
                var request = new StartTranscriptionJobRequest()
                {
                    Media = new Media { MediaFileUri = job_uri },
                    TranscriptionJobName = job_name,
                    MediaFormat = MediaFormat.Mp3,
                    LanguageCode = LanguageCode.EnUS,
                    Settings = new Settings { ShowSpeakerLabels = true, MaxSpeakerLabels = 2}
                };
                var response =   await _transcribeService.StartTranscriptionJobAsync(request);
                
             //var t = await   _transcribeService.ListTranscriptionJobsAsync(new ListTranscriptionJobsRequest());

                while (true)
                {
                    var status = _transcribeService.
                        GetTranscriptionJobAsync(new GetTranscriptionJobRequest { TranscriptionJobName = job_name });
                    


                    if (status.Result.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED)
                    {
                        Console.WriteLine(status.Result.TranscriptionJob.Transcript.TranscriptFileUri);
                        break;
                    }
                        
                   
                }
            }
            catch (AmazonTranscribeServiceException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private async Task CreateBucket()
        {
            try
            {
                if (await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketname) == false)
                {
                    var putBucketRequest = new PutBucketRequest
                    {
                        BucketName = bucketname,
                        BucketRegion = S3Region.USW1
                    };

                    var response = _client.PutBucketAsync(putBucketRequest);
                    var t = response.IsCompletedSuccessfully;
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
}
