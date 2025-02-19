using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers
{
    public abstract class IndexerBase<TSettings> : IIndexer
        where TSettings : IIndexerSettings, new()
    {
        protected readonly IIndexerStatusService _indexerStatusService;
        protected readonly IConfigService _configService;
        protected readonly Logger _logger;

        public abstract string Name { get; }
        public abstract string[] IndexerUrls { get; }
        public abstract string Description { get; }
        public abstract Encoding Encoding { get; }
        public abstract string Language { get; }
        public abstract bool FollowRedirect { get; }
        public abstract DownloadProtocol Protocol { get; }
        public abstract IndexerPrivacy Privacy { get; }
        public int Priority { get; set; }
        public bool Redirect { get; set; }

        public abstract bool SupportsRss { get; }
        public abstract bool SupportsSearch { get; }
        public abstract bool SupportsRedirect { get; }
        public abstract IndexerCapabilities Capabilities { get; protected set; }

        public IndexerBase(IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        {
            _indexerStatusService = indexerStatusService;
            _configService = configService;
            _logger = logger;
        }

        public Type ConfigContract => typeof(TSettings);

        public bool IsObsolete()
        {
            var attributes = GetType().GetCustomAttributes(false);

            foreach (ObsoleteAttribute attribute in attributes.OfType<ObsoleteAttribute>())
            {
                return true;
            }

            return false;
        }

        public virtual ProviderMessage Message => null;

        public virtual IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                var config = (IProviderConfig)new TSettings();

                yield return new IndexerDefinition
                {
                    Name = GetType().Name,
                    Enable = config.Validate().IsValid && SupportsRss,
                    Implementation = GetType().Name,
                    Settings = config
                };
            }
        }

        public virtual ProviderDefinition Definition { get; set; }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "getUrls")
            {
                var links = IndexerUrls;

                return new
                {
                    options = links.Select(d => new { Value = d, Name = d })
                };
            }

            return null;
        }

        protected TSettings Settings => GetDefaultBaseUrl((TSettings)Definition.Settings);

        public abstract Task<IndexerPageableQueryResult> Fetch(MovieSearchCriteria searchCriteria);
        public abstract Task<IndexerPageableQueryResult> Fetch(MusicSearchCriteria searchCriteria);
        public abstract Task<IndexerPageableQueryResult> Fetch(TvSearchCriteria searchCriteria);
        public abstract Task<IndexerPageableQueryResult> Fetch(BookSearchCriteria searchCriteria);
        public abstract Task<IndexerPageableQueryResult> Fetch(BasicSearchCriteria searchCriteria);
        public abstract Task<byte[]> Download(Uri link);

        public abstract IndexerCapabilities GetCapabilities();

        protected virtual IList<ReleaseInfo> CleanupReleases(IEnumerable<ReleaseInfo> releases)
        {
            var result = releases.ToList();

            result.ForEach(c =>
            {
                //Set GUID if not set
                if (c.Guid.IsNullOrWhiteSpace())
                {
                    if (c.DownloadUrl.IsNotNullOrWhiteSpace())
                    {
                        c.Guid = c.DownloadUrl;
                    }
                    else if (Protocol == DownloadProtocol.Torrent && ((TorrentInfo)c).MagnetUrl.IsNotNullOrWhiteSpace())
                    {
                        c.Guid = ((TorrentInfo)c).MagnetUrl;
                    }
                    else if (c.InfoUrl.IsNotNullOrWhiteSpace())
                    {
                        c.Guid = c.InfoUrl;
                    }
                }

                //Set common props
                c.IndexerId = Definition.Id;
                c.Indexer = Definition.Name;
                c.DownloadProtocol = Protocol;
                c.IndexerPriority = ((IndexerDefinition)Definition).Priority;

                //Add common flags
                if (Protocol == DownloadProtocol.Torrent && ((TorrentInfo)c).DownloadVolumeFactor == 0)
                {
                    c.IndexerFlags.Add(IndexerFlag.FreeLeech);
                }
            });

            return result.DistinctBy(v => v.Guid).ToList();
        }

        protected TSettings GetDefaultBaseUrl(TSettings settings)
        {
            if (settings.BaseUrl.IsNullOrWhiteSpace() && IndexerUrls.First().IsNotNullOrWhiteSpace())
            {
                settings.BaseUrl = IndexerUrls.First();
            }

            return settings;
        }

        public ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                Test(failures).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Test aborted due to exception");
                failures.Add(new ValidationFailure(string.Empty, "Test was aborted due to an error: " + ex.Message));
            }

            return new ValidationResult(failures);
        }

        protected abstract Task Test(List<ValidationFailure> failures);

        public override string ToString()
        {
            return Definition.Name;
        }
    }
}
