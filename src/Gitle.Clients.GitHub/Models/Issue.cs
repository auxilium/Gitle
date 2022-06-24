namespace Gitle.Clients.GitHub.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;
    using Helpers;
    using Post;

    [DataContract]
    public class Issue
    {
        public Issue()
        {
            Title = "";
            Labels = new List<Label>();
        }

        private readonly Regex repo = new Regex(@"repos/(.*?)/issues");

        [DataMember(Name = "url")]
        public virtual string Url { get; set; }

        public virtual string RepoName
        {
            get { return repo.Matches(Url).Cast<Match>().Select(p => p.Value).FirstOrDefault().Replace("repos/", "").Replace("/issues", ""); }
        }

        [DataMember(Name = "number")]
        public virtual int Number { get; set; }

        [DataMember(Name = "title")]
        public virtual string Title { get; set; }

        [DataMember(Name = "state")]
        public virtual string State { get; set; }

        public virtual double Hours
        {
            get { return TitleHelper.GetHoursFromTitle(Title); }
            set { Title = TitleHelper.CreateTitle(Title, Devvers, value); }
        }

        public virtual int Devvers
        {
            get { return TitleHelper.GetDevversFromTitle(Title); }
            set { Title = TitleHelper.CreateTitle(Title, value, Hours); }
        }

        public virtual string Name
        {
            get { return TitleHelper.GetNameFromTitle(Title); }
            set { Title = TitleHelper.CreateTitle(value, Devvers, Hours); }
        }

        public virtual string DevversString
        {
            get { return Hours > 0 ? Devvers.ToString() : "n.n.b."; }
        }

        public virtual string HoursString
        {
            get { return Hours > 0 ? Hours <= 2.5 ? string.Format("{0} uur", Hours) : string.Format("{0} dag", Hours/8) : "n.n.b."; }
        }

        public virtual string EstimateString
        {
            get { return Hours > 0 ? string.Format("{0} developer{1} {2}", Devvers, Devvers > 1 ? "s" : "", HoursString) : "n.n.b."; }
        }

        public virtual double TotalHours
        {
            get { return Hours*Devvers; }
        }

        public virtual string CostString(double hourPrice)
        {
            var culture = new CultureInfo("nl-NL");
            return TotalHours > 0 ? (TotalHours*hourPrice).ToString("C", culture) : "n.n.b.";
        }

        [DataMember(Name = "body")]
        public virtual string Body { get; set; }

        [DataMember(Name = "milestone")]
        public virtual Milestone Milestone { get; set; }

        [DataMember(Name = "labels")]
        public virtual List<Label> Labels { get; set; }

        public bool CheckLabel(string label)
        {
            return Labels.Select(l => l.Name).Contains(label);
        }

        [DataMember(Name = "comments")]
        public virtual int Comments { get; set; }

        [DataMember(Name = "created_at")]
        public virtual DateTime CreatedAt { get; set; }

        [DataMember(Name = "closed_at")]
        public virtual DateTime? ClosedAt { get; set; }

        [DataMember(Name = "updated_at")]
        public virtual DateTime UpdatedAt { get; set; }

        [DataMember(Name = "html_url")]
        public virtual string HtmlUrl { get; set; }

        public virtual IssuePost ToPost()
        {
            return new IssuePost
                       {
                           Title = Title,
                           Body = Body,
                           Milestone = Milestone.Number,
                           Labels = Labels.Select(l => l.Name).ToList()
                       };
        }

        public virtual IssuePatch ToPatch()
        {
            return new IssuePatch
                       {
                           Title = Title,
                           State = State,
                           Body = Body,
                           Milestone = Milestone.Number,
                           Labels = Labels.Select(l => l.Name).ToList()
                       };
        }

    }
}