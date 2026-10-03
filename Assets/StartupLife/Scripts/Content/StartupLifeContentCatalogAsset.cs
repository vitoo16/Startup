#nullable enable
using System;
using StartupLife.Core;
using UnityEngine;

namespace StartupLife.Content
{
    [CreateAssetMenu(fileName = "StartupLifeContent", menuName = "Startup Life/Content Catalog", order = 10)]
    public sealed class StartupLifeContentCatalogAsset : ScriptableObject
    {
        [SerializeField] private ContentCatalogSource source = new ContentCatalogSource();

        public ContentCatalogSource Source => source;

        public ContentCatalog BuildCatalog() => source.Build();

        public void ReplaceSourceForAuthoring(ContentCatalogSource value)
        {
            source = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
