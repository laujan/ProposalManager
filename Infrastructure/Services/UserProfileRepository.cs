// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Entities;
using ApplicationCore.Helpers;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class UserProfileRepository : BaseRepository<UserProfile>, IUserProfileRepository
	{
		private readonly IMemoryCache _cache;
		private readonly GraphSharePointAppService _graphSharePointAppService;
		private readonly GraphUserAppService _graphUserAppService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserContext _userContext;
		private readonly List<UserProfile> _usersList;
        private const string UserProfileCacheKey = "PM_UsersList";

        public UserProfileRepository(ILogger<UserProfileRepository> logger,
			 GraphSharePointAppService graphSharePointAppService,
			 GraphUserAppService graphUserAppService,
             IRoleRepository roleRepository,
             IUserContext userContext,
             IOptionsMonitor<AppOptions> appOptions,
			 IMemoryCache memoryCache) : base(logger, appOptions)
		{
			Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            Guard.Against.Null(graphUserAppService, nameof(graphUserAppService));
            Guard.Against.Null(roleRepository, nameof(roleRepository));
            Guard.Against.Null(userContext, nameof(userContext));

            _graphSharePointAppService = graphSharePointAppService;
			_graphUserAppService = graphUserAppService;
            _roleRepository = roleRepository;
            _userContext = userContext;
            _cache = memoryCache;

			_usersList = new List<UserProfile>();
		}

        public void CleanCache()
        {
            _cache.Remove(UserProfileCacheKey);
        }

		public async Task<UserProfile> GetItemByIdAsync(string id, string requestId = "")
		{
			_logger.LogInformation($"RequestId: {requestId} - GetItemByIdAsync called.");

			try
			{
				Guard.Against.NullOrEmpty(id, "GetItemByIdAsync_id Null", requestId);

				var usersList = await CacheTryGetUsersListAsync(requestId);

				var userProfile = usersList.Find(x => x.Id == id);
				if (userProfile == null)
				{
					_logger.LogWarning($"RequestId: {requestId} - GetItemByIdAsync_id no user found: {id}");
					throw new ResponseException($"RequestId: {requestId} - GetItemByIdAsync_id Sno user found: {id}");
				}

				return userProfile;
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestId: {requestId} - GetItemByIdAsync Service Exception: {ex}");
				throw new ResponseException($"RequestId: {requestId} - GetItemByIdAsync Service Exception: {ex}");
			};
		}

		public async Task<UserProfile> GetItemByUpnAsync(string upn, string requestId = "")
		{
			_logger.LogInformation($"RequestId: {requestId} - GetItemByIdAsync called.");

			try
			{
				Guard.Against.NullOrEmpty(upn, "GetItemByUmpn_upn Null", requestId);

				var usersList = await CacheTryGetUsersListAsync(requestId);

				var userProfile = usersList.Find(x => x.Fields.UserPrincipalName == upn);
				if (userProfile == null)
				{
					_logger.LogWarning($"RequestId: {requestId} - GetItemByUpnAsync no user found: {upn}");
					throw new ResponseException($"RequestId: {requestId} - GetItemByUpnAsync Sno user found: {upn}");
				}

				return userProfile;
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestId: {requestId} - GetItemByUpnAsync Service Exception: {ex}");
				throw new ResponseException($"RequestId: {requestId} - GetItemByUpnAsync Service Exception: {ex}");
			}

		}

		public async Task<IList<UserProfile>> GetAllAsync(string requestId = "")
		{
			_logger.LogInformation($"RequestId: {requestId} - GetAllAsync called.");

			try
			{
				return await CacheTryGetUsersListAsync(requestId);
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestId: {requestId} - GetAllAsync Service Exception: {ex}");
				throw new ResponseException($"RequestId: {requestId} - GetAllAsync Service Exception: {ex}");
			}
		}

		private async Task<List<UserProfile>> GetUsersListAsync(string requestId = "")
		{
			try
			{
				if (_usersList?.Count == 0)
				{
					var roles = await _roleRepository.GetAllAsync(requestId);
                    foreach (var role in roles)
                    {
                        var userRole = Role.Empty;
                        userRole.Id = role.Id;
                        userRole.DisplayName = role.DisplayName;
                        userRole.AdGroupName = role.AdGroupName;
                        userRole.TeamsMembership = role.TeamsMembership;
                        userRole.Permissions = (
                                                from permission in role.Permissions
                                                select new Permission { Name = permission.Name, Id = permission.Id }
                                               ).ToList();

                        var options = new List<QueryParam>
                                                {
                                                    new QueryParam("filter", $"startswith(displayName,'{role.AdGroupName}')"),
                                                    new QueryParam("$expand", "members")
                                                };
                    
                        dynamic jsonDyn = await _graphUserAppService.GetGroupAsync(options, "", requestId);

						if (jsonDyn.value.HasValues)
						{
							userRole.Id = jsonDyn.value[0].id.ToString();

							foreach (var member in jsonDyn.value[0]["members"])
							{
								var user = UserProfile.Empty;
								user = _usersList.Find(x => x.Id == member["id"].ToString());

								if (user != null)
								{
									_usersList.Remove(user);
								}
								else
								{
									user = UserProfile.Empty;
									user.Id = member["id"].ToString();
								}

								user.DisplayName = member["displayName"].ToString();
								if (user.Fields == null) user.Fields = UserProfileFields.Empty;
								user.Fields.Mail = member["mail"].ToString();
								user.Fields.UserPrincipalName = member["userPrincipalName"].ToString();
                                user.Fields.Title = member["jobTitle"].ToString() ?? String.Empty;

                                // Check if user already has the role
                                var existingRole = user.Fields.UserRoles.Find(x => x.Id == userRole.Id);
                                if (existingRole == null)
                                {
                                    user.Fields.UserRoles.Add(userRole);
                                }

								_usersList.Add(user);
							}
						}
					}
				}

				return _usersList;
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestId: {requestId} - GetUsersListAsync Service Exception: {ex}");
				throw new ResponseException($"RequestId: {requestId} - GetUsersListAsync Service Exception: {ex}");
			}
		}

		private async Task<List<UserProfile>> CacheTryGetUsersListAsync(string requestId = "")
		{
			try
			{
				var userProfileList = new List<UserProfile>();

				if (_appOptions.UserProfileCacheExpiration == 0)
				{
					userProfileList = await GetUsersListAsync(requestId);
				}
				else
				{
					var isExist = _cache.TryGetValue(UserProfileCacheKey, out userProfileList);

					if (!isExist)
					{
						userProfileList = await GetUsersListAsync(requestId);

						var cacheEntryOptions = new MemoryCacheEntryOptions()
							.SetAbsoluteExpiration(TimeSpan.FromMinutes(_appOptions.UserProfileCacheExpiration));

						_cache.Set(UserProfileCacheKey, userProfileList, cacheEntryOptions);
					}
				}

				return userProfileList;
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestId: {requestId} - CacheGetOrCreateUsersListAsync Service Exception: {ex}");
				throw new ResponseException($"RequestId: {requestId} - CacheGetOrCreateUsersListAsync Service Exception: {ex}");
			}
		}
	}
}
