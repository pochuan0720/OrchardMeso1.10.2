using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Http;
using System.Threading.Tasks;
using Orchard.ContentManagement;
using Orchard.MediaLibrary.Services;
using Orchard.MediaLibrary.ViewModels;
using Orchard.MediaLibrary.Models;
using Orchard.Localization;
using System.Linq;
using Orchard.FileSystems.Media;
using Orchard.Logging;
using Orchard.Core.Common.ViewModels;
using System.Net;
using System.Net.Http;
using Orchard.UI.Navigation;
using Orchard.Settings;

namespace Orchard.MediaLibrary.Controllers {

    [Authorize]
    public class ClientStorageApiController : ApiController {
        private readonly IMediaLibraryService _mediaLibraryService;
        private readonly IMimeTypeProvider _mimeTypeProvider;
        private readonly ISiteService _siteService;

        public ClientStorageApiController(
            IMediaLibraryService mediaManagerService,
            IOrchardServices orchardServices,
            IMimeTypeProvider mimeTypeProvider,
            ISiteService siteService) {
            _mediaLibraryService = mediaManagerService;
            _mimeTypeProvider = mimeTypeProvider;
            Services = orchardServices;
            _siteService = siteService;
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }


        [HttpPost]
        public IHttpActionResult query(MediaManagerMediaItemsApiViewModel inModel)
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageOwnMedia, T("Cannot view media")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Cannot view media" });


            if(!string.IsNullOrEmpty(inModel.FolderPath))
                inModel.FolderPath = Path.Combine(inModel.FolderPath.Split('/'));

            // Check permission
            if (!Services.Authorizer.Authorize(Permissions.ManageMediaContent) && !_mediaLibraryService.CanManageMediaFolder(inModel.FolderPath))
            {
                var model = new MediaManagerMediaItemsViewModel
                {
                    MediaItems = new List<MediaManagerMediaItemViewModel>(),
                    MediaItemsCount = 0,
                    FolderPath = inModel.FolderPath
                };

                return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            Pager pager = null;
            var mediaPartsCount = _mediaLibraryService.GetMediaContentItemsCount(inModel.FolderPath, "", VersionOptions.Latest);

            if (inModel != null && inModel.Pager != null)
                pager = new Pager(_siteService.GetSiteSettings(), inModel.Pager.Page, inModel.Pager.PageSize, mediaPartsCount);

            IList<MediaPart> mediaParts;

            if (pager != null)
            {
                mediaParts = _mediaLibraryService.GetMediaContentItems(inModel.FolderPath, inModel.Pager.GetStartIndex(), inModel.Pager.PageSize, "created", "", VersionOptions.Latest).ToArray();
                pager.PageSize = mediaParts.Count;
            }
            else
                mediaParts = _mediaLibraryService.GetMediaContentItems(inModel.FolderPath, "created", "", VersionOptions.Latest).ToArray();


            var viewModel = new MediaManagerMediaItemsApiViewModel
            {
                FolderPath = inModel.FolderPath,
                Data = mediaParts.ToList<MediaPart>(),
                Pager = pager
            };

            return Ok(new ResultViewModel { Content = viewModel, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }


        public IHttpActionResult index(string folderPath, string type, int? replaceId = null) {
            if (!Services.Authorizer.Authorize(Permissions.ManageOwnMedia)) {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "" });
            }

            // Check permission
            if (!Services.Authorizer.Authorize(Permissions.ManageMediaContent) && !_mediaLibraryService.CanManageMediaFolder(folderPath)) {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not allowed to manage Media" });
            }

            var viewModel = new ImportMediaViewModel {
                FolderPath = folderPath,
                Type = type,
            };

            if (replaceId != null) {
                var replaceMedia = Services.ContentManager.Get<MediaPart>(replaceId.Value);
                if (replaceMedia == null)
                    return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

                viewModel.Replace = replaceMedia;
            }

            return Ok(new ResultViewModel { Content = viewModel, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public async Task<IHttpActionResult> upload(string folderPath) {
            if (!Services.Authorizer.Authorize(Permissions.ManageOwnMedia)) {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "" });
            }

            //string folderPath = "";

            if (!string.IsNullOrEmpty(folderPath))
            {
                folderPath = Path.Combine(folderPath.Split('/'));
            }

            // Check permission
            if (!Services.Authorizer.Authorize(Permissions.ManageMediaContent) && !_mediaLibraryService.CanManageMediaFolder(folderPath)) {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "" });
            }

