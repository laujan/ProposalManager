﻿// Copyright(c) Microsoft Corporation. 
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
using ApplicationCore.Helpers;
using ApplicationCore.Models;
using ApplicationCore.Helpers.Exceptions;
using Infrastructure.Helpers;
using ApplicationCore.Authorization;

namespace Infrastructure.Services
{
    public class OpportunityService : BaseService<OpportunityService>, IOpportunityService
    {
        private readonly IOpportunityRepository _opportunityRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly OpportunityHelpers _opportunityHelpers;
        private readonly UserProfileHelpers _userProfileHelpers;
        private readonly IOpportunityFactory _opportunityFactory;
        private readonly IAuthorizationService _authorizationService;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUserContext _userContext;
        /// <summary>
        /// Constructor
        /// </summary>
        public OpportunityService(
            ILogger<OpportunityService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IUserProfileRepository userProfileRepository,
            IOpportunityRepository opportunityRepository,
            OpportunityHelpers opportunityHelpers,
            IOpportunityFactory opportunityFactory,
            IAuthorizationService authorizationService,
            IPermissionRepository permissionRepository,
            IUserContext userContext,
            UserProfileHelpers userProfileHelpers) : base(logger, appOptions)
            
        {
            Guard.Against.Null(opportunityRepository, nameof(opportunityRepository));
            Guard.Against.Null(opportunityHelpers, nameof(opportunityHelpers));
            Guard.Against.Null(userProfileHelpers, nameof(userProfileHelpers));
            Guard.Against.Null(authorizationService, nameof(authorizationService));
            Guard.Against.Null(permissionRepository, nameof(permissionRepository));
            Guard.Against.Null(userContext, nameof(userContext));

            _userProfileRepository = userProfileRepository;
            _opportunityRepository = opportunityRepository;
            _opportunityHelpers = opportunityHelpers;
            _userProfileHelpers = userProfileHelpers;
            _opportunityFactory = opportunityFactory;
            _authorizationService = authorizationService;
            _permissionRepository = permissionRepository;
            _userContext = userContext;
        }

