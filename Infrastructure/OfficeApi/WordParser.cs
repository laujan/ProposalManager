// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore;
using ApplicationCore.Entities;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Interfaces;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Infrastructure.OfficeApi
{
    public class WordParser : BaseService<WordParser>, IWordParser
    {
        public WordParser(ILogger<WordParser> logger, IOptionsMonitor<AppOptions> appOptions) : base(logger, appOptions)
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

            const string TOCHEADING = "TOCHeading";
            const string TOC = "Table of Contents";
            try
            {
                var documentSections = new List<DocumentSection>();

                using (var document = WordprocessingDocument.Open(fileStream, false))
                {
                    var docPart = document.MainDocumentPart;
                    var doc = docPart.Document;

                    OpenXmlElement block = doc.Descendants<DocPartGallery>().
                        Where(b => b.Val.HasValue &&
                        (b.Val.Value.Equals(TOC, StringComparison.InvariantCultureIgnoreCase))).FirstOrDefault();

                    if (block == null)
                    {
                        throw new InvalidOperationException("The document doesn't contain a Table of Contents.");
                    }

                    // Extract the Table of Contents section information and create the list
                    DocumentSection parent = null;
                    foreach (var tocPart in document.MainDocumentPart.Document.Body.Descendants<SdtContentBlock>().First())
                    {
                        var styles = tocPart.Descendants<ParagraphStyleId>();

                        if (styles.Count() == 0 || styles.First().Val.Value.Equals(TOCHEADING, StringComparison.InvariantCultureIgnoreCase))
                        {
                            continue;
                        }

                        var tocStyle = styles.First().Val.Value;
                        var level = int.Parse(tocStyle.Last().ToString());

                        var section = new DocumentSection()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Level = level,
                            LastModifiedDateTime = DateTimeOffset.MinValue,
                            DisplayName = tocPart.Descendants<Text>().ToArray()[0].InnerText,
                            Owner = new UserProfile
                            {
                                Id = string.Empty,
                                DisplayName = string.Empty,
                                Fields = new UserProfileFields()
                            },
                            SectionStatus = ActionStatus.NotStarted
                        };

                        if (level == 1)
                        {
                            section.SubSectionId = string.Empty;
                        }
                        else if (level > parent.Level)
                        {
                            section.SubSectionId = parent.Id;
                        }
                        else if (level == parent.Level)
                        {
                            section.SubSectionId = parent.SubSectionId;
                        }
                        else // search for parent level
                        {
                            var copy = new DocumentSection[documentSections.Count];
                            documentSections.CopyTo(copy);
                            var reversed = copy.Reverse().ToArray();

                            string parentId = null;

                            for (int i = 0; i < reversed.Length; i++)
                            {
                                if (reversed[i].Level == level)
                                {
                                    parentId = reversed[i].SubSectionId;
                                    break;
                                }
                            }

                            section.SubSectionId = parentId;
                        }

                        documentSections.Add(section);
                        parent = section;
                    }
                }

                // Return the list of DocumentSections
                return documentSections;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RetrieveTOC Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RetrieveTOC Service Exception: {ex}");
            }
        }
    }
}