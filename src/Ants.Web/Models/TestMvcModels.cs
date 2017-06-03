using System;

#pragma warning disable 1591

namespace Ants.Web.Models
{
    public class MvcRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = "John Doe";
    }
    public class MvcResponse
    {
        public DateTime CreatedOn { get; set; }

        public Guid Id { get; set; }

        public string Name { get; set; }
    }
}