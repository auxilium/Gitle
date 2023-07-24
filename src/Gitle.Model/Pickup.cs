using Gitle.Localization;

namespace Gitle.Model
{
    using Helpers;
    using Interfaces.Model;
    using System;

    public class HandOver : Touchable, IIssueAction
    {
        public virtual Issue Issue { get; set; }
        public virtual User User { get; set; }
        public virtual User ByUser { get; set; }
        public virtual DateTime CreatedAt { get; set; }

        public virtual string Text
        {
            get
            {
                return
                    string.Format(
                        "{2}{0}{1}",
                        User != null ? string.Format(" {1} <strong>{0}</strong>", User.FullName, Language.To) : "",
                        ByUser != null ? string.Format(" {1} <strong>{0}</strong>", ByUser.FullName, Language.By) : "", Language.IssueAction_HandOver_Text);
            }
        }

        public virtual string HtmlText
        {
            get
            {
                return
                    string.Format(
                        "{3}{0}{1} {4} <strong>{2}</strong>",
                        User != null ? string.Format(" {1} <strong>{0}</strong>", User.FullName, Language.To) : "",
                        ByUser != null ? string.Format(" {1} <strong>{0}</strong>", ByUser.FullName, Language.By) : "",
                        DateTimeHelper.Readable(CreatedAt), Language.IssueAction_HandOver_Text, Language.On);
            }
        }

        public virtual string EmailSubject
        {
            get
            {
                return string.Format("{3} {2} is {4}{0}{1}",
                                     User != null ? string.Format(" {1} {0}", User.FullName, Language.To) : "",
                                     ByUser != null ? string.Format(" {1} {0}", ByUser.FullName, Language.By) : "",
                                     Issue.Id, Language.Issue, Language.HandedOver.ToLower());
            }
        }


    }
}