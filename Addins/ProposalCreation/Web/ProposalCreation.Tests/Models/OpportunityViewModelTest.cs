// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreation.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProposalCreation.Tests.Models
{

	[TestClass]
	public class OpportunityViewModelTest
	{

		[TestMethod]
		public void EmptyOpportunityIsInvalid()
		{
			var opportunity = new OpportunityViewModel();
			var context = new ValidationContext(opportunity);

			var isModelStateValid = Validator.TryValidateObject(opportunity, context, new List<ValidationResult>(), true);

			Assert.IsFalse(isModelStateValid);
		}

	}

}
