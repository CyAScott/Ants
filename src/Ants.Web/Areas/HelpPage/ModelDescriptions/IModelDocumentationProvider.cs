using System;
using System.Reflection;

#pragma warning disable 1591

namespace Ants.Web.Areas.HelpPage.ModelDescriptions
{
    public interface IModelDocumentationProvider
    {
        string GetDocumentation(MemberInfo member);

        string GetDocumentation(Type type);
    }
}