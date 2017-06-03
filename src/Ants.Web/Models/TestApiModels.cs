using System;

#pragma warning disable 1591

namespace Ants.Web.Models
{
    public class ApiRequest
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
    }
    public class ApiResponse
    {
        public DateTime CreatedOn { get; set; }

        public Guid Id { get; set; }

        public Guid? RequestId { get; set; }

        public string Method { get; set; }

        public string Name { get; set; }
    }
}