            var statuses = new List<object>();
            var settings = Services.WorkContext.CurrentSite.As<MediaLibrarySettingsPart>();
            var allowedExtensions = (settings.UploadAllowedFileTypeWhitelist ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.StartsWith("."));

            // Read the form data.
            MultipartMemoryStreamProvider multipartMemoryStreamProvider = null;
            try
            {
                multipartMemoryStreamProvider = await Request.Content.ReadAsMultipartAsync();
            }
            catch (Exception e)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = e.Message });
            }
            // Loop through each file in the request
            foreach (var content in multipartMemoryStreamProvider.Contents)
            {
                // Pointer to file
                //var file = HttpContext.Request.Files[i];
                var filename = content.Headers.ContentDisposition.FileName.Replace("\"","");

                // if the file has been pasted, provide a default name
                if (content.Headers.ContentType.MediaType.Equals("image/png", StringComparison.InvariantCultureIgnoreCase) && !filename.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                {
                    filename = "clipboard.png";
                }

                // skip file if the allowed extensions is defined and doesn't match
                if (allowedExtensions.Any())
                {
                    if (!allowedExtensions.Any(e => filename.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                    {
                        statuses.Add(new
                        {
                            error = T("This file type is not allowed: {0}", Path.GetExtension(filename)).Text,
                            progress = 1.0,
                        });
                        continue;
                    }
                }

                try
                {
                    using (Stream stream = await content.ReadAsStreamAsync())
                    {
                        //byte[] bytes = new byte[stream.Length];
                        //stream.Read(bytes, 0, bytes.Length);
 
                        var mediaPart = _mediaLibraryService.ImportMedia(stream, folderPath, filename, null);
                        Services.ContentManager.Create(mediaPart);

                        statuses.Add(new
                        {
                            Id = mediaPart.Id,
                            Name = mediaPart.Title,
                            Type = mediaPart.MimeType,
                            Size = content.Headers.ContentLength,
                            Progress = 1.0,
                            Url = mediaPart.FileName,
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unexpected exception when uploading a media.");
                    statuses.Add(new
                    {
                        error = T(ex.Message).Text,
                        progress = 1.0,
                    });
                }
            }

            return Ok(new ResultViewModel {Content = statuses, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

        }

        [HttpPost]
        public IHttpActionResult delete(MediaManagerEditApiViewModel inModel)
        {

            if (!Services.Authorizer.Authorize(Permissions.ManageOwnMedia, T("Couldn't delete media items")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete media items" });

            if(inModel == null || inModel.Ids == null || inModel.Ids.Length<=0)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var mediaItems = Services.ContentManager
                .Query(VersionOptions.Latest)
                .ForContentItems(inModel.Ids)
                .List()
                .Select(x => x.As<MediaPart>())
                .Where(x => x != null);

            if (mediaItems.Count() <= 0)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var statuses = new List<object>();

            try
            {
                foreach (var media in mediaItems)
                {
                    if (_mediaLibraryService.CanManageMediaFolder(media.FolderPath))
                    {
                        Services.ContentManager.Remove(media.ContentItem);
                        statuses.Add(new
                        {
                            Id = media.Id
                        });
                    }
                }

                return Ok(new ResultViewModel { Content = statuses, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not delete media items.");
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.OK.ToString("d"), Message = "Could not delete media items" });
            }

        }
            
    }
}