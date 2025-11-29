using Microsoft.Extensions.Options;

namespace Persistance.LiteDb
{
    public class LiteDbOptions : IOptions<LiteDbOptions>
    {
        LiteDbOptions IOptions<LiteDbOptions>.Value => this;

        /// <summary>
        /// Use in-memory database (for testing) instead of file based.
        /// </summary>
        public bool InMemory { get; set; }

        /// <summary>
        /// Path to the LiteDB file. Ignored when <see cref="InMemory"/> is true.
        /// </summary>
        public string DatabasePath { get; set; } = "Stamon.db";

        internal string ConnectionString =>
            InMemory
                ? "Filename=:memory:;Connection=shared"
                : $"Filename={DatabasePath};Connection=shared";
    }
}
