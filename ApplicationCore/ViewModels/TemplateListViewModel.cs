using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ApplicationCore.Entities;
using ApplicationCore.ViewModels;

namespace ApplicationCore.ViewModels
{
    public class TemplateListViewModel : BaseListViewModel<TemplateViewModel>
    {
        public TemplateListViewModel() : base()
        {
        }           
    }
}
