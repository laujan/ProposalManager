// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;
using System.IO;
using System.Linq;
using ApplicationCore.Entities;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using ApplicationCore;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Helpers.Exceptions;
using System.Threading.Tasks;
using ApplicationCore.Interfaces;
using System.Text;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using Text = DocumentFormat.OpenXml.Presentation.Text;

namespace Infrastructure.OfficeApi
{
    public class PowerPointParser : BaseService<PowerPointParser>, IPowerPointParser
    {
        public PowerPointParser(ILogger<PowerPointParser> logger, IOptionsMonitor<AppOptions> appOptions) : base(logger, appOptions)
        {
        }

        /// <summary>
        /// RetrieveTOC
        /// </summary>
        /// <param name="fileStream">stream containing the docx file contents</param>
        /// <returns>List of DocumentSection objects</returns>
        public IList<DocumentSection> RetrieveTOC(Stream fileStream, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RetrieveTOC called.");

            try
            {
                // Open the presentation as read-only.
                using (PresentationDocument presentationDocument =
                    PresentationDocument.Open(fileStream, false))
                {
                    return GetSlideTitles(presentationDocument);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RetrieveTOC PowerPoint Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RetrieveTOC PowerPoint Service Exception: {ex}");
            }
        }
        // Get a list of the titles of all the slides in the presentation.
        private IList<DocumentSection> GetSlideTitles(PresentationDocument presentationDocument)
        {
            if (presentationDocument == null)
            {
                throw new ArgumentNullException("presentationDocument");
            }

            // Get a PresentationPart object from the PresentationDocument object.
            PresentationPart presentationPart = presentationDocument.PresentationPart;

            if (presentationPart != null &&
                presentationPart.Presentation != null)
            {
                // Get a Presentation object from the PresentationPart object.
                Presentation presentation = presentationPart.Presentation;

                if (presentation.SlideIdList != null)
                {
                    IList<DocumentSection> documentSections = new List<DocumentSection>();

                    // Get the title of each slide in the slide order.
                    foreach (var slideId in presentation.SlideIdList.Elements<SlideId>())
                    {
                        SlidePart slidePart = presentationPart.GetPartById(slideId.RelationshipId) as SlidePart;
                        //create a new guid.
                        var currentSecId = Guid.NewGuid().ToString();
                        // Create a new DocumentSection object and add it to the list
                        documentSections.Add(new DocumentSection
                        {
                            Id = currentSecId,
                            SubSectionId = slideId.Id,
                            DisplayName = GetSlideTitle(slidePart),
                            LastModifiedDateTime = DateTimeOffset.MinValue,
                            Owner = new UserProfile
                            {
                                Id = String.Empty,
                                DisplayName = String.Empty,
                                Fields = new UserProfileFields()
                            },
                            SectionStatus = ActionStatus.NotStarted
                        });
                    }

                    return documentSections;
                }

            }

            return null;
        }
        // Get the title string of the slide.
        private string GetSlideTitle(SlidePart slidePart)
        {
            if (slidePart == null)
            {
                throw new ArgumentNullException("presentationDocument");
            }

            // Declare a paragraph separator.
            string paragraphSeparator = null;

            if (slidePart.Slide != null)
            {
                // Find all the title shapes.
                var shapes = from shape in slidePart.Slide.Descendants<Shape>()
                             where IsTitleShape(shape)
                             select shape;

                StringBuilder paragraphText = new StringBuilder();

                foreach (var shape in shapes)
                {
                    // Get the text in each paragraph in this shape.
                    foreach (var paragraph in shape.TextBody.Descendants<D.Paragraph>())
                    {
                        // Add a line break.
                        paragraphText.Append(paragraphSeparator);

                        foreach (var text in paragraph.Descendants<D.Text>())
                        {
                            paragraphText.Append(text.Text);
                        }

                        paragraphSeparator = "\n";
                    }
                }

                return paragraphText.ToString();
            }

            return string.Empty;
        }
        // Determines whether the shape is a title shape.
        private bool IsTitleShape(Shape shape)
        {
            var placeholderShape = shape.NonVisualShapeProperties.ApplicationNonVisualDrawingProperties.GetFirstChild<PlaceholderShape>();
            if (placeholderShape != null && placeholderShape.Type != null && placeholderShape.Type.HasValue)
            {
                switch ((PlaceholderValues)placeholderShape.Type)
                {
                    // Any title shape.
                    case PlaceholderValues.Title:

                    // A centered title.
                    case PlaceholderValues.CenteredTitle:
                        return true;

                    default:
                        return false;
                }
            }
            return false;
        }
    }
}