namespace Gitle.Model
{
    using System.Collections.Generic;
    using Interfaces.Model;

    public class Application : ModelBase, ISlugger
    {
        public virtual string Name { get; set; }
        public virtual string RepositoryUrl { get; set; }
        public virtual string Slug { get; set; }
        public virtual string Comments { get; set; }
        public virtual IList<Project> Projects { get; set; }
        public virtual Customer Customer { get; set; }

        public virtual string CompleteName => $"{Name} ({Customer?.Name})";
    }
}