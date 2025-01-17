﻿using Boilerplate.Web.Mvc.OpenGraph;
using EPiServer;
using EPiServer.Commerce.Catalog;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Foundation.Cms.Pages;
using Foundation.Cms.ViewModels;
using Foundation.Commerce.Catalog.ViewModels;
using Foundation.Commerce.Extensions;
using Foundation.Demo.Models;
using Foundation.Find.Cms.Models.Pages;
using Foundation.Infrastructure.OpenGraph;
using Mediachase.Commerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Foundation.Helpers
{
    public static class HtmlHelpers
    {
        private static readonly Lazy<IContentLoader> _contentLoader = new Lazy<IContentLoader>(() => ServiceLocator.Current.GetInstance<IContentLoader>());
        private static readonly Lazy<UrlResolver> _urlResolver = new Lazy<UrlResolver>(() => ServiceLocator.Current.GetInstance<UrlResolver>());
        private static readonly Lazy<IContentTypeRepository> _contentTypeRepository = new Lazy<IContentTypeRepository>(() => ServiceLocator.Current.GetInstance<IContentTypeRepository>());

        public static IHtmlString RenderOpenGraphMetaData(this HtmlHelper helper, IContentViewModel<IContent> contentViewModel)
        {
            var metaTitle = (contentViewModel.CurrentContent as FoundationPageData)?.MetaTitle ?? contentViewModel.CurrentContent.Name;
            var defaultLocale = EPiServer.Globalization.GlobalizationSettings.CultureLanguageCode;
            IEnumerable<string> alternateLocales = null;
            string contentType = null;
            string imageUrl = null;
            IEnumerable<string> category = null;
            string brand = null;
            string priceAmount = null;
            Currency priceCurrency = null;

            if (contentViewModel.CurrentContent is FoundationPageData && ((FoundationPageData)contentViewModel.CurrentContent).PageImage != null)
            {
                imageUrl = GetUrl(((FoundationPageData)contentViewModel.CurrentContent).PageImage);
            }
            else
            {
                imageUrl = GetDefaultImageUrl();
            }

            if (contentViewModel.CurrentContent is FoundationPageData pageData)
            {
                alternateLocales = pageData.ExistingLanguages.Where(culture => culture.TextInfo.CultureName != defaultLocale)
                            .Select(culture => culture.TextInfo.CultureName.Replace('-', '_'));
            }
            else if (contentViewModel.CurrentContent is EntryContentBase entryContent)
            {
                alternateLocales = entryContent.ExistingLanguages.Where(culture => culture.TextInfo.CultureName != defaultLocale)
                            .Select(culture => culture.TextInfo.CultureName.Replace('-', '_'));
            }

            if (contentViewModel.CurrentContent is FoundationPageData)
            {
                if (((FoundationPageData)contentViewModel.CurrentContent).MetaContentType != null)
                {
                    contentType = ((FoundationPageData)contentViewModel.CurrentContent).MetaContentType;
                }
                else
                {
                    var pageType = _contentTypeRepository.Value.Load(contentViewModel.CurrentContent.GetOriginalType());
                    contentType = pageType.DisplayName;
                }
            }

            if (contentViewModel is GenericProductViewModel model)
            {
                brand = model.CurrentContent.Brand;
                priceAmount = model.ListingPrice.ToString().Remove(0, 1);
                priceCurrency = model.ListingPrice.Currency;
                category = GetNodes(model.CurrentContent);
            }

            switch (contentViewModel.CurrentContent)
            {
                case DemoHomePage homePage:
                    var openGraphHomePage = new OpenGraphHomePage(metaTitle, new OpenGraphImage(imageUrl), GetUrl(homePage.ContentLink))
                    {
                        Description = homePage.PageDescription,
                        Locale = defaultLocale.Replace('-', '_'),
                        AlternateLocales = alternateLocales,
                        ContentType = contentType,
                        Category = homePage.Categories?.Select(c => c.ToString()),
                        ModifiedTime = homePage.Changed,
                        PublishedTime = homePage.StartPublish ?? null,
                        ExpirationTime = homePage.StopPublish ?? null
                    };

                    return helper.OpenGraph(openGraphHomePage);

                case LocationItemPage locationItemPage:
                    var openGraphLocationItemPage = new OpenGraphLocationItemPage(metaTitle, new OpenGraphImage(imageUrl), GetUrl(contentViewModel.CurrentContent.ContentLink))
                    {
                        Description = locationItemPage.PageDescription,
                        Locale = defaultLocale.Replace('-', '_'),
                        AlternateLocales = alternateLocales,
                        ContentType = contentType,
                        ModifiedTime = locationItemPage.Changed,
                        PublishedTime = locationItemPage.StartPublish ?? null,
                        ExpirationTime = locationItemPage.StopPublish ?? null
                    };

                    var categories = new List<string>();

                    if (locationItemPage.Continent != null)
                    {
                        categories.Add(locationItemPage.Continent);
                    }

                    if (locationItemPage.Country != null)
                    {
                        categories.Add(locationItemPage.Country);
                    }

                    openGraphLocationItemPage.Category = categories;

                    var tags = new List<string>();
                    foreach (var item in ((LocationItemPage)contentViewModel.CurrentContent).Tags.Items)
                    {
                        tags.Add(item.GetContent().Name);
                    }
                    openGraphLocationItemPage.Tags = tags;

                    return helper.OpenGraph(openGraphLocationItemPage);

                case BlogItemPage _:
                case StandardPage _:
                case TagPage _:
                    var openGraphArticle = new OpenGraphFoundationPageData(metaTitle, new OpenGraphImage(imageUrl), GetUrl(contentViewModel.CurrentContent.ContentLink))
                    {
                        Description = ((FoundationPageData)contentViewModel.CurrentContent).PageDescription,
                        Locale = defaultLocale.Replace('-', '_'),
                        AlternateLocales = alternateLocales,
                        ContentType = contentType,
                        ModifiedTime = ((FoundationPageData)contentViewModel.CurrentContent).Changed,
                        PublishedTime = ((FoundationPageData)contentViewModel.CurrentContent).StartPublish ?? null,
                        ExpirationTime = ((FoundationPageData)contentViewModel.CurrentContent).StopPublish ?? null
                    };

                    return helper.OpenGraph(openGraphArticle);

                case FoundationPageData foundationPageData:
                    var openGraphFoundationPage = new OpenGraphFoundationPageData(metaTitle, new OpenGraphImage(imageUrl), GetUrl(foundationPageData.ContentLink))
                    {
                        Description = foundationPageData.PageDescription,
                        Locale = defaultLocale.Replace('-', '_'),
                        AlternateLocales = alternateLocales,
                        Author = foundationPageData.AuthorMetaData,
                        ContentType = contentType,
                        Category = foundationPageData.Categories?.Select(c => c.ToString()),
                        ModifiedTime = foundationPageData.Changed,
                        PublishedTime = foundationPageData.StartPublish ?? null,
                        ExpirationTime = foundationPageData.StopPublish ?? null
                    };

                    return helper.OpenGraph(openGraphFoundationPage);

                case NodeContentBase nodeContentBase:
                    var openGraphCategory = new OpenGraphGenericNode(metaTitle, new OpenGraphImage(imageUrl), GetUrl(nodeContentBase.ContentLink))
                    {
                        Locale = defaultLocale.Replace('-', '_'),
                        AlternateLocales = alternateLocales,
                        PublishedTime = nodeContentBase.StartPublish ?? null,
                        ExpirationTime = nodeContentBase.StopPublish ?? null
                    };

                    return helper.OpenGraph(openGraphCategory);

                case EntryContentBase entryContentBase:
                    var openGraphEntry = new OpenGraphGenericProduct(entryContentBase.DisplayName, new OpenGraphImage(entryContentBase.GetAssets<IContentImage>(_contentLoader.Value, _urlResolver.Value).FirstOrDefault()), GetUrl(entryContentBase.ContentLink))
                    {
                        Locale = defaultLocale.Replace('-', '_'),
                        AlternateLocales = alternateLocales,
                        Category = category,
                        Brand = brand,
                        PriceAmount = priceAmount,
                        PriceCurrency = priceCurrency
                    };

                    return helper.OpenGraph(openGraphEntry);
            }

            return new HtmlString(string.Empty);
        }

        private static string GetDefaultImageUrl()
        {
            var startPage = _contentLoader.Value.Get<DemoHomePage>(ContentReference.StartPage);
            var siteUrl = SiteDefinition.Current.SiteUrl;
            var url = new Uri(siteUrl, UrlResolver.Current.GetUrl(startPage.SiteLogo));

            return url.ToString();
        }

        private static string GetUrl(ContentReference content)
        {
            var siteUrl = SiteDefinition.Current.SiteUrl;
            var url = new Uri(siteUrl, UrlResolver.Current.GetUrl(content));

            return url.ToString();
        }

        private static List<string> GetNodes(ProductContent currentContent)
        {
            List<string> nodeList = new List<string>();

            foreach (var nodeRelation in currentContent.GetCategories())
            {
                var currentNode = _contentLoader.Value.Get<NodeContent>(nodeRelation);
                if (currentNode != null)
                {
                    AddParentNodes(currentNode, nodeList);
                }
            }

            return nodeList;
        }

        private static void AddParentNodes(NodeContent currentNode, List<string> nodeList)
        {
            if (currentNode == null)
            {
                return;
            }

            if (!nodeList.Contains(currentNode.DisplayName))
            {
                nodeList.Add(currentNode.DisplayName);
            }
            var nodeRelations = currentNode.GetCategories().ToList();
            nodeRelations.Add(currentNode.ParentLink);

            foreach (var nodeRef in nodeRelations)
            {
                var node = _contentLoader.Value.Get<CatalogContentBase>(nodeRef);
                AddParentNodes(node as NodeContent, nodeList);
            }
        }
    }
}