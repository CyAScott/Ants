using System.Collections.ObjectModel;

#pragma warning disable 1591

namespace Ants.Web.Areas.HelpPage.ModelDescriptions
{
    public class ParameterDescription
    {
        public ParameterDescription()
        {
            Annotations = new Collection<ParameterAnnotation>();
        }

        public Collection<ParameterAnnotation> Annotations { get; }

        public string Documentation { get; set; }

        public string Name { get; set; }

        public ModelDescription TypeDescription { get; set; }
    }
}