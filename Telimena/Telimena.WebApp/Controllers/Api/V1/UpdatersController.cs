﻿using System;
using System.IdentityModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using AutoMapper;

using Newtonsoft.Json;
using Telimena.WebApp.Core.DTO.MappableToClient;
using Telimena.WebApp.Core.Interfaces;
using Telimena.WebApp.Core.Messages;
using Telimena.WebApp.Core.Models;
using Telimena.WebApp.Core.Models.Portal;
using Telimena.WebApp.Infrastructure;
using Telimena.WebApp.Infrastructure.Repository.FileStorage;
using Telimena.WebApp.Infrastructure.Security;
using Telimena.WebApp.Infrastructure.UnitOfWork;

namespace Telimena.WebApp.Controllers.Api.V1
{

    /// <summary>
    /// Controls the updaters
    /// </summary>
    [TelimenaApiAuthorize(Roles = TelimenaRoles.Admin)]
    [RoutePrefix("api/v{version:apiVersion}/updaters")]
    public partial class UpdatersController : ApiController
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="work"></param>
        /// <param name="fileSaver"></param>
        /// <param name="fileRetriever"></param>
        public UpdatersController(IToolkitDataUnitOfWork work, IFileSaver fileSaver, IFileRetriever fileRetriever)
        {
            this.work = work;
            this.fileSaver = fileSaver;
            this.fileRetriever = fileRetriever;
        }

        private readonly IToolkitDataUnitOfWork work;
        private readonly IFileSaver fileSaver;
        private readonly IFileRetriever fileRetriever;

        /// <summary>
        /// Downloads the updater with specified ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns> 
        [AllowAnonymous]
        
        [HttpGet, Route("{id}", Name = Routes.Get)]
        public async Task<IHttpActionResult> Get(Guid id)
        {
            UpdaterPackageInfo updaterInfo = await this.work.UpdaterRepository.GetPackageInfo(id).ConfigureAwait(false);
            if (updaterInfo == null)
            {
                return this.BadRequest($"Updater id [{id}] does not exist");
            }

            byte[] bytes = await this.work.UpdaterRepository.GetPackage(updaterInfo.Id, this.fileRetriever).ConfigureAwait(false);
            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = updaterInfo.ZippedFileName };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

            return this.ResponseMessage(result);
        }

        /// <summary>
        /// Uploads an updater package
        /// </summary>
        /// <returns></returns>
        
        [HttpPost, Route("", Name = Routes.Upload)]
        public async Task<IHttpActionResult> Upload()
        {
            try
            {
                string reqString = HttpContext.Current.Request.Form["Model"];
                UploadUpdaterRequest request = JsonConvert.DeserializeObject<UploadUpdaterRequest>(reqString);

                HttpPostedFile uploadedFile = HttpContext.Current.Request.Files.Count > 0 ? HttpContext.Current.Request.Files[0] : null;
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    TelimenaUser user = await this.work.Users.GetByPrincipalAsync(this.User).ConfigureAwait(false);
                    Updater updater = await this.work.UpdaterRepository.GetUpdater(request.UpdaterInternalName).ConfigureAwait(false);

                    if (updater == null)
                    {
                        updater = this.work.UpdaterRepository.Add(uploadedFile.FileName, request.UpdaterInternalName, user);
                    }

                    if (user.AssociatedDeveloperAccounts.All(x => x.Id != updater.DeveloperTeam.Id))
                    {
                        return this.BadRequest(
                            $"Updater '{updater.InternalName}' is managed by a team that you don't belong to - '{updater.DeveloperTeam.Name}'");
                    }

                    if (uploadedFile.FileName != updater.FileName && !uploadedFile.FileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return this.BadRequest(
                            $"Incorrect file. Expected {updater.FileName} or a zip package with it");
                    }

                    UpdaterPackageInfo pkg =
                        await this.work.UpdaterRepository.StorePackageAsync(updater, request.MinimumCompatibleToolkitVersion, uploadedFile.InputStream, this.fileSaver).ConfigureAwait(false);
                    await this.work.CompleteAsync().ConfigureAwait(false);
                    return this.Ok($"Uploaded package {pkg.Version} with ID {pkg.Id}");
                }

                return this.BadRequest("Empty attachment");
            }
            catch (Exception ex)
            {
                return this.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Sets the value of the 'is public' property on the specified updater  
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isPublic"></param>
        /// <returns></returns>
        
        [HttpPut, Route("{id}/is-public/{isPublic}", Name=Routes.SetIsPublic)]
        public async Task<IHttpActionResult> SetIsPublic(Guid id, bool isPublic)
        {
            var updater = await this.work.UpdaterRepository.GetUpdater(id).ConfigureAwait(false);
            if (updater == null)
            {
                return this.BadRequest($"Updater id [{id}] does not exist");
            }

            if (!isPublic && (updater.InternalName == DefaultToolkitNames.UpdaterInternalName || updater.InternalName == DefaultToolkitNames.PackageTriggerUpdaterInternalName))
            {
                return this.BadRequest($"Cannot change default updater");
            }
            updater.IsPublic = isPublic;
            await this.work.CompleteAsync().ConfigureAwait(false);
            return this.Ok($"Set package with ID: {id} public flag to: {isPublic}");
        }


        /// <summary>
        /// Gets the info about the newer version of the updater
        /// </summary>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost, Route("update-check", Name = Routes.UpdateCheck)]
        public async Task<UpdateResponse> UpdateCheck(UpdateRequest requestModel)
        {
         //   UpdateRequest requestModel = Utilities.ReadRequest(request, this.serializer);
            var program = await this.work.Programs.GetByTelemetryKey(requestModel.TelemetryKey).ConfigureAwait(false);
            if (program == null)
            {
                return new UpdateResponse()
                {
                    Exception = new BadRequestException($"Program with id [{requestModel.TelemetryKey}] does not exist")
                };
            }
            UpdaterPackageInfo updaterInfo =
                await this.work.UpdaterRepository.GetNewestCompatibleUpdater(program, requestModel.UpdaterVersion, requestModel.ToolkitVersion, false).ConfigureAwait(false);
            UpdateResponse response = new UpdateResponse();
            if (updaterInfo != null)
            {
                var info = Mapper.Map<UpdatePackageData>(updaterInfo);
                info.DownloadUrl = Router.Api.DownloadUpdaterUpdate(updaterInfo);
                response.UpdatePackages = new[] { info };
            }

            return response;
        }

    }
}