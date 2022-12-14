// © Microsoft Corporation. All rights reserved.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CallAutomation
{
    public class BlobSettingsController : Controller
    {
        private readonly string _blobSasUri;
        private readonly string _blobContainerName;
        private readonly string _isFeedbackEnabled;

        public BlobSettingsController(IConfiguration configuration)
        {
            _blobSasUri = configuration["LogBlobSasUri"];
            _blobContainerName = configuration["LogBlobContainerName"];
            _isFeedbackEnabled = configuration["IsFeedbackEnabled"];
        }

        /// <summary>
        /// Gets Blob settings for log service
        /// </summary>
        /// <returns></returns>
        [Route("/feedbackSettings")]
        [HttpGet]
        public IActionResult Get()
        {
            var clientResponse = new
            {
                isFeedbackEnabled = _isFeedbackEnabled,
                sasUri = _blobSasUri,
                containerName = _blobContainerName,
            };

            return this.Ok(clientResponse);
        }
    }
}
