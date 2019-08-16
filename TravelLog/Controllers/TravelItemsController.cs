using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using TravelLog.Helpers;
using TravelLog.Models;

namespace TravelLog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TravelItemsController : ControllerBase
    {
        private readonly TravelLogContext _context;
        private IConfiguration _configuration;

        public TravelItemsController(TravelLogContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/TravelItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TravelItems>>> GetTravelItems()
        {
            return await _context.TravelItems.ToListAsync();
        }

        // GET: api/TravelItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TravelItems>> GetTravelItems(int id)
        {
            var travelItems = await _context.TravelItems.FindAsync(id);

            if (travelItems == null)
            {
                return NotFound();
            }

            return travelItems;
        }

        // PUT: api/TravelItems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTravelItems(int id, TravelItems travelItems)
        {
            if (id != travelItems.Id)
            {
                return BadRequest();
            }

            _context.Entry(travelItems).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TravelItemsExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TravelItems
        [HttpPost]
        public async Task<ActionResult<TravelItems>> PostTravelItems(TravelItems travelItems)
        {
            _context.TravelItems.Add(travelItems);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTravelItems", new { id = travelItems.Id }, travelItems);
        }

        // DELETE: api/TravelItems/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<TravelItems>> DeleteTravelItems(int id)
        {
            var travelItems = await _context.TravelItems.FindAsync(id);
            if (travelItems == null)
            {
                return NotFound();
            }

            _context.TravelItems.Remove(travelItems);
            await _context.SaveChangesAsync();

            return travelItems;
        }

        private bool TravelItemsExists(int id)
        {
            return _context.TravelItems.Any(e => e.Id == id);
        }

        // GET: api/Travel/Tags

        [HttpGet]
        [Route("tag")]
        public async Task<List<TravelItems>> GetTagsItem([FromQuery] string tags)
        {
            var travels = from m in _context.TravelItems
                        select m; //get all the travelitems


            if (!String.IsNullOrEmpty(tags)) //make sure user gave a tag to search
            {
                travels = travels.Where(s => s.Tags.ToLower().Contains(tags.ToLower()) || s.Title.ToLower().Contains(tags.ToLower())); // find the entries with the search tag and reassign
            }

            var returned = await travels.ToListAsync(); //return the memes

            return returned;
        }

        [HttpPost, Route("upload")]
        public async Task<IActionResult> UploadFile([FromForm]TravelImageItems meme)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            try
            {
                using (var stream = meme.Image.OpenReadStream())
                {
                    var cloudBlock = await UploadToBlob(meme.Image.FileName, null, stream);
                    //// Retrieve the filename of the file you have uploaded
                    //var filename = provider.FileData.FirstOrDefault()?.LocalFileName;
                    if (string.IsNullOrEmpty(cloudBlock.StorageUri.ToString()))
                    {
                        return BadRequest("An error has occured while uploading your file. Please try again.");
                    }

                    TravelItems travelItems = new TravelItems();
                    travelItems.Title = meme.Title;
                    travelItems.Tags = meme.Tags;

                    System.Drawing.Image image = System.Drawing.Image.FromStream(stream);
                    travelItems.Height = image.Height.ToString();
                    travelItems.Width = image.Width.ToString();
                    travelItems.Url = cloudBlock.SnapshotQualifiedUri.AbsoluteUri;
                    travelItems.Uploaded = DateTime.Now.ToString();

                    _context.TravelItems.Add(travelItems);
                    await _context.SaveChangesAsync();

                    return Ok($"File: {meme.Title} has successfully uploaded");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"An error has occured. Details: {ex.Message}");
            }


        }

        private async Task<CloudBlockBlob> UploadToBlob(string filename, byte[] imageBuffer = null, System.IO.Stream stream = null)
        {

            var accountName = _configuration["AzureBlob:name"];
            var accountKey = _configuration["AzureBlob:key"]; ;
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer imagesContainer = blobClient.GetContainerReference("images");

            string storageConnectionString = _configuration["AzureBlob:connectionString"];

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    // Generate a new filename for every new blob
                    var fileName = Guid.NewGuid().ToString();
                    fileName += GetFileExtention(filename);

                    // Get a reference to the blob address, then upload the file to the blob.
                    CloudBlockBlob cloudBlockBlob = imagesContainer.GetBlockBlobReference(fileName);

                    if (stream != null)
                    {
                        await cloudBlockBlob.UploadFromStreamAsync(stream);
                    }
                    else
                    {
                        return new CloudBlockBlob(new Uri(""));
                    }

                    return cloudBlockBlob;
                }
                catch (StorageException ex)
                {
                    return new CloudBlockBlob(new Uri(""));
                }
            }
            else
            {
                return new CloudBlockBlob(new Uri(""));
            }

        }

        private string GetFileExtention(string fileName)
        {
            if (!fileName.Contains("."))
                return ""; //no extension
            else
            {
                var extentionList = fileName.Split('.');
                return "." + extentionList.Last(); //assumes last item is the extension 
            }
        }


    }
}
