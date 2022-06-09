namespace Gitle.ViewModel
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;

    public class ProjectListViewModel
    {
        public ProjectListViewModel(Project project)
        {
            Id = project.Id;
            Slug = project.Slug;
            Name = project.Name;
            Number = project.Number;
            Application = project.Application?.Name;
            Customer = project.Application?.Customer?.Name;
            Type = project.TypeString;
            Closed = project.Closed;
            Urgent = project.Issues.Any(i => i.Urgent);
        }

        public long Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
        public string Application { get; set; }
        public string Customer { get; set; }
        public string Type { get; set; }
        public bool Closed { get; set; }
        public bool Urgent { get; set; }
    }
}
