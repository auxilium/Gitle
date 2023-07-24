using Gitle.Localization;

namespace Gitle.Model
{
    using Helpers;
    using Interfaces.Model;
    using System;

    public class Pickup : Touchable, IIssueAction
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
                        "{1}{0}",
                        User != null ? string.Format(" {1} <strong>{0}</strong>", User.FullName, Language.By) : "", Language.IssueAction_PickUp_Text);
            }
        }

        public virtual string HtmlText
        {
            get
            {
                return
                    string.Format(
                        "{3}{0} {2} <strong>{1}</strong>",
                        User != null ? string.Format(" {1} <strong>{0}</strong>", User.FullName, Language.By) : "",
                        DateTimeHelper.Readable(CreatedAt), Language.On, Language.IssueAction_PickUp_Text);
            }
        }

        public virtual string EmailSubject { get { return string.Format("{3} {1} is {2} {4} {0}", User != null ? User.FullName : "", Issue.Id, Language.PickedUp.ToLower(), Language.Issue, Language.By); } }

    }
}