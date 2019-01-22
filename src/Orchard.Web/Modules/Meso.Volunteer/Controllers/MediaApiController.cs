using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Http;
using System.Threading.Tasks;
using Orchard.ContentManagement;
using Orchard.Localization;
using System.Linq;
using Orchard.FileSystems.Media;
using Orchard.Logging;
using Orchard.Core.Common.ViewModels;
using System.Net;
using System.Net.Http;
using Orchard.UI.Navigation;
using Orchard.Settings;
using Newtonsoft.Json.Linq;
using Orchard.UI.Notify;
using System.Net.Http.Headers;
using Orchard.MediaLibrary.Services;
using Meso.Volunteer.Handlers;
using Orchard;
using Orchard.MediaLibrary.ViewModels;
using Orchard.MediaLibrary.Models;
using Orchard.Security;

namespace Meso.Volunteer.Controllers {

    [Authorize]
    public class MediaApiController : ApiController {
        private readonly IAuthenticationService _authenticationService;
        private readonly IMediaLibraryService _mediaLibraryService;
        private readonly IMimeTypeProvider _mimeTypeProvider;
        private readonly ISiteService _siteService;
        private readonly IMediaUpdateModelHandler _updateModelHandler;

        public MediaApiController(
            IAuthenticationService authenticationService,
            IMediaLibraryService mediaManagerService,
            IOrchardServices orchardServices,
            IMimeTypeProvider mimeTypeProvider,
            ISiteService siteService,
            IMediaUpdateModelHandler updateModelHandler) {
            _authenticationService = authenticationService;
            _mediaLibraryService = mediaManagerService;
            _mimeTypeProvider = mimeTypeProvider;
            Services = orchardServices;
            _siteService = siteService;
            _updateModelHandler = updateModelHandler;
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }


