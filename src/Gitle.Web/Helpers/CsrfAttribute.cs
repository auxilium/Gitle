namespace Gitle.Web.Helpers
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CsrfAttribute : Attribute
    {
         
    }
}