using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using WebApplication3.Models;
using WebApplication3.Helpers;
using System.IO;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApplication3.Controllers
{
    [Route("api/[controller]")]
    public class ImagesController : Controller
    {
        private readonly AzureStorageConfig storageConfig = null;

        public ImagesController(IOptions<AzureStorageConfig> config)
        {
            storageConfig = config.Value;

        }
        //Post/api/images/upload
        [HttpPost("[action]")]
        public async Task<IActionResult> Upload(ICollection<IFormFile> files)
        {
            bool isUploaded = false;
            try
            {
                if (files.Count == 0)
                {
                    return BadRequest("No files received from the upload");
                }
                if (storageConfig.ImageContainer == string.Empty)
                {
                    return BadRequest("Please provide a name for your image container in the azure blob storage ");
                }
                foreach (var formfile in files)
                {
                    if (StorageHelpers.IsImage(formfile))
                    {
                        if (formfile.Length > 0)
                        {
                            using (Stream stream = formfile.OpenReadStream())
                            {
                                isUploaded = await StorageHelpers.UploadFileTorStorage(stream, formfile.FileName, storageConfig);
                            }
                        }
                    }
                    else
                    {
                        return new UnsupportedMediaTypeResult();
                    }
                }
                if (isUploaded)
                {
                    if (storageConfig.ThumbnailContainer != string.Empty)
                    {
                        return new AcceptedAtActionResult("GetThumbNails", "Images", null, null);
                    }
                    else
                        return new AcceptedResult();
                }
                else
                    return BadRequest("Look like the image couldn't upload to the storage");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("thumbnails")]
        public async Task<IActionResult> GetThumbnails()
        {
            try
            {
                if (storageConfig.AccountKey == string.Empty || storageConfig.AccountName == string.Empty)
                    return BadRequest("Sorry, can't retrieve your azure storage details from appsetttings.js make sre that you add azure storage details there");
                if (storageConfig.ImageContainer == string.Empty)

                    return BadRequest("Please provide a name for your image conatiner in the azure blob storage");

                List<string> thumbnails = await StorageHelpers.GetThumnailsUrls(storageConfig);
                return new ObjectResult(thumbnails);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
