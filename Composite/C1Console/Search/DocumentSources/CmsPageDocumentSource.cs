﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Composite.C1Console.Search.Crawling;
using Composite.Core;
using Composite.Core.Extensions;
using Composite.Core.Linq;
using Composite.Core.Routing;
using Composite.Core.WebClient;
using Composite.Data;
using Composite.Data.ProcessControlled.ProcessControllers.GenericPublishProcessController;
using Composite.Data.Types;

namespace Composite.C1Console.Search.DocumentSources
{
    internal class CmsPageDocumentSource : ISearchDocumentSource
    {
        const string LogTitle = nameof(CmsPageDocumentSource);

        private readonly List<IDocumentSourceListener> _listeners = new List<IDocumentSourceListener>();
        private readonly DataChangesIndexNotifier _changesIndexNotifier;
        private readonly Lazy<ICollection<DocumentField>> _customFields;

        public CmsPageDocumentSource()
        {
            _customFields = new Lazy<ICollection<DocumentField>>(() =>
            {
                var pageDocFields = DataTypeSearchReflectionHelper.GetDocumentFields(typeof (IPage));
                var metaDataFields = PageMetaDataFacade.GetAllMetaDataTypes()
                    .SelectMany(dataType => DataTypeSearchReflectionHelper.GetDocumentFields(dataType, false));

                return pageDocFields
                       .Concat(metaDataFields)
                       .ExcludeDuplicateKeys(f => f.Name)
                       .Evaluate();
            });

            _changesIndexNotifier = new DataChangesIndexNotifier(
                _listeners, typeof(IPage), 
                data => FromPage((IPage) data),
                data => GetDocumentId((IPage) data));

            _changesIndexNotifier.Start();
        }

        public string Name => typeof (IPage).FullName;

        public void Subscribe(IDocumentSourceListener sourceListener)
        {
            _listeners.Add(sourceListener);
        }

        public IEnumerable<SearchDocument> GetAllSearchDocuments(CultureInfo culture)
        {
            ICollection<IPage> unpublishedPages;

            using (var conn = new DataConnection(PublicationScope.Unpublished, culture))
            {
                unpublishedPages = conn.Get<IPage>().Evaluate();
            }

            var publishedPages = new Dictionary<Tuple<Guid, Guid>, IPage>();
            using (var conn = new DataConnection(PublicationScope.Published, culture))
            {
                publishedPages = conn.Get<IPage>().ToDictionary(page => new Tuple<Guid, Guid>(page.Id, page.VersionId));
            }

            foreach (var unpublishedPage in unpublishedPages)
            {
                IPage publishedPage;
                if (publishedPages.TryGetValue(new Tuple<Guid, Guid>(unpublishedPage.Id, unpublishedPage.VersionId), 
                        out publishedPage))
                {
                    yield return FromPage(publishedPage);

                    if (unpublishedPage.PublicationStatus == GenericPublishProcessController.Published)
                    {
                        // If page is in "published" state, indexing only one version of it
                        continue;
                    }
                }

                yield return FromPage(unpublishedPage);
            }
        }

        public ICollection<DocumentField> CustomFields => _customFields.Value;

        private SearchDocument FromPage(IPage page)
        {
            string label = page.MenuTitle;
            if (string.IsNullOrWhiteSpace(label))
            {
                label = page.Title;
            }

            var documentBuilder = new SearchDocumentBuilder();

            documentBuilder.SetDataType(typeof(IPage));
            documentBuilder.CrawlData(page);

            using (new DataConnection(page.DataSourceId.PublicationScope, page.DataSourceId.LocaleScope))
            {
                var placeholders = PageManager.GetPlaceholderContent(page.Id, page.VersionId);
                placeholders.ForEach(pl => documentBuilder.CrawlData(pl, true));

                try
                {
                    page.GetMetaData()
                        .ForEach(pageMetaData => documentBuilder.CrawlData(pageMetaData));
                }
                catch (Exception ex)
                {
                    Log.LogWarning(LogTitle, ex);
                }
            }

            bool isPublished = page.DataSourceId.PublicationScope == PublicationScope.Published;
            string documentId = GetDocumentId(page);

            string url = isPublished ? PageUrls.BuildUrl(page, UrlKind.Internal) : null;

            var entityToken = page.GetDataEntityToken();

            if (isPublished)
            {
                entityToken.DataSourceId.DataScopeIdentifier = DataScopeIdentifier.Administrated;
            }

            return documentBuilder.BuildDocument(Name, documentId, label, null, entityToken, url);
        }

        private string GetDocumentId(IPage page)
        {
            bool isUnpublished = page.DataSourceId.PublicationScope == PublicationScope.Unpublished;

            string versionId = "";
            if (page.VersionId != Guid.Empty)
            {
                versionId = UrlUtils.CompressGuid(page.VersionId);
            }
            return $"{UrlUtils.CompressGuid(page.Id)}{versionId}" + (isUnpublished ? "u" : "");
        }
    }
}
