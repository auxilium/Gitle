using Gitle.Localization;

namespace Gitle.Model
{
    using Helpers;
    using Interfaces.Model;
    using System;

    public class Change : Touchable, IIssueAction
    {
        public virtual Issue Issue { get; set; }
        public virtual User User { get; set; }
        public virtual DateTime CreatedAt { get; set; }

        public virtual string Text
        {
            get
            {
                return
                    string.Format(
                        "{0}{1}", Language.IssueAction_Change_Text,
                        User != null ? string.Format(" door <strong>{0}</strong>", User.FullName) : "");
            }
        }

        public virtual string HtmlText
        {
            get
            {
                return
                    string.Format(
                        "{0}{2} {1} <strong>{3}</strong>", Language.IssueAction_Change_Text, Language.On,
                        User != null ? string.Format(" {0} <strong>{1}</strong>", Language.By, User.FullName) : "",
                        DateTimeHelper.Readable(CreatedAt));
            }
        }

        public virtual string EmailSubject { get { return string.Format(Language.IssueAction_Change_EmailSubject, User != null ? User.FullName : "", Issue.Id); } }
    }
}