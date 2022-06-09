using System;
using Newtonsoft.Json;

namespace Gitle.Clients.GitHub
{
    using System.Collections.Generic;
    using System.Configuration;
    using Gitle.Clients.GitHub.Models;
    using Interfaces;
    using ServiceStack.ServiceClient.Web;

    public class IssueClient : IIssueClient
    {
        private readonly JsonServiceClient _client;
        private readonly int perPage = 20;

        public IssueClient(string token, string useragent, string githubApi)
        {
            _client = new JsonServiceClient(githubApi);
            _client.Headers.Add("Authorization", "token " + token);
            _client.LocalHttpWebRequestFilter = request => request.UserAgent = useragent;
        }

        public List<Issue> List(string repo, string state = "open,closed,urgent")
        {
            return List(repo, 0, state);
        }

        public List<Issue> List(string repo, int milestoneId, string state = "open,closed,urgent")
        {
            var issues = new List<Issue>();
            foreach (var s in state.Split(','))
            {
                var paging = true;
                for (var page = 1; paging; page++ )
                {
                    var url = string.Format("repos/{0}/issues?page={1}&per_page={2}&state={3}{4}", 
                                            repo, page, perPage, s,
                                            milestoneId > 0 ? string.Format("&milestone={0}", milestoneId) : "");
                    var issuePage = _client.Get<List<Issue>>(url);
                    issues.AddRange(issuePage);

                    if (issuePage.Count < perPage)
                        paging = false;
                }
            }
            return issues;
        }

        public Issue Get(string repo, int issueId)
        {
            if (issueId == 0) return null;
            return _client.Get<Issue>("repos/" + repo + "/issues/" + issueId);
        }

        public Issue Post(string repo, Issue issue)
        {
            return _client.Post<Issue>("repos/" + repo + "/issues", issue.ToPost());
        }

        public Issue Patch(string repo, int issueId, Issue issue)
        {
            return _client.Patch<Issue>("repos/" + repo + "/issues/" + issueId, issue.ToPatch());
        }
    }
}