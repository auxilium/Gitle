namespace Gitle.Model
{
    using Enum;
    using Helpers;
    using Interfaces.Model;
    using Localization;
    using System;
    using System.Linq;

    public class ChangeState : Touchable, IIssueAction
    {
        public virtual IssueState IssueState { get; set; }
        public virtual Issue Issue { get; set; }
        public virtual User User { get; set; }
        public virtual DateTime CreatedAt { get; set; }

        public virtual string Text
        {
            get
            {
                var state = Language.ResourceManager.GetString(IssueState.ToString()).ToLower();
                state = (IssueState == IssueState.Open) ? Language.ChangeState_Opened.ToLower() : state;
                var openings = Issue.ChangeStates.Where(x => x.IssueState == IssueState.Open || x.IssueState == IssueState.Urgent).ToList();
                if (openings.Count() > 1 && openings.OrderByDescending(x => x.CreatedAt).Last() != this &&
                    IssueState == IssueState.Open)
                {
                    state = Language.ChangeState_Reopened.ToLower();
                }
                return string.Format("{2} is {0}{1}", state,
                                     User != null ? string.Format(" {1} {0}", User.FullName, Language.By) : "", Language.TheIssue);
            }
        }

        public virtual string HtmlText
        {
            get
            {
                var state = Language.ResourceManager.GetString(IssueState.ToString()).ToLower();
                state = (IssueState == IssueState.Open) ? Language.ChangeState_Opened.ToLower() : state;

                var openings = Issue.ChangeStates.Where(x => x.IssueState == IssueState.Open || x.IssueState == IssueState.Urgent).ToList();


                if (openings.Count() > 1 && openings.OrderByDescending(x => x.CreatedAt).Last() != this &&
                    IssueState == IssueState.Open)
                {
                    if (IssueState != IssueState.Urgent)
                    {
                        state = Language.IssueAction_Delete_Urgent.ToLower();
                    }
                    else
                    {
                        state = Language.ChangeState_Reopened.ToLower();
                    }
                }
                return string.Format("{4} is {0}{1} {3} <strong>{2}</strong>", state,
                                     User != null ? string.Format(" {1} <strong>{0}</strong>", User.FullName, Language.By) : "", DateTimeHelper.Readable(CreatedAt), Language.On, Language.TheIssue);
            }
        }

        public virtual string EmailSubject
        {
            get
            {
                var state = Language.ResourceManager.GetString(IssueState.ToString()).ToLower();
                state = (IssueState == IssueState.Open) ? Language.ChangeState_Opened.ToLower() : state;
                var openings = Issue.ChangeStates.Where(x => x.IssueState == IssueState.Open || x.IssueState == IssueState.Urgent).ToList();
                if (openings.Count() > 1 && openings.OrderByDescending(x => x.CreatedAt).Last() != this &&
                    IssueState == IssueState.Open)
                {
                    state = Language.ChangeState_Reopened.ToLower();
                }
                return string.Format("{3} {2} is {0}{1}", state,
                                     User != null ? string.Format(" {1} {0}", User.FullName, Language.By) : "", Issue.Number, Language.Issue);
            }
        }
    }
}