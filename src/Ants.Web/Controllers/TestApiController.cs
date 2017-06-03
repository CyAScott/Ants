using System;
using System.Net.Http;
using System.Web.Http;
using Ants.Web.Filters;
using Ants.Web.Models;

namespace Ants.Web.Controllers
{
    /// <summary>
    /// Example API controller.
    /// </summary>
    public class TestApiController : ApiController
    {
        /// <summary>
        /// A simple delete by ID endpoint.
        /// </summary>
        /// <param name="id">A GUID</param>
        [IdFilter, HttpDelete, Route("api/TestApi/{id:guidItem}")]
        public ApiResponse Delete(string id)
        {
            return new ApiResponse
            {
                CreatedOn = DateTime.UtcNow,
                Id = Guid.Parse(id),
                Method = HttpMethod.Delete.Method,
                Name = "Delete Item"
            };
        }

        /// <summary>
        /// A simple get by ID endpoint.
        /// </summary>
        /// <param name="id">A GUID</param>
        [IdFilter, HttpGet, Route("api/TestApi/{id:guidItem}")]
        public ApiResponse Get(string id)
        {
            return new ApiResponse
            {
                CreatedOn = DateTime.UtcNow,
                Id = Guid.Parse(id),
                Method = HttpMethod.Get.Method,
                Name = "Get Item"
            };
        }

        /// <summary>
        /// A simple post item.
        /// </summary>
        /// <param name="id">A GUID</param>
        /// <param name="request">The body of the post.</param>
        [IdFilter, HttpPost, Route("api/TestApi/{id:guidItem}")]
        public ApiResponse Post(string id, ApiRequest request)
        {
            if (!ModelState.IsValid)
            {
                throw new ArgumentException(nameof(request));
            }

            return new ApiResponse
            {
                CreatedOn = DateTime.UtcNow,
                Id = Guid.Parse(id),
                RequestId = request.RequestId,
                Method = HttpMethod.Post.Method,
                Name = "Post Item"
            };
        }

        /// <summary>
        /// A simple put item.
        /// </summary>
        /// <param name="id">A GUID</param>
        /// <param name="request">The body of the post.</param>
        [IdFilter, HttpPut, Route("api/TestApi/{id:guidItem}")]
        public ApiResponse Put(string id, ApiRequest request)
        {
            if (!ModelState.IsValid)
            {
                throw new ArgumentException(nameof(request));
            }

            return new ApiResponse
            {
                CreatedOn = DateTime.UtcNow,
                Id = Guid.Parse(id),
                RequestId = request.RequestId,
                Method = HttpMethod.Put.Method,
                Name = "Put Item"
            };
        }
    }
}