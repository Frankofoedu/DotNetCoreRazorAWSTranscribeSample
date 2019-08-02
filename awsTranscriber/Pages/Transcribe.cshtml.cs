using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace awsTranscriber.Pages
{
    public class TranscribeModel : PageModel
    {
        //inject services into constructor
        public TranscribeModel(IAmazonS3 client, IAmazonTranscribeService transcribeService)
        {
            _client = client;
            _transcribeService = transcribeService;
        }

        public readonly IAmazonS3 _client;
        public readonly IAmazonTranscribeService _transcribeService;
        public readonly string bucketname = "aws-transcriber";
        [BindProperty]
        public IFormFile Image { get; set; }
        public string Error { get; set; }
        [BindProperty]
        public string TranscribedText { get; set; }

      //  public List<string> s3Keys;


        public void OnGet()
        {

        }

        // [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
        public async Task OnPostAsync()
        {

            try
            {
                //create bucket
                await CreateBucket(_client, bucketname);

                var mp3Path = Image.FileName;

                var mp3Length = new Mp3FileReader(mp3Path).TotalTime;

                DoTrim(mp3Path, mp3Length);

                var files = Directory.GetFiles(Path.GetDirectoryName(mp3Path));
                var sb = new StringBuilder();
                foreach (var item in files)
                {
                    string s3Key = Path.GetFileName(item);
                    await UploadFile(item,s3Key);
                   string transcriptUri = await TranscribeFile(s3Key, bucketname, $"{s3Key}--{DateTime.Now.Ticks.ToString()}");

                    sb.Append(await GetTranscript(transcriptUri));

                }

                TranscribedText = sb.ToString();
                //delete trimmed files if they exist
                DeleteFiles(files);               
            }
            catch (AmazonS3Exception e)
            {
                Error = e.Message;
                // throw;
            }
            catch (AmazonTranscribeServiceException e)
            {
                Console.WriteLine(e.Message);

            }
            catch (Exception e)
            {
                Error = e.Message;
                // return Page();
            }


        }
        /// <summary>
        /// Creates storage bucket/container on aws S3 service
        /// </summary>
        /// <param name="client">AWS S3 client</param>
        /// <param name="bucketname">AWS bucket name</param>
        /// <returns></returns>
        private async Task CreateBucket(IAmazonS3 client, string bucketname)
        {

            //check if s3 bucket exists
            if (await AmazonS3Util.DoesS3BucketExistV2Async(client, bucketname) == false)
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketname,
                    BucketRegion = S3Region.USW1
                };

                var response = client.PutBucketAsync(putBucketRequest);
               // var t = response.IsCompletedSuccessfully;
            }

        }

       /// <summary>
       /// Upload file to S3
       /// </summary>
       /// <param name="file">Form file</param>
       /// <param name="s3Key">File name</param>
       /// <returns></returns>
        private async Task UploadFile(string file, string s3Key)
        {         
            //create file transfer utility
            var ftUtils = new TransferUtility(_client);

            //uploads file to s3 bucket
            await ftUtils.UploadAsync(file, bucketname,s3Key);

        }

        /// <summary>
        /// MEthod to transcribe files stored on s3
        /// </summary>
        /// <param name="s3Key"></param>
        /// <param name="s3Bucket"></param>
        /// <param name="region"></param>
        /// <param name="job_name"></param>
        /// <returns>Location of the transcription online</returns>
        private async Task<string> TranscribeFile(string s3Key,string s3Bucket, string job_name)
        {
            var job_uri = $"https://s3.{RegionEndpoint.USWest2.SystemName}.amazonaws.com/{s3Bucket}/{s3Key}";

            var request = new StartTranscriptionJobRequest()
            {
                Media = new Media { MediaFileUri = job_uri },
                TranscriptionJobName = job_name,
                MediaFormat = MediaFormat.Mp3,
                LanguageCode = LanguageCode.EnUS,
                Settings = new Settings { ShowSpeakerLabels = true, MaxSpeakerLabels = 2 }
            };

            await _transcribeService.StartTranscriptionJobAsync(request);

            //var t = await   _transcribeService.ListTranscriptionJobsAsync(new ListTranscriptionJobsRequest());

            while (true)
            {
                var status = _transcribeService.
                    GetTranscriptionJobAsync(new GetTranscriptionJobRequest { TranscriptionJobName = job_name });

                if (status.Result.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED)
                {
                    return status.Result.TranscriptionJob.Transcript.TranscriptFileUri;
                }
                else if(status.Result.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.FAILED)
                {
                    throw new AmazonTranscribeServiceException("Transcribing failed. Please contact server admin");
                }
            }
        }
        /// <summary>
        /// Gets transcript after it is completed
        /// </summary>
        /// <param name="uri"> Transcription job uri</param>
        /// <returns></returns>
        private async Task<string> GetTranscript(string uri)
        {
            var transcriptionDocument = await new HttpClient().GetStringAsync(uri);

            var root = JsonConvert.DeserializeObject(transcriptionDocument) as JObject;

            var sb = new StringBuilder();

            foreach (JObject transcriptionNode in root["results"]["transcripts"])
            {
                if (sb.Length != 0)
                {
                    sb.AppendLine("\n\n");
                }
                sb.Append(transcriptionNode["transcript"]);
            }

            return sb.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        static void TrimMp3(string inputPath, string outputPath, TimeSpan? begin, TimeSpan? end)
        {
            if (begin.HasValue && end.HasValue && begin > end)
                throw new ArgumentOutOfRangeException("end", "end should be greater than begin");

            using (var reader = new Mp3FileReader(inputPath))
            {
                using (var writer = System.IO.File.Create(outputPath)) {

                    Mp3Frame frame;
                    while ((frame = reader.ReadNextFrame()) != null)
                        if (reader.CurrentTime >= begin || !begin.HasValue)
                        {
                            if (reader.CurrentTime <= end || !end.HasValue)
                                writer.Write(frame.RawData, 0, frame.RawData.Length);
                            else break;
                        }
                };
            } ;

           
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="length"></param>
        static void DoTrim(string inputPath, TimeSpan length)
        {
           

            //AWS transcribe maximum length of audio to pass
            var skip = new TimeSpan(0, 60, 0);

            

            if (length > skip)
            {
                var t = Convert.ToInt32(length.Ticks / skip.Ticks);

                var begin = new TimeSpan();

                for (int i = 0; i < t; i++)
                {

                    var end = (i + 1) * skip;
                    //do work
                    var outputPath = Path.ChangeExtension(inputPath, $".trimmed--{DateTime.Now.Ticks.ToString()}");
                    TrimMp3(inputPath, outputPath, begin, end);
                    //lis.Add(Path.GetFileName(outputPath));
                    begin = end;
                }

                var remaninder = length - (Convert.ToInt32(t) * skip);

                if (remaninder != new TimeSpan(0, 0, 0))
                {
                    var outputPath = Path.ChangeExtension(inputPath, $".trimmed--{DateTime.Now.Ticks.ToString()}");
                    TrimMp3(inputPath, outputPath, length - remaninder, length);
                  //  lis.Add(Path.GetFileName(outputPath));
                }

            }


        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        private static void DeleteFiles(string[] x)
        {
            foreach (var item in x)
            {
                if (!item.Contains("trimmed"))
                    continue;
                else
                    System.IO.File.Delete(item);
            }
        }


    }
}