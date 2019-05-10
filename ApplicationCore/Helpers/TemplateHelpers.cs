// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.ViewModels;
using ApplicationCore.Interfaces;
using ApplicationCore;
using ApplicationCore.Artifacts;
using ApplicationCore.Entities;
using ApplicationCore.Services;
using ApplicationCore.Helpers;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Models;


namespace ApplicationCore.Helpers
{
    public class TemplateHelpers
    {
        protected readonly ILogger _logger;
        protected readonly AppOptions _appOptions;
        private readonly IUserContext _userContext;
        private readonly IUserProfileRepository _userProfileRepository;

        /// <summary>
        /// Constructor
        /// </summary>
        public TemplateHelpers(
            ILogger<TemplateHelpers> logger,
            IOptions<AppOptions> appOptions,
            IUserContext userContext,
            IUserProfileRepository userProfileRepository)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));
            Guard.Against.Null(userContext, nameof(userContext));
            Guard.Against.Null(userProfileRepository, nameof(userProfileRepository));

            _logger = logger;
            _appOptions = appOptions.Value;
            _userContext = userContext;
            _userProfileRepository = userProfileRepository;
        }

        public async Task<Template> MapToEntity(TemplateViewModel viewModel, string requestId = "")
        {
            var entity = new Template();

            entity.Id = viewModel.Id ?? String.Empty;
            entity.TemplateName = viewModel.TemplateName ?? String.Empty;
            entity.Description = viewModel.Description ?? String.Empty;
            entity.DefaultTemplate = viewModel.DefaultTemplate;
            entity.Initilaltemplate = viewModel.Initilaltemplate;
            entity.SelectProcessFlag = viewModel.SelectProcessFlag;

            //get userprofile entity
            if (viewModel.CreatedBy.Id == "")
            {
                var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                entity.CreatedBy  = await _userProfileRepository.GetItemByUpnAsync(currentUser, requestId);
            }
            else
            {
                entity.CreatedBy = await MapToUserProfileEntity(viewModel.CreatedBy,requestId);
            }

            entity.LastUsed = viewModel.LastUsed;
            entity.ProcessList = await MapToProcessEntity(viewModel.ProcessList, requestId);

            return entity;
        }
        public async Task<TemplateViewModel> MapToViewModel(Template entity, string requestId = "")
        {
            var model = new TemplateViewModel();

            model.Id = entity.Id ?? String.Empty;
            model.TemplateName = entity.TemplateName ?? String.Empty;
            model.Description = entity.Description ?? String.Empty;
            model.CreatedBy = await MapToUserProfileViewModel(entity.CreatedBy, requestId);
            model.LastUsed = entity.LastUsed;
            model.ProcessList = await MapToProcessViewModel(entity.ProcessList, requestId);

            model.DefaultTemplate = entity.DefaultTemplate;
            model.Initilaltemplate = entity.Initilaltemplate;
            model.SelectProcessFlag = entity.SelectProcessFlag;

            return model;
        }
        public Task<UserProfile> MapToUserProfileEntity(UserProfileViewModel userProfile, string requestId = "")
        {
            try
            {
                var userProfilefields = new UserProfileFields
                {
                    UserPrincipalName = userProfile.UserPrincipalName,
                    Mail = userProfile.Mail,
                    Title = userProfile.Title,
                    UserRoles = new List<Role>()
                };
                var userProfileEntity = new UserProfile
                {
                    Id = userProfile.Id,
                    DisplayName = userProfile.DisplayName,
                    Fields = userProfilefields
                };

                if (userProfile.UserRoles != null)
                {
                    foreach (var role in userProfile.UserRoles)
                    {
                        var userRole = new Role();
                        userRole.Id = role.Id;
                        userRole.DisplayName = role.DisplayName;
                        //Granular Permission Change :  Start
                        userRole.AdGroupName = role.AdGroupName;


                        userProfileEntity.Fields.UserRoles.Add(userRole);
                    }
                }

                return Task.FromResult(userProfileEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ToViewModel Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - ToViewModel Service Exception: {ex}");
            }
        }
        public async Task<UserProfileViewModel> MapToUserProfileViewModel(UserProfile userProfile, string requestId = "")
        {
            try
            {
                var userProfileViewModel = new UserProfileViewModel
                {
                    Id = userProfile.Id,
                    DisplayName = userProfile.DisplayName,
                    Mail = userProfile.Fields.Mail ?? String.Empty,
                    UserPrincipalName = userProfile.Fields.UserPrincipalName,
                    Title = userProfile.Fields.Title ?? String.Empty,
                    UserRoles = new List<RoleModel>()
                };

                if (userProfile.Fields.UserRoles != null)
                {
                    foreach (var role in userProfile.Fields.UserRoles)
                    {
                        var userRole = new RoleModel();
                        userRole.Id = role.Id;
                        userRole.DisplayName = role.DisplayName;
                        //Granular Permission Change :  Start
                        userRole.AdGroupName = role.AdGroupName;

                        userProfileViewModel.UserRoles.Add(userRole);
                    }
                }

                return userProfileViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ToViewModel Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - ToViewModel Service Exception: {ex}");
            }
        }
        public async Task<List<ProcessViewModel>> MapToProcessViewModel(IList<Process> entity, string requestId = "")
        {
            List<ProcessViewModel> model = new List<ProcessViewModel>();

            try
            {
                foreach (var process in entity)
                {
                    var temp = new ProcessViewModel();
                    temp.ProcessStep = process.ProcessStep ?? string.Empty;
                    temp.ProcessType = process.ProcessType ?? string.Empty;
                    temp.Order = process.Order ?? string.Empty;
                    temp.DaysEstimate = process.DaysEstimate ?? string.Empty;
                    temp.Channel = process.Channel ?? string.Empty;
                    temp.Status = process.Status ?? ActionStatus.NotStarted;
                    temp.RoleId = process.RoleId ?? String.Empty;
                    temp.RoleName = process.RoleName ?? String.Empty;

                    model.Add(temp);
                }

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ToViewModel Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - ToViewModel Service Exception: {ex}");
            }
        }
        public async Task<List<Process>> MapToProcessEntity(IList<ProcessViewModel> viewModel, string requestId = "")
        {
            List<Process> entity = new List<Process>();

            try
            {
                foreach (var process in viewModel)
                {
                    var temp = new Process();
                    temp.ProcessStep = process.ProcessStep ?? string.Empty;
                    temp.ProcessType = process.ProcessType ?? string.Empty;
                    temp.Order = process.Order ?? string.Empty;
                    temp.DaysEstimate = process.DaysEstimate ?? string.Empty;
                    temp.Channel = process.Channel ?? string.Empty;
                    temp.Status = process.Status ?? ActionStatus.NotStarted;

                    temp.RoleId = process.RoleId ?? String.Empty;
                    temp.RoleName = process.RoleName ?? String.Empty;

                    entity.Add(temp);
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ToViewModel Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - ToViewModel Service Exception: {ex}");
            }
        }
    }
}