        public async Task<StatusCodes> CreateItemAsync(OpportunityViewModel opportunityViewModel, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - CreateItemAsync called.");

            Guard.Against.Null(opportunityViewModel, nameof(opportunityViewModel), requestId);
            Guard.Against.NullOrEmpty(opportunityViewModel.DisplayName, nameof(opportunityViewModel.DisplayName), requestId);
            try
            {
                var opportunity = await _opportunityHelpers.ToOpportunityAsync(opportunityViewModel, Opportunity.Empty, requestId);

                var result = await _opportunityRepository.CreateItemAsync(opportunity, requestId);

                Guard.Against.NotStatus201Created(result, "CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(OpportunityViewModel opportunityViewModel, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - UpdateItemAsync called.");

            Guard.Against.Null(opportunityViewModel, nameof(opportunityViewModel), requestId);
            Guard.Against.NullOrEmpty(opportunityViewModel.Id, nameof(opportunityViewModel.Id), requestId);

            //Granular bug fix : Start
            //Temp fix for checklist process update
            //Overriding granular access while getting exsiting opportunity model from sharepoint
            _authorizationService.SetGranularAccessOverride(true);
            var currentOppModel = await GetItemByIdAsync(opportunityViewModel.Id, requestId);
            _authorizationService.SetGranularAccessOverride(false);
            //Granular bug fix : End

            // Check if deal type is beign updated to check permissions
            if (opportunityViewModel.Template != currentOppModel.Template)
            {
                //Check access HERE for permission: Opportunity_ReadWrite_Dealtype and throw accessdenied if permission not present for current user
                //Granular Access : Start
                var result = await _authorizationService.CheckAccessFactoryAsync(PermissionNeededTo.WriteAll, requestId);
                if (result != StatusCodes.Status200OK) {
                    result = await _authorizationService.CheckAccessFactoryAsync(PermissionNeededTo.DealTypeWrite, requestId);
                    if (result != StatusCodes.Status200OK)
                    {
                        //One more check need to put in place
                        result = await _authorizationService.CheckAccessFactoryAsync(PermissionNeededTo.Write, requestId);
                        var result1 = await _authorizationService.CheckAccessFactoryAsync(PermissionNeededTo.WritePartial, requestId);
                        if (result != StatusCodes.Status200OK && result1 != StatusCodes.Status200OK)
                        {
                            _logger.LogError($"RequestId: {requestId} - UpdateItemAsync Service Exception");
                            throw new AccessDeniedException($"RequestId: {requestId} - UpdateItemAsync Service Exception");
                        }
                        else
                        {
                            var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                            if (!(opportunityViewModel.TeamMembers).ToList().Any
                                    (teamMember => teamMember.UserPrincipalName.ToString() == currentUser))
                            {
                                // This user is not having any write permissions, so he won't be able to update
                                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                                throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                            }
                        }
                    }
                }
                //Granular Access : End
            }

            var currentOpp = await _opportunityHelpers.ToOpportunityAsync(currentOppModel, Opportunity.Empty, requestId);

            if (opportunityViewModel.Id == currentOpp.Id)
            {
                try
                {
                    var opportunity = await _opportunityHelpers.ToOpportunityAsync(opportunityViewModel, currentOpp, requestId);

                    var result = await _opportunityRepository.UpdateItemAsync(opportunity, requestId);

                    Guard.Against.NotStatus200OK(result, "UpdateItemAsync", requestId);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - UpdateItemAsync Service Exception: {ex}");
                    throw new ResponseException($"RequestId: {requestId} - UpdateItemAsync Service Exception: {ex}");
                }
            }
            else
            {
                _logger.LogError($"RequestId: {requestId} - UpdateItemAsync Service error: mistmatch");
                throw new ResponseException($"RequestId: {requestId} - UpdateItemAsync Service Exception: mistmatch");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DeleteItemAsync called.");
            Guard.Against.Null(id, nameof(id));

            var result = await _opportunityRepository.DeleteItemAsync(id, requestId);

            Guard.Against.NotStatus204NoContent(result, "DeleteItemAsync", requestId);

            return result;
        }

        public async Task<OpportunityIndexViewModel> GetAllAsync(int pageIndex, int itemsPage, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetAllAsync called.");

            Guard.Against.Null(pageIndex, nameof(pageIndex), requestId);
            Guard.Against.Null(itemsPage, nameof(itemsPage), requestId);

            try
            {
                var listItems = (await _opportunityRepository.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(listItems, nameof(listItems), requestId);

                var vmListItems = new List<OpportunityViewModel>();
                foreach (var item in listItems)
                {
					try
					{
						vmListItems.Add(await _opportunityHelpers.ToOpportunityViewModelAsync(item, requestId));
					}
					catch (Exception ex)
					{
						_logger.LogError($"Opportunity Id: {item.Id} - GetAllAsync Opportunity Service Exception: {ex}");
					}
                }

                if (vmListItems.Count == 0)
                {
                    _logger.LogWarning($"RequestId: {requestId} - GetAllAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: GetAllAsync - No Items Found");
                }


                return new OpportunityIndexViewModel
                {
                    ItemsList = vmListItems.Select(item => new OpportunityIndexModel
                                {
                                    Id = item.Id,
                                    DisplayName = item.DisplayName,
                                    Customer = item.Customer,
                                    OpportunityState = item.OpportunityState,
                                    Template = item.Template,
                                    Dealsize = item.MetaDataFields.ToList().Find(x => x.DisplayName == "Deal Size")?.Values.ToString(),
                                    OpenedDate = item.MetaDataFields.ToList().Find(x => x.DisplayName == "Opened Date").Values.ToString()

                    }).ToList(),

                    PaginationInfo = new PaginationInfoViewModel
                    {
                        ActualPage = 1,
                        ItemsPerPage = itemsPage,
                        TotalItems = 0,
                        TotalPages = 0,
                        Next = String.Empty,
                        Previous = String.Empty
                    }

                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetAllAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetAllAsync Service Exception: {ex}");
            }
        }

        public async Task<OpportunityViewModel> GetItemByIdAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetItemByIdAsync called.");

            Guard.Against.NullOrEmpty(id, nameof(id), requestId);

            try
            {
                var thisOpportunity = new Opportunity();
                if (id == "testdata") // To get test data instead of going to back end
                {
                    thisOpportunity = TestData();
                    thisOpportunity.Id = id;
                }
                else
                {
                    thisOpportunity = await _opportunityRepository.GetItemByIdAsync(id, requestId);
                }

                if (thisOpportunity.Id != id)
                {
                    _logger.LogWarning($"RequestId: {requestId} - GetItemByIdAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: GetItemByIdAsync - No Items Found");
                }

                return await _opportunityHelpers.ToOpportunityViewModelAsync(thisOpportunity, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetItemByIdAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetItemByIdAsync Service Exception: {ex}");
            }
        }

        public async Task<OpportunityViewModel> GetItemByNameAsync(string name, bool isCheckName, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetItemByNameAsync called.");

            Guard.Against.Null(name, nameof(name), requestId);

            try
            {

                var thisOpportunity = await _opportunityRepository.GetItemByNameAsync(name, isCheckName, requestId);

                var opportunityViewModel = new OpportunityViewModel();
                if (thisOpportunity.DisplayName != name.Replace("'", ""))
                {
                    _logger.LogWarning($"RequestId: {requestId} - GetItemByNameAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: GetItemByNameAsync - No Items Found");
                }
                else
                {
                    if (isCheckName)
                    {
                        opportunityViewModel = new OpportunityViewModel();
                        opportunityViewModel.DisplayName = name.Replace("'", "");
                    }
                    else
                    {
                        opportunityViewModel = await _opportunityHelpers.ToOpportunityViewModelAsync(thisOpportunity, requestId);
                    }
                }

                // TODO: foreach service to filter out data you don't have access

                return opportunityViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetItemByNameAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetItemByNameAsync Service Exception: {ex}");
            }
        }

        public async Task<OpportunityViewModel> GetItemByRefAsync(string reference, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetItemByRefAsync called.");

            Guard.Against.NullOrEmpty(reference, nameof(reference), requestId);

            try
            {
                var thisOpportunity =  await _opportunityRepository.GetItemByRefAsync(reference, requestId);

                reference = reference.Replace("'", "");
                if (thisOpportunity.Reference != reference)
                {
                    _logger.LogWarning($"RequestId: {requestId} - GetItemByRefAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: GetItemByRefAsync - No Items Found");
                }

                return await _opportunityHelpers.ToOpportunityViewModelAsync(thisOpportunity, requestId);
                
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetItemByRefAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetItemByRefAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> AddSectionsAsync(string opportunityName, IList<DocumentSection> docSections, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - AddSectionsAsync called.");

            Guard.Against.Null(opportunityName, nameof(opportunityName), requestId);
            Guard.Against.Null(docSections, nameof(docSections), requestId);

            try
            {
                
                var thisOpportunity = await _opportunityRepository.GetItemByNameAsync(opportunityName, false, requestId);

                if (thisOpportunity.DisplayName != opportunityName.Replace("'", ""))
                {
                    _logger.LogWarning($"RequestId: {requestId} - AddSectionsAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: AddSectionsAsync - No Items Found");
                }
                else
                {
                    thisOpportunity.Content.ProposalDocument.Content.ProposalSectionList = docSections;
                    //TODO: Persist template in SharePoint and get the docuri

                    var respUpdate = await _opportunityRepository.UpdateItemAsync(thisOpportunity, requestId);
                    if (respUpdate != StatusCodes.Status200OK)
                    {
                        _logger.LogError($"RequestId: {requestId} - UpdateItemAsync error: {respUpdate.Name}");
                        return StatusCodes.Status400BadRequest;
                    }
                }

                // TODO: Repsond with the ok with metadata json with url link
                return StatusCodes.Status200OK;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - AddSectionsAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - AddSectionsAsync Service Exception: {ex}");
            }
        }

        // Private methods
        //WAVE-4 GENERIC ACCELERATOR Change : start
        private Opportunity TestData()
        {
            var role1 = Role.Empty;
            role1.Id = new Guid().ToString();
            role1.DisplayName = "RelationshipManager";
            role1.AdGroupName = "Relationship Managers";

            var role2 = Role.Empty;
            role2.Id = new Guid().ToString();
            role2.DisplayName = "LoanOfficer";
            role2.AdGroupName = "Loan Officers";
            

            var role3 = Role.Empty;
            role3.Id = new Guid().ToString();
            role3.DisplayName = "CreditAnalyst";
            role3.AdGroupName = "Credit Analysts";
            

            var role4 = Role.Empty;
            role4.Id = new Guid().ToString();
            role4.DisplayName = "LegalCounsel";
            role4.AdGroupName = "Legal Counsel";
           
            var role5 = Role.Empty;
            role5.Id = new Guid().ToString();
            role5.DisplayName = "SeniorRiskOfficer";
            role5.AdGroupName = "Senior Risk Officers";
            

            var tm2 = new List<Role>();
            tm2.Add(role2);
            tm2.Add(role3);

            var tm3 = new List<Role>();
            tm3.Add(role3);

            var tm4 = new List<Role>();
            tm4.Add(role4);

            var tm5 = new List<Role>();
            tm5.Add(role5);

            var user1 = UserProfile.Empty;
            user1.Id = new Guid().ToString();
            user1.DisplayName = "Robin Counts";
            user1.Fields = UserProfileFields.Empty;
            user1.Fields.Mail = "robin@contoso.com";
            user1.Fields.UserPrincipalName = "robin@contoso.com";
            user1.Fields.UserRoles.Add(role1);
            user1.Fields.UserRoles.Add(role3);
            user1.Fields.Title = "Relationship Manager";

            var checklist1 = new Checklist
            {
                Id = Guid.NewGuid().ToString(),
                ChecklistChannel = "Risk Assessment",
                ChecklistStatus = ActionStatus.NotStarted,
                ChecklistTaskList = new List<ChecklistTask>() { new ChecklistTask { Id = Guid.NewGuid().ToString(), ChecklistItem = "Task 2", Completed = false, FileUri = "some uri of the file" }, new ChecklistTask { Id = Guid.NewGuid().ToString(), ChecklistItem = "Task 1", Completed = false, FileUri = "some uri of the file" } }
            };

            var checklist2 = new Checklist
            {
                Id = Guid.NewGuid().ToString(),
                ChecklistChannel = "Credit Check",
                ChecklistStatus = ActionStatus.NotStarted,
                ChecklistTaskList = new List<ChecklistTask>() { new ChecklistTask { Id = Guid.NewGuid().ToString(), ChecklistItem = "Task 2", Completed = false, FileUri = "some uri of the file" }, new ChecklistTask { Id = Guid.NewGuid().ToString(), ChecklistItem = "Task 1", Completed = false, FileUri = "some uri of the file" } }
            };

            var checklist3 = new Checklist
            {
                Id = Guid.NewGuid().ToString(),
                ChecklistChannel = "Compliance",
                ChecklistStatus = ActionStatus.NotStarted,
                ChecklistTaskList = new List<ChecklistTask>() { new ChecklistTask { Id = Guid.NewGuid().ToString(), ChecklistItem = "Task 2", Completed = false, FileUri = "some uri of the file" }, new ChecklistTask { Id = Guid.NewGuid().ToString(), ChecklistItem = "Task 1", Completed = false, FileUri = "some uri of the file" } }
            };

            var checklists = new List<Checklist>();
            checklists.Add(checklist1);
            checklists.Add(checklist2);
            checklists.Add(checklist3);

            var opportunity = new Opportunity
            {
                Id = "3",
                DisplayName = "Modernize fleet",
                Reference = "Some other correlation Id",
                Metadata = new OpportunityMetadata
                {
                    OpportunityState = OpportunityState.Creating,
                    Customer = new Customer
                    {
                        Id = new Guid().ToString(),
                        DisplayName = "ZXY Motors",
                        ReferenceId = String.Empty,
                    },
                    Fields = new List<OpportunityMetaDataFields>
                    {
                        new OpportunityMetaDataFields{DisplayName="DealSize",Values=1500000,FieldType=FieldType.Double, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="AnnualRevenue",Values=7500100,FieldType=FieldType.Double, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="OpenedDate",Values=DateTime.Now,FieldType=FieldType.Date, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="Margin",Values= 10,FieldType=FieldType.Double, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="Rate",Values=5,FieldType=FieldType.Double, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="DebtRatio",Values=123,FieldType=FieldType.Double, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="Purpose",Values="Purchase Real Estate",FieldType=FieldType.String, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="DisbursementSchedule",Values="12 monthly instalments",FieldType=FieldType.String, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="CollateralAmount",Values=139900000.0,FieldType=FieldType.Double, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="Guarantees",Values="Primary Assets",FieldType=FieldType.String, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="RiskRating",Values=1,FieldType=FieldType.Int, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="Industry",Values="Industry 1",FieldType=FieldType.DropDown, Screen="Screen1"},
                        new OpportunityMetaDataFields{DisplayName="Region",Values="Region 1",FieldType=FieldType.DropDown, Screen="Screen1"}
                    }

                },
                Content = new OpportunityContent
                {
                    Notes = new List<Note>() { new Note() { Id = "1", CreatedDateTime = DateTimeOffset.Now, NoteBody = "Some Note", CreatedBy = user1 } },
                    TeamMembers = new List<TeamMember>
                    {
                        new TeamMember
                        {
                            Id = user1.Id,
                            DisplayName = user1.DisplayName,
                            RoleId = user1.Id,
                            Fields = new TeamMemberFields
                            {
                                Mail = "robin@contoso.com",
                                UserPrincipalName = "robin@contoso.com",
                                Title = "Loan Officer",
                                Permissions = new List<Permission>
                                {
                                    new Permission {Name="Permission 1" ,Id="1"},
                                    new Permission {Name="Permission 2" ,Id="2"},
                                }
                            },
                        },
                        new TeamMember
                        {
                            Id = new Guid().ToString(),
                            DisplayName = "Carol Poland",
                            RoleId = user1.Id,
                            Fields = new TeamMemberFields
                            {
                                Mail = "carol@contoso.com",
                                UserPrincipalName = "carol@contoso.com",
                                Title = "Loan Officer",
                                Permissions = new List<Permission>
                                {
                                    new Permission {Name="Permission 1" ,Id="1"},
                                    new Permission {Name="Permission 2" ,Id="2"},
                                }
                            }
                        }
                    },
                    Checklists = checklists,
                    CustomerDecision = new CustomerDecision
                    {
                        Id = "1",
                        Approved = false,
                        ApprovedDate = DateTimeOffset.MinValue,
                        LoanDisbursed = DateTimeOffset.MinValue
                    },
                    ProposalDocument = new ProposalDocument
                    {
                        Id = new Guid().ToString(),
                        DisplayName = "Proposal Document for this loan",
                        Reference = "Some correlation ID",
                        Metadata = new DocumentMetadata
                        {
                            Category = new Category
                            {
                                Id = "1",
                                Name = "Category 1"
                            },
                            //DocumentName = "ProposalDocName.docx",
                            DocumentUri = "https://onterawe.sharepoint.com/sites/XYZMotors/General/ProposalDocName.docx",
                            Notes = new List<Note>() { new Note() { Id = "1", CreatedDateTime = DateTimeOffset.Now, NoteBody = "Some proposal document note", CreatedBy = user1 }, new Note() { Id = "2", CreatedDateTime = DateTimeOffset.Now, NoteBody = "Some proposal document note2", CreatedBy = user1 } },
                            Tags = "Tag1, Tag2, Tag3"
                        },
                        Content = new ProposalDocumentContent
                        {
                            ProposalSectionList = new List<DocumentSection>
                            {
                                new DocumentSection
                                {
                                    Id = "1",
                                    DisplayName = "Executive Summary",
                                    Owner = user1,
                                    SectionStatus = ActionStatus.InProgress,
                                    LastModifiedDateTime = DateTimeOffset.Now,
                                    SubSectionId = String.Empty
                                },
                                new DocumentSection
                                {
                                    DisplayName = "Introduction",
                                    Owner = user1,
                                    SectionStatus = ActionStatus.InProgress,
                                    LastModifiedDateTime = DateTimeOffset.Now,
                                    SubSectionId = String.Empty
                                },
                            }
                        }
                    }
                },
                DocumentAttachments = new List<DocumentAttachment>()
            };

            var doc = DocumentAttachment.Empty;
            doc.Id = "01DSZQSAWRMQMEVQQUV5A2T2OYAANNOGD6";
            doc.FileName = "SimpleDoc.docx";
            doc.Category = Category.Empty;
            doc.Category.Id = "1";
            doc.Category.Name = "Category1";
            doc.Tags = "Tag1, Tag2";
            doc.DocumentUri = "https://onterawe.sharepoint.com/sites/ProposalManagement/_layouts/Doc.aspx?sourcedoc=%7B4A1864D1-14C2-41AF-A9E9-D8001AD7187E%7D&file=SimpleDoc.docx&action=default&mobileredirect=true";
            doc.Note = "Some notes";

            opportunity.DocumentAttachments.Add(doc);

            return opportunity;
        }
        //WAVE-4 GENERIC ACCELERATOR Change : end
    }
}
