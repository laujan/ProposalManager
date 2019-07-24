// Copyright(c) Microsoft Corporation. 
// All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.ViewModels;
using ApplicationCore.Interfaces;
using ApplicationCore;
using ApplicationCore.Helpers;
using ApplicationCore.Helpers.Exceptions;

namespace Infrastructure.Services
{
    public class UserProfileService : BaseService<UserProfileService>, IUserProfileService
	{
		private readonly IUserProfileRepository _userProfileRepository;
        private readonly UserProfileHelpers _userProfileHelpers;

        public UserProfileService(
			ILogger<UserProfileService> logger,
            IOptionsMonitor<AppOptions> appOptions,
			IUserProfileRepository userProfileRepository,
            UserProfileHelpers userProfileHelpers) : base(logger, appOptions)
		{
			Guard.Against.Null(userProfileRepository, nameof(userProfileRepository));
            Guard.Against.Null(userProfileHelpers, nameof(userProfileHelpers));

            _userProfileRepository = userProfileRepository;
            _userProfileHelpers = userProfileHelpers;

        }

		public async Task<UserProfileViewModel> GetItemByIdAsync(string id, string requestId = "")
		{
            _logger.LogInformation($"RequestId: {requestId} - UserProfileService_GetItemByIdAsync called.");

            var selectedUserProfile = await _userProfileRepository.GetItemByIdAsync(id);
			var userProfileViewModel = await _userProfileHelpers.ToViewModelAsync(selectedUserProfile);

			return userProfileViewModel;
		}

		public async Task<UserProfileViewModel> GetItemByUpnAsync(string upn, string requestId = "")
		{
			_logger.LogInformation($"RequestId: {requestId} - UserProfileService_GetItemByUpnAsync called.");

			try
			{
				var selectedUserProfile = await _userProfileRepository.GetItemByUpnAsync(upn, requestId);
				var userProfileViewModel = await _userProfileHelpers.ToViewModelAsync(selectedUserProfile);

				return userProfileViewModel;
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestId: {requestId} - UserProfileService_GetItemByUpnAsync Service Exception: {ex}");
				throw new ResponseException($"RequestId: {requestId} - UserProfileService_GetItemByUpnAsync Service Exception: {ex}");
			}

		}

		public async Task<UserProfileListViewModel> GetAllAsync(int pageIndex, int itemsPage, string requestId = "")
		{
			_logger.LogInformation($"RequestId: {requestId} - UserProfileService_GetAllAsync called.");

			try
			{
				var listItems = (await _userProfileRepository.GetAllAsync(requestId)).ToList();
				Guard.Against.Null(listItems, nameof(listItems), requestId);

                var userProfileListViewModel = new UserProfileListViewModel
                {
                    ItemsList = await Task.WhenAll(listItems.Select(item => _userProfileHelpers.ToViewModelAsync(item)).ToList())
                };

                if (userProfileListViewModel.ItemsList.Count == 0)
				{
					_logger.LogWarning($"RequestId: {requestId} - UserProfileService_GetAllAsync no items found");
					throw new NoItemsFound($"RequestId: {requestId} - Method name: UserProfileService_GetAllAsync - No Items Found");
				}

				return userProfileListViewModel;
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestId: {requestId} - UserProfileService_GetAllAsync Service Exception: {ex}");
				throw new ResponseException($"RequestId: {requestId} - UserProfileService_GetAllAsync Service Exception: {ex}");
			}
		}
	}
}
