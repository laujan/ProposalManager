﻿// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using AutoMapper;
using ProjectSmartLink.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectSmartLink.Service
{
    public class ServiceMappingProfile : Profile
    {
        public override string ProfileName
        {
            get
            {
                return "DomainModelMappings";
            }
        }

        public ServiceMappingProfile()
        {
            CreateMap<SourcePoint, PublishedHistory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PublishedUser, opt => opt.MapFrom(source => source.Creator))
                .ForMember(dest => dest.PublishedDate, opt => opt.MapFrom(source => source.Created));
        }
    }
}
