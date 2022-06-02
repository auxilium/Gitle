using Gitle.Localization;

namespace Gitle.Model
{
    using Helpers;
    using Interfaces.Model;
    using System;

    public class Comment : Touchable, IIssueAction
    {
        public virtual string Text { get; set; }

        public virtual string HtmlText
        {
            get { return Text.Markdown(Issue.Project); }
        }

        public virtual DateTime CreatedAt { get; set; }

        public virtual bool IsInternal { get; set; }


        public virtual User User { get; set; }
        public virtual Issue Issue { get; set; }

        public virtual string Name { get { return User != null ? User.FullName : "Auxilium"; } }

        public virtual string EmailSubject { get { return string.Format(Language.IssueAction_Comment_EmailSubject, User != null ? User.FullName : "", Issue.Number); } }
    }
}