        [HttpPost]
        public IHttpActionResult query(JObject inModel)
        {
            if (inModel == null)
                return BadRequest();

            if (!Services.Authorizer.Authorize(Orchard.MediaLibrary.Permissions.ManageOwnMedia, T("Cannot view media")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Cannot view media" });

            string folderPath = "";
            if(inModel["FolderPath"] != null)
                folderPath = Path.Combine(inModel["FolderPath"].ToString().Split('/'));
            else
                folderPath = _authenticationService.GetAuthenticatedUser().UserName;

            // Check permission
            if (!Services.Authorizer.Authorize(Orchard.MediaLibrary.Permissions.ManageMediaContent) && !_mediaLibraryService.CanManageMediaFolder(folderPath))
            {
                var model = new MediaManagerMediaItemsViewModel
                {
                    MediaItems = new List<MediaManagerMediaItemViewModel>(),
                    MediaItemsCount = 0,
                    FolderPath = folderPath
                };

                return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            Pager pager = null;
            var mediaPartsCount = _mediaLibraryService.GetMediaContentItemsCount(folderPath, "", VersionOptions.Latest);

            if (inModel != null && inModel["Pager"] != null)
            {
                Pager _pager = inModel["Pager"].ToObject<Pager>();
                pager = new Pager(_siteService.GetSiteSettings(), _pager.Page, _pager.PageSize, mediaPartsCount);
            }

            IList<object> mediaParts;

            if (pager != null)
            {
                mediaParts = _mediaLibraryService.GetMediaContentItems(folderPath, pager.GetStartIndex(), pager.PageSize, "created", "", VersionOptions.Latest)
                    .Select(m => GetMediaObject(m)).OrderByDescending(o => o["Position"]).ToArray();
                pager.PageSize = mediaParts.Count;
            }
            else
                mediaParts = _mediaLibraryService.GetMediaContentItems(folderPath, "created", "", VersionOptions.Latest)
                    .Select(m => GetMediaObject(m)).OrderByDescending( o => o["Position"]).ToArray();


            var viewModel = new 
            {
                FolderPath = folderPath,
                Data = mediaParts.ToList<object>(),
                Pager = pager
            };

            return Ok(new ResultViewModel { Content = viewModel, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private int GetFieldValue(object item)
        {
            if(item != null && item.GetType().ToString().Equals("Orchard.Fields.Fields.NumericField"))
            {
                dynamic field = item;
                if (field.Value != null)
                    return Convert.ToInt32(field.Value);
            }

            return 0;
        }
        private JObject GetMediaObject(MediaPart item)
        {
            JObject obj = JObject.FromObject(item);
            obj.Add(new JProperty("Position", GetFieldValue(item.Fields.FirstOrDefault())));
            return obj;
        }


        public IHttpActionResult index(string folderPath, string type, int? replaceId = null)
        {
            if (!Services.Authorizer.Authorize(Orchard.MediaLibrary.Permissions.ManageOwnMedia))
            {
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "" });
            }

            // Check permission
            if (!Services.Authorizer.Authorize(Orchard.MediaLibrary.Permissions.ManageMediaContent) && !_mediaLibraryService.CanManageMediaFolder(folderPath))
            {
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not allowed to manage Media" });
            }

            var viewModel = new ImportMediaViewModel
            {
                FolderPath = folderPath,
                Type = type,
            };

            if (replaceId != null)
            {
                var replaceMedia = Services.ContentManager.Get<MediaPart>(replaceId.Value);
                if (replaceMedia == null)
                    return NotFound();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

                viewModel.Replace = replaceMedia;
            }

            return Ok(new ResultViewModel { Content = viewModel, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public async Task<IHttpActionResult> upload(string folderPath = null) {
            if (!Services.Authorizer.Authorize(Orchard.MediaLibrary.Permissions.ManageOwnMedia)) {
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "" });
            }

            //string folderPath = "";

            if (!string.IsNullOrEmpty(folderPath))
            {
                folderPath = Path.Combine(folderPath.Split('/'));
            }
            else
            {
                folderPath = _authenticationService.GetAuthenticatedUser().UserName;
            }

            // Check permission
            if (!Services.Authorizer.Authorize(Orchard.MediaLibrary.Permissions.ManageMediaContent) && !_mediaLibraryService.CanManageMediaFolder(folderPath)) {
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "" });
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
                return InternalServerError();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = e.Message });
            }
            // Loop through each file in the request
            foreach (HttpContent content in multipartMemoryStreamProvider.Contents)
            {
                // Pointer to file
                //var file = HttpContext.Request.Files[i];

                string name = content.Headers.ContentDisposition.Name;
                string filename = string.IsNullOrEmpty(content.Headers.ContentDisposition.FileName) ? null :  content.Headers.ContentDisposition.FileName.Replace("\"", "");

                if (!string.IsNullOrEmpty(filename))
                {
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
                }


                try
                {
                    using (Stream stream = await content.ReadAsStreamAsync())
                    {
                        //byte[] bytes = new byte[stream.Length];
                        //stream.Read(bytes, 0, bytes.Length);

                        if (!string.IsNullOrEmpty(filename))
                        {
                            var mediaPart = _mediaLibraryService.ImportMedia(stream, folderPath, filename, null);
                            Services.ContentManager.Create(mediaPart);
                            statuses.Add(mediaPart);
                            /*statuses.Add(new
                            {
                                Id = mediaPart.Id,
                                Title = mediaPart.Title,
                                FileName = mediaPart.Title,
                                MimeType = mediaPart.MimeType,
                                //Size = content.Headers.ContentLength,
                                //Progress = 1.0,
                                MediaUrl = mediaPart.MediaUrl
                            });*/
                        }
                        else if(!string.IsNullOrEmpty(name))
                        {
                            StreamReader readStream = new StreamReader(stream);
                            string text = readStream.ReadToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unexpected exception when uploading a media.");
                    statuses.Add(new
                    {
                        error = T(ex.Message).Text,
                        //progress = 1.0,
                    });
                }
            }

            return Ok(new ResultViewModel {Content = new { FolderPath = folderPath, Data = statuses }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

        }

        [HttpPost]
        public async Task<IHttpActionResult> replace(int id)
        {
            if (!Services.Authorizer.Authorize(Orchard.MediaLibrary.Permissions.ManageOwnMedia))
            {
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "" });
            }

            var replaceMedia = Services.ContentManager.Get<MediaPart>(id);
            if (replaceMedia == null)
                return NotFound();


            // Check permission
            if (!Services.Authorizer.Authorize(Orchard.MediaLibrary.Permissions.ManageMediaContent) && !_mediaLibraryService.CanManageMediaFolder(replaceMedia.FolderPath))
            {
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "" });
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
                return InternalServerError();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = e.Message });
            }
            // Loop through each file in the request
            foreach (HttpContent content in multipartMemoryStreamProvider.Contents)
            {
                // Pointer to file

                string name = content.Headers.ContentDisposition.Name;
                string filename = string.IsNullOrEmpty(content.Headers.ContentDisposition.FileName) ? null : content.Headers.ContentDisposition.FileName.Replace("\"", "");
                filename = _mediaLibraryService.GetUniqueFilename(replaceMedia.FolderPath, filename);
                if (!string.IsNullOrEmpty(filename))
                {
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
                }


                try
                {
                    using (Stream stream = await content.ReadAsStreamAsync())
                    {
                        //byte[] bytes = new byte[stream.Length];
                        //stream.Read(bytes, 0, bytes.Length);

                        if (!string.IsNullOrEmpty(filename))
                        {
                            var mimeType = _mimeTypeProvider.GetMimeType(filename);

                            try
                            {
                                _mediaLibraryService.DeleteFile(replaceMedia.FolderPath, replaceMedia.FileName);
                            } catch(Exception ex)
                            {

                            }
                            _mediaLibraryService.UploadMediaFile(replaceMedia.FolderPath, filename, stream);
                            replaceMedia.FileName = filename;
                            replaceMedia.MimeType = mimeType;
                            var mediaFactory = _mediaLibraryService.GetMediaFactory(stream, mimeType, null);

                            //var mediaPart = _mediaLibraryService.ImportMedia(stream, folderPath, filename, null);
                            //Services.ContentManager.Create(mediaPart);
                            // Force a publish event which will update relevant Media properties
                            replaceMedia.ContentItem.VersionRecord.Published = false;
                            Services.ContentManager.Publish(replaceMedia.ContentItem);

                            statuses.Add(replaceMedia);
                        }
                        else if (!string.IsNullOrEmpty(name))
                        {
                            StreamReader readStream = new StreamReader(stream);
                            string text = readStream.ReadToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unexpected exception when uploading a media.");
                    statuses.Add(new
                    {
                        error = T(ex.Message).Text,
                        //progress = 1.0,
                    });
                }
            }

            return Ok(new ResultViewModel { Content = new { FolderPath = replaceMedia.FolderPath, Data = statuses }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

        }

        [HttpPost]
        public IHttpActionResult Download(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = Services.ContentManager.Get((int)inModel["Id"], VersionOptions.Published);
            MediaPart part = content.As<MediaPart>();
            if (content == null || part == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            var sourcePath = HttpContext.Current.Server.MapPath(@"~/Media/Default/" + part.FolderPath + "/" + part.FileName); //取得server的相對路徑
            if (!File.Exists(sourcePath))
            {
                return ResponseMessage(new HttpResponseMessage(HttpStatusCode.Gone));
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            response.Content = new StreamContent(fileStream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(part.MimeType);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = HttpUtility.UrlPathEncode(part.FileName);
            response.Content.Headers.ContentLength = fileStream.Length; //告知瀏覽器下載長度
            return ResponseMessage(response);
        }

        [HttpPost]
        public IHttpActionResult delete(MediaManagerEditApiViewModel inModel)
        {

            if (!Services.Authorizer.Authorize(Orchard.MediaLibrary.Permissions.ManageOwnMedia, T("Couldn't delete media items")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete media items" });

            if (inModel == null || inModel.Ids == null || inModel.Ids.Length <= 0)
                return NotFound();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var mediaItems = Services.ContentManager
                .Query(VersionOptions.Latest)
                .ForContentItems(inModel.Ids)
                .List()
                .Select(x => x.As<MediaPart>())
                .Where(x => x != null);

            if (mediaItems.Count() <= 0)
                return NotFound();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var statuses = new List<object>();

            foreach (var media in mediaItems)
            {
                try
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
                catch (Exception ex)
                {
                    Logger.Error(ex, "Could not delete media items.");
                    statuses.Add(new
                    {
                        error = T(ex.Message).Text
                    });
                }
            }
            return Ok(new ResultViewModel { Content = new { Data = statuses }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult update(JObject inModel)
        {
            if (inModel == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = Services.ContentManager.Get((int)inModel["Id"], VersionOptions.DraftRequired);

            if (content == null)
                return NotFound();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!Services.Authorizer.Authorize(Orchard.MediaLibrary.Permissions.ManageOwnMedia, content, T("Couldn't edit schedule")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit schedule" });

            if (inModel["Position"] != null)
            {
                MediaPart media = content.As<MediaPart>();
                dynamic field = media.Fields.First();
                if (field != null)
                    field.Value = Convert.ToDecimal(inModel["Position"]);

            }

            Services.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));

            Services.ContentManager.Publish(content);
            Services.Notifier.Information(T("content information updated"));

            return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(JObject inModel)
        {
            if (inModel == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = Services.ContentManager.Get((int)inModel["Id"], VersionOptions.DraftRequired);

            if (content == null)
                return NotFound();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            return Ok(new ResultViewModel { Content = content.As<MediaPart>(), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

    }
}