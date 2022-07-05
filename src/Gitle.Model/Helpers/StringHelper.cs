// ReSharper disable once IdentifierTypo
namespace Gitle.Model.Helpers
{
    using System.Globalization;
    using System.Text.RegularExpressions;
    using MarkdownDeep;

    public static class StringHelper
    {
        // ReSharper disable once IdentifierTypo
        public static string Slugify(this string phrase)
        {
            var str = phrase.RemoveAccent().ToLower();

            str = Regex.Replace(str, @"[^a-z0-9\s-]", ""); // invalid chars           
            str = Regex.Replace(str, @"\s+", " ").Trim(); // convert multiple spaces into one space   
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim(); // cut and trim it   
            str = Regex.Replace(str, @"\s", "-"); // hyphens   

            return str;
        }

        public static string RemoveAccent(this string txt)
        {
            var bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }

        public static string ToCamelCase(this string txt)
        {
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(txt);
        }

        public static string Markdown(this string txt, Project project)
        {
            if (string.IsNullOrEmpty(txt)) return "";
            // spaces in urls will not work with MarkdownDeep
            txt = Regex.Replace(txt, @"\(http(.*)\)", match => match.ToString().Replace(" ", "%20"));
            // single linebreaks become double linebreaks to conform gitle markdown
            var md = new Markdown();
            txt = Regex.Replace(txt, @"( |^)(?<hash>#(?<number>[0-9]{1,6}))(?<hash2>#(?<number2>[0-9]{1,6}))( |\.|$)",
                match => $" <a href=\"/project/{project.Slug}/issue/{match.Groups["number"]}/view#{match.Groups["number2"]}\">{match.Groups["hash"]}{match.Groups["hash2"]}</a> ");
            txt = Regex.Replace(txt, @"( |^)(?<hash>#(?<number>[0-9]{1,6}))( |\.|$)",
                match => $" <a href=\"/project/{project.Slug}/issue/{match.Groups["number"]}/view\">{match.Groups["hash"]}</a> ");
            txt = md.Transform(txt);
            txt = Regex.Replace(txt, @"(?<!(</p>))\n(?!\n)", match => "<br/>");
            return txt;
        }

        public static string ToHourDayNotation(this double hours)
        {
            return hours < 8 ?
               hours > 1 ? $"{hours} hours" : $"{hours} hour" :
               hours/8 > 1 ? $"{hours/8} days" : $"{hours / 8} day";
        }
    }
}